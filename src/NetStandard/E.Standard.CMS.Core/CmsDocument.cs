using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.Security;
using E.Standard.Configuration;
using E.Standard.Extensions.Compare;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace E.Standard.CMS.Core;

public class CmsDocument : IDisposable
{
    internal static NumberFormatInfo nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    public const string AuthExclusivePostfix = ".@@EXCLUSIVE@@";
    public const bool UseAuthExclusives = true;

    private DateTime _lastWriteTimeUtc;
    private XmlDocument _doc = null;
    private XmlNode _aclNode = null;
    private bool _isPortalCms = false;
    private readonly string _name;
    private Dictionary<string, AuthNode> _cachedAuthNodes = null;
    private readonly IEnumerable<ICustomCmsDocumentAclProviderService> _aclProviders;

    private CmsDocument(CmsDocument cms)
    {
        _lastWriteTimeUtc = cms._lastWriteTimeUtc;
        _doc = cms._doc;
        _aclNode = cms._aclNode;
        _isPortalCms = cms._isPortalCms;
        _name = cms._name;
        _aclProviders = cms._aclProviders;

        _cachedAuthNodes = null; // Soll bei jedem Parse wieder neu intialisert werden. Brauch zwar mehr Zeit muss dafür nicht Threadsafe gemacht werden!! Würde sonst noch länger brauchen!!

        this.WebAppPath = cms.WebAppPath;
    }

    public CmsDocument(string cmsName,
                       string webAppPath,
                       string webEtcPath,
                       IEnumerable<ICustomCmsDocumentAclProviderService> aclProviders)
    {
        _lastWriteTimeUtc = new DateTime(0);
        _name = cmsName;
        _aclProviders = aclProviders;

        this.WebAppPath = webAppPath;
        this.WebEtcPath = webEtcPath;
    }

    public bool ReadXml(string filename)
    {
        try
        {
            FileInfo fi = new FileInfo(filename);

            if (fi.Exists)
            {
                if (!_lastWriteTimeUtc.Equals(fi.LastWriteTimeUtc))  // nur laden, wenn datei geändert wurde
                {
                    _lastWriteTimeUtc = fi.LastWriteTimeUtc;
                    _doc = new XmlDocument();
                    _doc.Load(filename);

                    _aclNode = _doc.SelectSingleNode("config/acl");
                    _cachedAuthNodes = null;

                    return true;
                }
            }
            else
            {
                _doc = null;
                _aclNode = null;
                _cachedAuthNodes = null;
                _lastWriteTimeUtc = new DateTime();
                return true;
            }
        }
        catch
        {
            _doc = null;
            _aclNode = null;
            _cachedAuthNodes = null;
            _lastWriteTimeUtc = new DateTime(0);
            return true;
        }

        return false;
    }
    public bool ReadXml(XmlDocument doc)
    {
        try
        {
            _lastWriteTimeUtc = new DateTime(0);
            _doc = doc;
            _aclNode = _doc.SelectSingleNode("config/acl");
            _cachedAuthNodes = null;
        }
        catch
        {
            _doc = null;
            _aclNode = null;
            _cachedAuthNodes = null;
        }
        return true;
    }

    public DateTime LastWriteTimeUtc
    {
        get { return _lastWriteTimeUtc; }
    }

    public bool IsPortalCms
    {
        get { return _isPortalCms; }
        set { _isPortalCms = value; }
    }
    public string CmsName
    {
        get { return _name; }
    }

    public CmsNode CreateEmptyNode()
    {
        return new CmsNode(this, null, null);
    }

    public string WebAppPath
    {
        get;
        private set;
    }

    public string WebEtcPath { get; private set; }

    public void Dispose()
    {
        _doc = null;
        _aclNode = null;

        if (_cachedAuthNodes != null)
        {
            _cachedAuthNodes.Clear();
            _cachedAuthNodes = null;
        }
    }

    #region Query
    
    public CmsNodeCollection SelectNodes(UserIdentification ui, string xPath)
    {
        try
        {
            CmsNodeCollection collection = new CmsNodeCollection();
            if (RootNode == null)
            {
                return collection;
            }

            foreach (XmlNode node in RootNode.SelectNodes(xPath + "[@schemanode='1']"))
            {
                if (ui != null)
                {
                    if (!CheckAuth(ui, NodeXPath(node)))
                    {
                        continue;
                    }
                }

                CmsNode cmsNode = CreateCmsNode(ui, node);
                if (cmsNode != null)
                {
                    collection.Add(cmsNode);
                }
            }
            return collection;
        }
        catch (Exception ex)
        {
            throw new Exception("CMS Parse Exception", new Exception("SelectNodes('" + xPath + "')", ex));
        }
    }
    public CmsNodeCollection SelectNodes(UserIdentification ui, string xPath, string propertyName, string value)
    {
        return SelectNodes(ui, xPath, propertyName, value, false);
    }
    public CmsNodeCollection SelectNodes(UserIdentification ui, string xPath, string propertyName, string value, bool fuzzy)
    {
        try
        {
            CmsNodeCollection collection = new CmsNodeCollection();

            bool returnLink = propertyName.StartsWith("~");
            propertyName = (returnLink == true ? propertyName.Substring(1, propertyName.Length - 1) : propertyName);

            foreach (CmsNode cmsNode in SelectNodes(ui, xPath))
            {
                if (cmsNode == null)
                {
                    continue;
                }

                if (Compare(cmsNode.LoadString(propertyName), value, fuzzy) ||
                    (propertyName == "url" &&
                     cmsNode.Url == value))
                {
                    collection.Add(cmsNode);
                }
                if (cmsNode is CmsLink &&
                    ((CmsLink)cmsNode).Target != null &&
                    (
                     Compare(((CmsLink)cmsNode).Target.LoadString(propertyName), value, fuzzy) ||
                     (propertyName == "url" && ((CmsLink)cmsNode).Target.Url == value)
                    )
                   )
                {
                    if (returnLink)
                    {
                        collection.Add(cmsNode);
                    }
                    else
                    {
                        collection.Add(((CmsLink)cmsNode).Target);
                    }
                }
            }
            return collection;
        }
        catch (Exception ex)
        {
            throw new Exception("CMS Parse Exception", new Exception("SelectNodes('" + xPath + "')", ex));
        }
    }

    public CmsNode SelectSingleNode(UserIdentification ui, string xPath)
    {
        try
        {
            if (RootNode == null)
            {
                return null;
            }

            if (!CheckAuth(ui, xPath))
            {
                return null;
            }

            XmlNode node = RootNode.SelectSingleNode(xPath + "[@schemanode='1']");
            if (node == null)
            {
                return null;
            }

            return CreateCmsNode(ui, node);
        }
        catch (Exception ex)
        {
            throw new Exception("CMS Parse Exception", new Exception("SelectSingleNode('" + xPath + "')", ex));
        }
    }
    public CmsNode SelectSingleNode(UserIdentification ui, string xPath, string propertyName, string value)
    {
        try
        {
            CmsNodeCollection collection = SelectNodes(ui, xPath, propertyName, value);
            if (collection != null &&
                collection.Count > 0)
            {
                return collection[0];
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new Exception("CMS Parse Exception", new Exception("SelectSingleNode('" + xPath + "')", ex));
        }
    }

    public CmsArrayItems SelectArray(UserIdentification ui, string xPath, string arrayName)
    {
        CmsArrayItems items = new CmsArrayItems();
        CmsNode node = SelectSingleNode(ui, xPath);
        if (node == null)
        {
            return items;
        }

        int count = (int)node.Load(arrayName + "_count", 0);
        for (int i = 0; i < count; i++)
        {
            string itemGuid = node.LoadString(arrayName + i);
            if (String.IsNullOrEmpty(itemGuid))
            {
                continue;
            }

            if (ui != null)
            {
                string itemAuthXPath = node.NodeXPath + "@" + arrayName + "_" + itemGuid;
                if (!CheckAuth(ui, itemAuthXPath))
                {
                    continue;
                }
            }

            CmsArrayItem item = new CmsArrayItem(node, arrayName, itemGuid);
            items.Add(item);
        }

        return items;
    }

    private CmsNode CreateCmsNode(UserIdentification ui, XmlNode node)
    {
        if (node == null)
        {
            return null;
        }

        XmlNode linkUriNode = node.SelectSingleNode("_linkuri");
        if (linkUriNode != null)
        {
            if (!CheckAuth(ui, linkUriNode.InnerText))
            {
                return null;
            }

            return new CmsLink(this, ui, node,
                String.IsNullOrEmpty(linkUriNode.InnerText) ? null : this.SelectSingleNode(ui, linkUriNode.InnerText.Replace(@"\", "/")));
        }
        else
        {
            return new CmsNode(this, ui, node);
        }
    }
    private XmlNode RootNode
    {
        get
        {
            if (_doc == null)
            {
                return null;
            }

            return _doc.SelectSingleNode("config");
        }
    }

    private string NodeXPath(XmlNode node)
    {
        if (node == null)
        {
            return String.Empty;
        }

        string xPath = node.Name;
        while (node.ParentNode != null)
        {
            node = node.ParentNode;
            if (node.Name == "config")
            {
                break;
            }

            xPath = $"{node.Name}/{xPath}";
        }

        return xPath;
    }
    
    #endregion

    #region Authorization
    
    public const string Everyone = "jeder";

    internal bool CheckAuth(UserIdentification ui, string path)
    {
        if (ui == null ||
            _aclNode == null ||
            _aclNode.ChildNodes.Count == 0)
        {
            return true;
        }

        AuthNode authNode = GetAuthNode(path);

        return CheckAuthorization(ui, authNode);
    }

    #region AuthNodeCaching

    private void ReadAuthNodes(XmlNode aclNode)
    {
        _cachedAuthNodes = new Dictionary<string, AuthNode>();

        if (aclNode == null)
        {
            return;
        }

        foreach (XmlNode authNode in aclNode.SelectNodes("authnode[@value]"))
        {
            _cachedAuthNodes.Add(authNode.Attributes["value"].Value, new AuthNode(authNode));
        }

        _aclProviders.EachIfNotNull((aclProvider) =>
        {
            var dictAuthNodes = aclProvider.CmsAuthNodes(_name).Result;

            dictAuthNodes?.Keys.EachIfNotNull((path) =>
            {
                if (_cachedAuthNodes.ContainsKey(path))
                {
                    _cachedAuthNodes[path].Append(dictAuthNodes[path]);
                }
                else
                {
                    _cachedAuthNodes.Add(path, dictAuthNodes[path]);
                }
            });
        });
    }

    private AuthNode GetAuthNode(string xPath, bool exactPathOnly = false)
    {
        if (_cachedAuthNodes == null)
        {
            ReadAuthNodes(_aclNode);
        }

        var userListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
        var roleListBuilder = new CmsAuthItemList.UniqueItemListBuilder();

        while (true)
        {
            AuthNode cachedAuthNode = _cachedAuthNodes.ContainsKey(xPath) 
                ? _cachedAuthNodes[xPath] 
                : null;

            if (cachedAuthNode is not null)
            {
                foreach (CmsUser user in cachedAuthNode.Users?.Items ?? [])
                {
                    userListBuilder.Add(user.Clone());
                }

                foreach (CmsRole role in cachedAuthNode.Roles?.Items ?? [])
                {
                    roleListBuilder.Add(role.Clone());
                }
            }

            if (exactPathOnly || String.IsNullOrEmpty(xPath))
            {
                break;
            }

            int pos = xPath.LastIndexOf("/");
            if (pos == -1)
            {
                xPath = String.Empty;
            }
            else
            {
                // to authorize a property, the syntax is path+"@gdipriperities_id-of-the-item"
                var posAttribute = xPath.IndexOf("@", pos);  
                if (posAttribute > 0)
                {
                    pos = Math.Max(posAttribute, xPath.IndexOf("_", posAttribute));
                }
                xPath = xPath.Substring(0, pos);
            }
        }

        var resultAuthNode = new AuthNode(
            new CmsAuthItemList(userListBuilder.Build()),
            new CmsAuthItemList(roleListBuilder.Build()));

        // handle excluve auth items (name@@EXCLUSIVE@@)
        resultAuthNode = resultAuthNode.IgnoreAllowedIfHasExclusives()  // first step: set all allowed to false if there are exclusive authItems 
                                       .RemoveIgnoredItems();  // second step: => remove all ignored authItems

        return resultAuthNode;
    }

    public static AuthNode GetAuthNodeFast(CmsDocument cms, string xPath)
    {
        return cms.GetAuthNode(xPath);
    }

    public static AuthNode GetPropertyAuthNodeFast(CmsDocument cms, string xPath, string propertyName)
    {
        return cms.GetAuthNode($"{xPath}@{propertyName}", true);
    }

    #region Check Names / Category Prefix

    internal const string AuthCategoryPrefixSeperator = "::";

    private static string RemoveAuthNamePrefix(string authName)
    {
        int pos = authName.IndexOf(AuthCategoryPrefixSeperator);
        if (pos > 0)
        {
            authName = authName.Substring(pos + 2, authName.Length - pos - 2);
        }

        return authName;
    }

    private static bool RemoveAuthNamePrefixAndCompare(string authName, string candidate)
    {
        int prefixIndex = authName.IndexOf(AuthCategoryPrefixSeperator, StringComparison.Ordinal);

        if (prefixIndex > 0)
        {
            int startIndex = prefixIndex + AuthCategoryPrefixSeperator.Length;

            return authName.Length == startIndex + candidate.Length
                && authName.EndsWith(candidate, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    #endregion

    public static bool CheckAuthorization(UserIdentification ui, AuthNode authNode)
    {
        if (ui == null || authNode == null)
        {
            return true;
        }

        string currentUsername = ui.Username;
        string[] currentUserRoles = ui.Userroles;
        string[] currentUserInstanceRoles = ui.InstanceRoles;

        bool allowed = false;
        bool useSctAuthComparing = authNode.UseScrictAuthNameComparing;

        #region Test all allowed users

        foreach (CmsUser authNodeUser in authNode.Users.AllowedItems)
        {
            if (authNodeUser.Name.IsEveryone())
            {
                allowed = true;
                continue;
            }
            // if username ist directly allowed by username => return true
            if (currentUsername.IsEqualAuthName(authNodeUser.Name, useSctAuthComparing))
            {
                return true;
            }
        }

        #endregion

        #region Test all roles

        bool allowedFromRole = false;
        if (currentUserRoles != null)
        {
            foreach (CmsRole authNodeRole in authNode.Roles.AllowedItems)
            {
                foreach (string currentUserRole in currentUserRoles)
                {
                    if (currentUserRole.IsEqualAuthName(authNodeRole.Name, useSctAuthComparing))
                    {
                        allowedFromRole = allowed = true;
                        break;
                    }
                }
                if (allowedFromRole && allowed)
                {
                    break;
                }
            }
        }

        #endregion

        #region Test instance roles

        bool allowedFromInstance = false;
        if (!allowed && currentUserInstanceRoles != null)
        {
            foreach (CmsRole authNodeRole in authNode.Roles.AllowedItems)
            {
                foreach (string currentUserInstanceRole in currentUserInstanceRoles)
                {
                    if ($"instance::{currentUserInstanceRole}".IsEqualAuthName(authNodeRole.Name, useSctAuthComparing))
                    {
                        allowedFromInstance = allowed = true;
                        break;
                    }
                }
                if (allowed)
                {
                    break;
                }
            }
        }

        #endregion

        // if not allowed => return false
        if (!allowed)
        {
            return false;
        }

        #region Otherwise test denials

        // Directy return false, if user is a denied user
        foreach (CmsUser authNodeUser in authNode.Users.DeniedItems)
        {
            if (currentUsername.IsEqualAuthName(authNodeUser.Name, useSctAuthComparing))
            {
                return false;
            }
        }

        // Only deny from a role, if user is not in an allowed role!
        if (allowedFromRole == false && currentUserRoles != null)
        {
            foreach (CmsRole authNodeRole in authNode.Roles.DeniedItems)
            {
                foreach (string currentUserRole in currentUserRoles)
                {
                    if (currentUserRole.IsEqualAuthName(authNodeRole.Name, useSctAuthComparing))
                    {
                        return false;
                    }
                }
            }
        }

        // Only deny from instance, if user is not in an allowed instance role!
        if (allowedFromInstance == false && currentUserInstanceRoles != null)
        {
            foreach (CmsRole authNodeRole in authNode.Roles.DeniedItems)
            {
                foreach (string currentUserInstanceRole in currentUserInstanceRoles)
                {
                    if ($"instance::{currentUserInstanceRole}".IsEqualAuthName(authNodeRole.Name, useSctAuthComparing))
                    {
                        return false;
                    }
                }
            }
        }

        #endregion

        // all denied checks passed => return true
        return true;
    }

    #endregion

    public class AuthNode
    {
        private CmsAuthItemList _users;
        private CmsAuthItemList _roles;
        private bool _useStrictAuthNameComparing = false;

        public AuthNode(CmsAuthItemList users, CmsAuthItemList roles)
        {
            _users = users ?? CmsAuthItemList.Empty;
            _roles = roles ?? CmsAuthItemList.Empty;

            ReCalcUseStrictMode();
        }

        public AuthNode(XmlNode aNode)
        {
            if (aNode == null)
            {
                return;
            }

            string xPath = aNode.Attributes["value"]?.Value.ToString();

            var userListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
            var roleListBuilder = new CmsAuthItemList.UniqueItemListBuilder();

            foreach (XmlNode n in aNode.SelectNodes("user[@name and @allowed]"))
            {
                userListBuilder.Add(UserFromXmlNode(n));
            }

            foreach (XmlNode n in aNode.SelectNodes("role[@name and @allowed]"))
            {
                roleListBuilder.Add(RoleFromXmlNode(n));
            }

            _users = new CmsAuthItemList(userListBuilder.Build());
            _roles = new CmsAuthItemList(roleListBuilder.Build());

            ReCalcUseStrictMode();
        }

        public AuthNode Clone()
        {
            AuthNode clone = new AuthNode(_users.Clone(), _roles.Clone());

            return clone;
        }

        public CmsAuthItemList Users => _users;
        public CmsAuthItemList Roles => _roles;

        public bool UseScrictAuthNameComparing => _useStrictAuthNameComparing;

        public void Append(AuthNode authNode)
        {
            if (authNode?.Users?.Count > 0)
            {
                var userListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
                userListBuilder.AddRange(this.Users.Items);
                userListBuilder.AddRange(authNode.Users.Items);

                _users = new CmsAuthItemList(userListBuilder.Build());
            }

            if (authNode.Roles?.Count > 0)
            {
                var roleListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
                roleListBuilder.AddRange(this.Roles.Items);
                roleListBuilder.AddRange(authNode.Roles.Items);

                _roles = new CmsAuthItemList(roleListBuilder.Build());
            }

            ReCalcUseStrictMode();
        }

        public override bool Equals(object obj)
        {
            if (obj is AuthNode authNode)
            {
                if (_users.Count != authNode.Users.Count ||
                    _roles.Count != authNode.Roles.Count)
                {
                    return false;
                }

                foreach (var user in _users?.Items ?? [])
                {
                    var authNodeUser = authNode.Users?.Items.Where(u => u.Name == user.Name).FirstOrDefault();
                    if (authNodeUser == null || !authNodeUser.Equals(user))
                    {
                        return false;
                    }
                }

                foreach (var role in _roles?.Items ?? [])
                {
                    var authNodeRole = authNode.Roles?.Items.Where(r => r.Name == role.Name).FirstOrDefault();
                    if (authNodeRole == null || !authNodeRole.Equals(role))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region Helper

        private CmsUser UserFromXmlNode(XmlNode n)
        {
            var user = new CmsUser(n.Attributes["name"].Value, n.Attributes["allowed"].Value == "true")
            {
                Ignore = n.Attributes["ignore"]?.Value == "true",
                IsExclusive = n.Attributes["exclusive"]?.Value == "true"
            };

            return user;
        }

        private CmsRole RoleFromXmlNode(XmlNode n)
        {
            var role = new CmsRole(n.Attributes["name"].Value, n.Attributes["allowed"].Value == "true")
            {
                Ignore = n.Attributes["ignore"]?.Value == "true",
                IsExclusive = n.Attributes["exclusive"]?.Value == "true"
            };

            return role;
        }

        private void ReCalcUseStrictMode()
        {
            // if every authItem (except EverOne) has an AuthPrefix,
            // use the (faster) strict mode for this CMS

            _useStrictAuthNameComparing =
                _users.Items.Select(i => i.Name)
                            .Distinct()
                            .Where(name => !name.Equals(Everyone, StringComparison.OrdinalIgnoreCase))
                            .Any(name => name.HasNoAuthNamePrefix()) == false &&
                _roles.Items.Select(i => i.Name)
                            .Distinct()
                            .Any(name => name.HasNoAuthNamePrefix()) == false;
        }

        #endregion
    }

    public List<string> AllRoles
    {
        get
        {
            List<string> roles = new List<string>();

            #region Cms Xml Acl Nodes

            if (_aclNode != null)
            {
                foreach (XmlNode roleNode in _aclNode.SelectNodes("authnode/role[@name]"))
                {
                    roles.Add(roleNode.Attributes["name"].Value);
                }
            }

            #endregion

            return roles;
        }
    }

    public class UserIdentification
    {
        private readonly string _username;
        private string[] _userroles;
        private readonly string[] _userrolesParams;
        private readonly string[] _instanceRoles;

        public UserIdentification(string username, string[] userroles, string[] userrolesParams, string[] instanceRoles, string publicKey = "", string task = "", string userId = "", string displayName = "", string branch = "")
        {
            _username = username ?? String.Empty;
            _userroles = userroles;
            _userrolesParams = userrolesParams;
            _instanceRoles = instanceRoles;
            this.Task = task;
            this.PublicKey = publicKey;
            this.UserId = userId;
            this.DisplayName = displayName;
            this.Branch = branch;
        }

        public string Username
        {
            get { return _username; }
        }
        public string[] Userroles
        {
            get { return _userroles; }
        }
        public string[] UserrolesParameters
        {
            get { return _userrolesParams; }
        }
        public string[] InstanceRoles
        {
            get { return _instanceRoles; }
        }
        public string PublicKey { get; }

        public string UserId { get; }

        public string DisplayName { get; }

        public static UserIdentification Anonymous
        {
            get
            {
                return new UserIdentification(String.Empty, null, null, null);
            }
        }

        public static UserIdentification Create(string username)
            => new UserIdentification(username, null, null, null);

        public bool IsAnonymous { get { return String.IsNullOrWhiteSpace(_username); } }

        public string Task { get; private set; }

        public string Branch { get; private set; }

        #region Static Members

        public static void ResetUserroles(UserIdentification ui, string[] userroles)
        {
            ui._userroles = userroles;
        }

        #endregion
    }
    
    #endregion

    public CmsDocument Copy()
    {
        CmsDocument copy = new CmsDocument(this);
        return copy;
    }

    public XmlNode ToXml()
    {
        return _doc.Clone();
    }

    #region Replacing 

    public void ReplaceInXmlDocument(string path)
    {
        try
        {
            if (_doc == null)
            {
                return;
            }

            var fi = new ConfigFileInfo(path);
            if (!fi.Exists)
            {
                return;
            }

            XmlDocument replaceXml = new XmlDocument();
            replaceXml.Load(fi.FullName);

            XmlNodeList replace1 = replaceXml.SelectNodes("cms_replace/cms[@name='*']/replace[@from and @to]");
            XmlNodeList replace2 = replaceXml.SelectNodes("cms_replace/cms[@name='" + _name + "']/replace[@from and @to]");

            foreach (XmlNode node in _doc.ChildNodes)
            {
                if (replace1 != null && replace1.Count > 0)
                {
                    ReplaceInXmlDocument(node, replace1);
                }

                if (replace2 != null && replace2.Count > 0)
                {
                    ReplaceInXmlDocument(node, replace2);
                }
            }
        }
        catch
        {
        }
    }

    private void ReplaceInXmlDocument(XmlNode node, XmlNodeList replaceNodes)
    {
        if (node == null || replaceNodes == null || node.ChildNodes == null)
        {
            return;
        }

        if (node.ChildNodes.Count == 0)
        {
            if (node.Name == "_linkuri" ||
                (node is XmlText && node.ParentNode != null && node.ParentNode.Name == "_linkuri"))
            {
                // Bei Links nicht ersetzen!!!
            }
            else
            {
                #region Replace
                foreach (XmlNode replaceNode in replaceNodes)
                {
                    string from = replaceNode.Attributes["from"]?.Value;
                    string to = replaceNode.Attributes["to"]?.Value;
                    if (String.IsNullOrEmpty(from))
                    {
                        continue;
                    }

                    node.InnerText = node.InnerText.Replace(from, to);
                }
                #endregion
            }
        }
        else
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                ReplaceInXmlDocument(child, replaceNodes);
            }
        }
    }

    #endregion

    #region Helper

    private bool Compare(string cand, string val, bool fuzzy)
    {
        if (fuzzy)
        {
            return CompareFuzzy(cand, val);
        }
        else
        {
            return cand == val;
        }
    }

    static public bool CompareFuzzy(string cand, string val)
    {
        if (cand == val)
        {
            return true;
        }

        if (String.IsNullOrEmpty(cand) || String.IsNullOrEmpty(val))
        {
            return false;
        }

        cand = cand.ToLower();
        val = val.ToLower();

        if (cand.Contains(val))
        {
            return true;
        }

        if (cand.Replace("-", "").Replace(".", "").Replace(" ", "").Contains(val.Replace("-", "").Replace(".", "").Replace(" ", "")))
        {
            return true;
        }

        return false;
    }

    #endregion
}
