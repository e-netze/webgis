using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Plattform;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.CMS.Core;

[Flags]
public enum CmsNodeType
{
    Any = 0,
    Node = 1,
    File = 2,
    Link = 4,
    Template = 8
}

public partial class CMSManager
{
    //public const string WildcardAnyPlaceholder = "---wildcardany---";  // Never change this

    private readonly XmlDocument _schema;
    private readonly Encoding _encoding = Encoding.Unicode;
    private string _root = String.Empty;
    private NameValueCollection _secrets = null;

    public CMSManager(XmlDocument schema)
    {
        _schema = schema;
        if (_schema == null)
        {
            return;
        }

        #region Includes

        XmlNodeList includes = _schema.SelectNodes("//schema-template-include[@target]");
        foreach (XmlNode include in includes)
        {
            var targetName = include.Attributes["target"].Value;

            if (targetName.Contains(":"))
            {
                var targetXmlPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "",
                    "schemes",
                    $"{targetName.Substring(0, targetName.IndexOf(":"))}.xml");

                targetName = targetName.Substring(targetName.IndexOf(":") + 1);

                var schemaXml = new XmlDocument();
                schemaXml.Load(targetXmlPath);

                var cmsManager = new CMSManager(schemaXml);
                XmlNode template = cmsManager._schema.SelectSingleNode($"schema/schema-template[@name='{targetName}']");

                if (template == null)
                {
                    throw new Exception($"Schema-Template '{include.Attributes["target"].Value}' not found...");
                }

                var clone = _schema.ImportNode(template.CloneNode(true), true);
                XmlNode insertAfter = include;
                foreach (XmlNode child in clone.ChildNodes)
                {
                    include.ParentNode.InsertAfter(child, insertAfter);
                    insertAfter = child;
                }

                include.ParentNode.RemoveChild(include);
            }
            else
            {
                XmlNode template = _schema.SelectSingleNode($"schema/schema-template[@name='{targetName}']");

                if (template == null)
                {
                    throw new Exception($"Schema-Template '{include.Attributes["target"].Value}' not found...");
                }

                XmlNode clone = template.CloneNode(true);
                _schema.ImportNode(clone, true);
                XmlNode insertAfter = include;
                foreach (XmlNode child in clone.ChildNodes)
                {
                    include.ParentNode.InsertAfter(child, insertAfter);
                    insertAfter = child;
                }
                include.ParentNode.RemoveChild(include);
            }
        }

        #endregion
    }

    public CMSManager Clone(CmsItemTransistantInjectionServicePack servicePack,
                            NameValueCollection secrets = null)
    {
        var cmsManager = new CMSManager(_schema);
        cmsManager.SetConnectionString(servicePack, _root);

        cmsManager._secrets = secrets;

        return cmsManager;
    }

    public string ConnectionString
    {
        get { return _root; }
    }

    public void SetConnectionString(CmsItemTransistantInjectionServicePack servicePack, string connectionString)
    {
        _root = connectionString;

        //Console.WriteLine("set connectinstring: " + value.Substring(0, 30));

        DocumentFactory.ModifySchema(_root.ToPlattformPath(), _schema);

        var di = DocumentFactory.PathInfo(_root.ToPlattformPath());
        if (di is IDatabasePath)
        {
            ((IDatabasePath)di).CreateDatabase();
        }

        //Console.WriteLine($"{di.Parent} => exists => ${di.Exists}");

        if (!di.Exists)
        {
            di.Create();
            OpenRoot(servicePack);
        }
        else if (di.Exists &&
            di.GetDirectories().Count() == 0 &&
            di.GetFiles().Where(f => !f.Name.StartsWith("_")).Count() == 0)
        {
            OpenRoot(servicePack);
        }
    }

    public string CmsDisplayName { get; set; }

    public string CmsSchemaName { get; set; }

    #region VersionInfo
    public bool CheckVersion(CmsItemTransistantInjectionServicePack servicePack)
    {
        VersionInfoList ths = GetThisVersionInfo(servicePack);
        VersionInfoList act = ReadVersionInfo(servicePack);

        int c = ths.CompareTo(act);
        if (c < 0)
        {
            return false;
        }

        if (c > 0)
        {
            WriteVersionInfo(servicePack);
            if (ths.Minor > act.Minor)
            {
                Reload(servicePack, true);
            }
        }
        return true;
    }
    private VersionInfoList GetThisVersionInfo(CmsItemTransistantInjectionServicePack servicePack)
    {
        VersionInfoList vis = new VersionInfoList();

        vis.Add(new VersionInfo(Assembly.GetEntryAssembly()));
        vis.Add(new VersionInfo(this.GetType().Assembly));

        if (_schema != null)
        {
            foreach (XmlNode node in _schema.SelectNodes("//*[@assembly]"))
            {
                object obj = this.SchemaNodeInstance(servicePack, node);
                if (obj == null)
                {
                    continue;
                }

                vis.Add(new VersionInfo(obj.GetType().Assembly));
            }
        }
        return vis;
    }
    private VersionInfoList ReadVersionInfo(CmsItemTransistantInjectionServicePack servicePack)
    {
        var fi = DocumentFactory.DocumentInfo((ConnectionString + @"/.versioninfo.xml").ToPlattformPath());
        if (!fi.Exists)
        {
            return WriteVersionInfo(servicePack);
        }

        IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
        VersionInfoList vis = new VersionInfoList();
        vis.Load(xmlStream);

        return vis;
    }
    private VersionInfoList WriteVersionInfo(CmsItemTransistantInjectionServicePack servicePack)
    {
        VersionInfoList vis = GetThisVersionInfo(servicePack);
        IStreamDocument xmlStream = DocumentFactory.New(this.ConnectionString);
        vis.Save(xmlStream);
        xmlStream.SaveDocument(ConnectionString + @"/.versioninfo.xml");

        return vis;
    }
    #endregion

    public int CountFilesAndDirectries
    {
        get
        {
            if (String.IsNullOrEmpty(_root))
            {
                return 0;
            }

            var di = DocumentFactory.PathInfo(_root.ToPlattformPath());
            return CountNodesInDirectory(di);
        }
    }
    private int CountNodesInDirectory(IPathInfo di)
    {
        int c = di.GetFiles().Count() + di.GetDirectories().Count();

        foreach (var child in di.GetDirectories())
        {
            c += CountNodesInDirectory(child);
        }

        return c;
    }

    public void Reload(CmsItemTransistantInjectionServicePack servicePack, bool fireEvent)
    {
        if (fireEvent && BeforeReload != null)
        {
            BeforeReload(this, new EventArgs());
        }

        OpenRoot(servicePack);
    }

    async public Task<bool> DeleteCmsDatabase(IConsoleOutputStream stream = null)
    {
        var pathInfo = DocumentFactory.PathInfo(_root.ToPlattformPath());
        if (pathInfo is IDatabasePath)
        {
            return await ((IDatabasePath)pathInfo).DeleteDatabase(stream);
        }

        return false;
    }

    private Dictionary<string, bool> _isDir = new Dictionary<string, bool>();
    public XmlNode SchemaNode(string relPath, CmsNodeType nodeTypes = CmsNodeType.Any)
    {
        return SchemaNode(relPath, true, nodeTypes);
    }
    public XmlNode SchemaNode(string relPath, bool fast, CmsNodeType nodeTypes = CmsNodeType.Any)
    {
        string[] parts = relPath.ToLower().Replace(@"\", "/").Split('/');
        XmlNode schemaRoot = _schema.SelectSingleNode("schema/schema-root");

        string nameAttr = Helper.XPathToLowerCase("name");
        string path = _root;

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (String.IsNullOrEmpty(part))
            {
                continue;
            }

            path += @"/" + part;
            bool isDir = false;

            if (fast)
            {
                // Damit Filesystemzugriffe beim Export minimiert werden minimiert werden
                if (!_isDir.TryGetValue(path, out isDir))
                {
                    bool i_d = DocumentFactory.PathInfo(path).Exists, i_f = false;
                    if (!i_d)
                    {
                        i_f = DocumentFactory.DocumentInfo(path).Exists;
                    }
                    // Nur speichern wenns entweder oder ist!!!!
                    if (i_d || i_f)
                    {
                        _isDir.Add(path, isDir = i_d);
                    }
                }
            }
            else
            {
                isDir = DocumentFactory.PathInfo(path).Exists;
            }

            //if (part.IndexOf(WildcardAnyPlaceholder) > 0)
            //{
            //    part = part.Substring(0, part.IndexOf(WildcardAnyPlaceholder)) + "*";   // themes_*
            //}

            XmlNode schemaNode = (nodeTypes.UseWith(CmsNodeType.Node) || i != parts.Length - 1) ?
                schemaRoot.SelectSingleNode("schema-node[" + nameAttr + "='" + part + "']") :
                null;

            if (nodeTypes.UseWith(CmsNodeType.File) && schemaNode == null && i == parts.Length - 1)
            {
                schemaNode = schemaRoot.SelectSingleNode("schema-file[" + nameAttr + "='" + part + "']");
            }

            if (nodeTypes.UseWith(CmsNodeType.Link) && schemaNode == null && i == parts.Length - 1 && isDir == false)
            {
                schemaNode = schemaRoot.SelectSingleNode("schema-link[" + nameAttr + "='" + part + "']");
            }

            if (nodeTypes.UseWith(CmsNodeType.File) && schemaNode == null && i == parts.Length - 1 && isDir == false)
            {
                schemaNode = schemaRoot.SelectSingleNode("schema-file[" + nameAttr + "='*']");
            }

            if (nodeTypes.UseWith(CmsNodeType.Link) && schemaNode == null && i == parts.Length - 1 && isDir == false)
            {
                schemaNode = schemaRoot.SelectSingleNode("schema-link[" + nameAttr + "='*']");
            }

            if (nodeTypes.UseWith(CmsNodeType.Template) && schemaNode == null && i == parts.Length - 1 && part.ToLower() == ".linktemplate")
            {
                schemaNode = schemaRoot.SelectSingleNode("link-template");
            }

            if ((nodeTypes.UseWith(CmsNodeType.Node) || i != parts.Length - 1) && schemaNode == null)
            {
                schemaNode = schemaRoot.SelectSingleNode("schema-node[" + nameAttr + "='*']");
            }

            if (schemaNode == null)
            {
                return null;
            }

            schemaRoot = schemaNode;
        }
        return schemaRoot;
    }
    public object SchemaNodeInstance(CmsItemTransistantInjectionServicePack servicePack, string relPath, CmsNodeType nodeTypes = CmsNodeType.Any, bool existingOnly = false)
    {
        return SchemaNodeInstance(servicePack, relPath, false, false, nodeTypes, existingOnly);
    }
    public object SchemaNodeInstance(CmsItemTransistantInjectionServicePack servicePack, string relPath, bool initialize, CmsNodeType nodeTypes = CmsNodeType.Any, bool existingOnly = false)
    {
        return SchemaNodeInstance(servicePack, relPath, initialize, false, nodeTypes, existingOnly);
    }

    public object SchemaNodeInstance(CmsItemTransistantInjectionServicePack servicePack, string relPath, bool intialize, bool fast, CmsNodeType nodeTypes = CmsNodeType.Any, bool existingOnly = false)
    {
        string absolutePath = "";
        return SchemaNodeInstance(servicePack, relPath, intialize, fast, out absolutePath, nodeTypes, existingOnly);
    }

    public object SchemaNodeInstance(CmsItemTransistantInjectionServicePack servicePack, string relPath, bool initialize, bool fast, out string absoutePath, CmsNodeType nodeTypes = CmsNodeType.Any, bool existingOnly = false)
    {
        absoutePath = "";
        object obj = SchemaNodeInstance(servicePack, SchemaNode(relPath, fast, nodeTypes));
        bool exists = false;

        var cmsUIAttribute = obj?.GetType().GetCustomAttribute<CmsUIAttribute>();

        if ((obj is IPersistable && initialize) ||
            (obj is IForcePerist && ((IForcePerist)obj).AlwaysForcePersitForInstance) ||
            !String.IsNullOrEmpty(cmsUIAttribute?.PrimaryDisplayProperty))
        {
            var fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + ".xml").ToPlattformPath());
            if (!fi.Exists)
            {
                fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + ".link").ToPlattformPath());
            }

            if (!fi.Exists)
            {
                fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + @"/.general.xml").ToPlattformPath());
            }

            if (!fi.Exists)
            {
                fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + @"/general.xml").ToPlattformPath());
            }

            if (fi.Exists)
            {
                IStreamDocument doc = DocumentFactory.Open(fi.FullName, _secrets);
                ((IPersistable)obj).Load(doc);
                if (obj is ISchemaNode)
                {
                    ((ISchemaNode)obj).CmsManager = this;
                    ((ISchemaNode)obj).RelativePath = relPath;
                }
                absoutePath = fi.FullName;
                exists = true;
            }
        }

        if (existingOnly == true && exists == false)
        {
            return null;
        }

        return obj;
    }

    public bool DeleteNode(string relPath)
    {
        var schemaNode = SchemaNode(relPath);
        if (schemaNode?.Attributes["deletable"]?.Value != "true")
        {
            throw new Exception("Delete node not allowed");
        }

        var fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + ".xml").ToPlattformPath());
        if (fi.Exists)
        {
            fi.Delete();
            return true;
        }
        fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + ".link").ToPlattformPath());
        if (fi.Exists)
        {
            fi.Delete();
            return true;
        }
        var di = DocumentFactory.PathInfo((_root + @"/" + relPath).ToPlattformPath());
        if (di.Exists)
        {
            di.Delete(true);
            return true;
        }

        return false;
    }

    public object SchemaNodeInstance(CmsItemTransistantInjectionServicePack servicePack, XmlNode schemaNode)
    {
        if (schemaNode == null)
        {
            return null;
        }

        string assembly = AttributeValue(schemaNode, "assembly");
        string instance = AttributeValue(schemaNode, "instance");

        object obj = Helper.GetRelInstance(assembly, instance, servicePack);
        return obj;
    }

    public object[] SchemaNodeInstances(CmsItemTransistantInjectionServicePack servicePack, string relPath, bool initialize)
    {
        List<object> objects = new List<object>();

        var di = DocumentFactory.PathInfo((_root + @"/" + relPath).ToPlattformPath());
        foreach (var fi in di.GetFiles("*.*"))
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            string title = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            if (title.StartsWith("."))
            {
                continue;
            }

            object obj = SchemaNodeInstance(servicePack, relPath + "/" + title, initialize);
            if (obj != null)
            {
                objects.Add(obj);
            }
        }
        foreach (var sub in di.GetDirectories())
        {
            string title = sub.Name;

            if (title.StartsWith("."))
            {
                continue;
            }

            object obj = SchemaNodeInstance(servicePack, relPath + "/" + title, initialize);
            if (obj != null)
            {
                objects.Add(obj);
            }
        }

        return objects.ToArray();
    }

    public IStreamDocument XmlStream(string relPath)
    {
        var fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + ".xml").ToPlattformPath());
        if (fi.Exists)
        {
            return DocumentFactory.Open(fi.FullName);
        }

        var di = DocumentFactory.PathInfo((_root + @"/" + relPath).ToPlattformPath());
        if (di.Exists)
        {
            fi = DocumentFactory.DocumentInfo((di.FullName + @"/.general").ToPlattformPath());
            if (fi.Exists)
            {
                return DocumentFactory.Open(fi.FullName);
            }
        }
        return null;
    }

    public XmlNodeList XmlExportNodes()
    {
        if (_schema == null)
        {
            return null;
        }

        return _schema.SelectNodes("schema/xml-export[@name]");
    }

    private void OpenRoot(CmsItemTransistantInjectionServicePack servicePack)
    {
        //try
        {
            _isDir = new Dictionary<string, bool>();
            CreateSchemaNode(servicePack, Root, _schema.SelectSingleNode("schema/schema-root"), true);
            _isDir.Clear();
        }
        //catch (Exception ex)
        //{
        //    _root = String.Empty;
        //    throw ex;
        //}
    }

    #region ParseEvents
    public event EventHandler OnParseSchemaNode = null;
    public event EventHandler BeforeReload = null;

    public class ParseEventArgs : EventArgs
    {
        public string NodeName = String.Empty;
        public string FileName = String.Empty;

        public ParseEventArgs(string nodeName)
        {
            NodeName = nodeName;
        }
        public ParseEventArgs(string nodeName, string fileName)
            : this(nodeName)
        {
            FileName = fileName;
        }
    }
    #endregion

    public void ParseSchemaNode(CmsItemTransistantInjectionServicePack servicePack, IPathInfo di, XmlNode node)
    {
        CreateSchemaNode(servicePack, di, node, false);
    }

    private void CreateSchemaNode(CmsItemTransistantInjectionServicePack servicePack, IPathInfo di, XmlNode node, bool fast)
    {
        if (di == null || node == null)
        {
            return;
        }

        foreach (XmlNode child in node.ChildNodes) // .SelectNodes("schema-node[@name]"))
        {
            if (child is XmlComment)
            {
                continue;
            }

            if (child == null || child.Attributes == null)
            {
                continue;
            }

            if (child.Attributes["name"] == null &&
                child.Name != "link-template")
            {
                continue;
            }

            if (child.Name != "schema-node" &&
                child.Name != "schema-file" &&
                child.Name != "schema-link" &&
                child.Name != "link-template")
            {
                continue;
            }

            string createmode = AttributeValue(child, "createmode", "auto");
            if (AttributeValue(child, "name") == "*")  // * muss immer durch user erzeugt werden werden...
            {
                createmode = "user";
            }

            switch (createmode)
            {
                case "auto":
                    if (child.Name == "schema-node")
                    {
                        var schemaNodeDir = DocumentFactory.PathInfo((di.FullName + @"/" + AttributeValue(child, "name")).ToPlattformPath());
                        if (OnParseSchemaNode != null)
                        {
                            OnParseSchemaNode(this, new ParseEventArgs(child.Name, schemaNodeDir.FullName));
                        }

                        if (!schemaNodeDir.Exists)
                        {
                            schemaNodeDir.Create();
                        }

                        if (AttributeValue(child, "itemorder") == "true")
                        {
                            ItemOrder itemOrder = new ItemOrder(schemaNodeDir.FullName);
                            itemOrder.Save();
                        }
                        CreateSchemaNode(servicePack, schemaNodeDir, child, fast);
                    }
                    else if (child.Name == "schema-file" ||
                             child.Name == "schema-link")
                    {
                        var schemaFile = DocumentFactory.DocumentInfo((di.FullName + @"/" + AttributeValue(child, "name") +
                            ((child.Name == "schema-file") ? ".xml" : ".link")).ToPlattformPath());
                        if (OnParseSchemaNode != null)
                        {
                            OnParseSchemaNode(this, new ParseEventArgs(child.Name, schemaFile.FullName));
                        }

                        string assembly = AttributeValue(child, "assembly");
                        string instance = AttributeValue(child, "instance");
                        IPersistable obj = Helper.GetRelInstance(assembly, instance, servicePack) as IPersistable;
                        if (obj != null)
                        {
                            IStreamDocument xmlStreamOld = ((schemaFile.Exists) ? DocumentFactory.Open(schemaFile.FullName) : null);
                            if (schemaFile.Exists)
                            {
                                obj.Load(xmlStreamOld);
                            }

                            IStreamDocument xmlStreamNew = DocumentFactory.New(this.ConnectionString);
                            obj.Save(xmlStreamNew);
                            if (!xmlStreamNew.Equals(xmlStreamOld))
                            {
                                xmlStreamNew.SaveDocument(schemaFile.FullName);
                            }
                        }
                    }
                    else if (child.Name == "link-template")
                    {
                        var schemaFile = DocumentFactory.DocumentInfo((di.FullName + @"/.linktemplate.xml").ToPlattformPath());
                        if (OnParseSchemaNode != null)
                        {
                            OnParseSchemaNode(this, new ParseEventArgs(child.Name, schemaFile.FullName));
                        }

                        string assembly = AttributeValue(child, "assembly");
                        string instance = AttributeValue(child, "instance");
                        IPersistable obj = Helper.GetRelInstance(assembly, instance, servicePack) as IPersistable;
                        if (obj != null)
                        {
                            IStreamDocument xmlStreamOld = ((schemaFile.Exists) ? DocumentFactory.Open(schemaFile.FullName) : null);
                            if (schemaFile.Exists)
                            {
                                obj.Load(xmlStreamOld);
                            }
                            else if (obj is IOnCreateCmsNode)
                            {
                                ((IOnCreateCmsNode)obj).OnCreateCmsNode(di.FullName);
                            }

                            IStreamDocument xmlStreamNew = DocumentFactory.New(this.ConnectionString);
                            obj.Save(xmlStreamNew);
                            if (!xmlStreamNew.Equals(xmlStreamOld))
                            {
                                xmlStreamNew.SaveDocument(schemaFile.FullName);
                            }
                        }
                    }
                    break;
                case "user":
                    if (child.Name == "schema-node")
                    {
                        if (AttributeValue(child, "name") == "*")
                        {
                            foreach (var subDir in di.GetDirectories())
                            {
                                if (OnParseSchemaNode != null)
                                {
                                    OnParseSchemaNode(this, new ParseEventArgs(child.Name, subDir.FullName));
                                }

                                string relPath = subDir.FullName.Substring(_root.Length + 1, subDir.FullName.Length - _root.Length - 1);
                                XmlNode schemaNode = SchemaNode(relPath, fast);
                                if (schemaNode != child)
                                {
                                    continue;
                                }

                                if (AttributeValue(child, "itemorder") == "true")
                                {
                                    ItemOrder itemOrder = new ItemOrder(subDir.FullName);
                                    itemOrder.Save();
                                }

                                IPersistable obj = SchemaNodeInstance(servicePack, schemaNode) as IPersistable;
                                if (obj != null && obj is ICreatable)
                                {
                                    var subFile = DocumentFactory.DocumentInfo((subDir.FullName + @"/" + ((ICreatable)obj).CreateAs(false) + ".xml").ToPlattformPath());
                                    IStreamDocument xmlStreamOld = ((subFile.Exists) ? DocumentFactory.Open(subFile.FullName) : null);
                                    if (subFile.Exists)
                                    {
                                        obj.Load(xmlStreamOld);
                                    }
                                    else if (obj is IOnCreateCmsNode)
                                    {
                                        ((IOnCreateCmsNode)obj).OnCreateCmsNode(di.FullName);
                                    }

                                    IStreamDocument xmlStreamNew = DocumentFactory.New(this.ConnectionString);
                                    obj.Save(xmlStreamNew);
                                    if (!xmlStreamNew.Equals(xmlStreamOld))
                                    {
                                        xmlStreamNew.SaveDocument(subFile.FullName);
                                    }
                                }
                                CreateSchemaNode(servicePack, subDir, child, false);
                            }
                        }
                        else
                        {
                            var schemaNodeDir2 = DocumentFactory.PathInfo((di.FullName + @"/" + AttributeValue(child, "name")).ToPlattformPath());
                            if (schemaNodeDir2.Exists)
                            {
                                CreateSchemaNode(servicePack, schemaNodeDir2, child, fast);
                            }
                        }
                    }
                    else if (child.Name == "schema-file" ||
                             child.Name == "schema-link")
                    {
                        if (AttributeValue(child, "name") == "*")
                        {
                            foreach (var subFile in di.GetFiles())
                            {
                                if (subFile.Extension.ToLower() == ".acl")
                                {
                                    continue;
                                }

                                if (subFile.Name.StartsWith("."))
                                {
                                    continue;
                                }
                                //if (subFile.Name.ToLower() == ".itemorder.xml" ||
                                //    subFile.Name.ToLower()==".general.xml") continue;
                                if (OnParseSchemaNode != null)
                                {
                                    OnParseSchemaNode(this, new ParseEventArgs(child.Name, subFile.FullName));
                                }

                                string relPath = subFile.FullName.Substring(_root.Length + 1, subFile.FullName.Length - subFile.Extension.Length - _root.Length - 1);
                                XmlNode schemaNode = SchemaNode(relPath, fast);
                                if (schemaNode != child)
                                {
                                    continue;
                                }

                                string assembly = AttributeValue(child, "assembly");
                                string instance = AttributeValue(child, "instance");
                                IPersistable obj = Helper.GetRelInstance(assembly, instance, servicePack) as IPersistable;
                                if (obj != null)
                                {
                                    IStreamDocument xmlStreamOld = ((subFile.Exists) ? DocumentFactory.Open(subFile.FullName) : null);
                                    if (subFile.Exists)
                                    {
                                        obj.Load(xmlStreamOld);
                                    }
                                    else if (obj is IOnCreateCmsNode)
                                    {
                                        ((IOnCreateCmsNode)obj).OnCreateCmsNode(di.FullName);
                                    }

                                    IStreamDocument xmlStreamNew = DocumentFactory.New(this.ConnectionString);
                                    obj.Save(xmlStreamNew);
                                    if (!xmlStreamNew.Equals(xmlStreamOld))
                                    {
                                        xmlStreamNew.SaveDocument(subFile.FullName);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // gibts das?
                        }
                    }
                    break;
            }
        }

        if (OnParseSchemaNode != null)
        {
            OnParseSchemaNode(this, null);
        }
    }

    private IPathInfo Root
    {
        get
        {
            return Node(null);
        }
    }
    private IPathInfo Node(string subPath)
    {
        IPathInfo di = null;
        if (String.IsNullOrEmpty(subPath))
        {
            di = DocumentFactory.PathInfo(_root.ToPlattformPath());
        }
        else
        {
            di = DocumentFactory.PathInfo((_root + @"/" + subPath).ToPlattformPath());
        }
        if (di != null && !di.Exists)
        {
            di.Create();
        }

        return di;
    }

    private IEnumerable<IPathInfo> GetSubNodes(IPathInfo di)
    {
        return di.GetDirectories();
    }
    private IEnumerable<IDocumentInfo> GetConfigs(IPathInfo di)
    {
        return di.GetFiles();
    }

    public XmlDocument ToXml(CmsItemTransistantInjectionServicePack servicePack,
                             bool appendConfigs, bool recursive)
    {
        return ToXml(servicePack, _root, appendConfigs, null, null, recursive);
    }
    public XmlDocument ToXml(CmsItemTransistantInjectionServicePack servicePack,
                             bool appendConfigs, string[] filters, bool recursive)
    {
        return ToXml(servicePack, _root, appendConfigs, filters, null, recursive);
    }
    public XmlDocument ToXml(CmsItemTransistantInjectionServicePack servicePack,
                             bool appendConfigs, string[] filters, string[] uriFilters, bool recursive)
    {
        return ToXml(servicePack, _root, appendConfigs, filters, uriFilters, recursive);
    }
    public XmlDocument ToXml(CmsItemTransistantInjectionServicePack servicePack,
                             string root, bool appendConfigs, bool recursive)
    {
        return ToXml(servicePack, root, appendConfigs, null, null, recursive);
    }
    public XmlDocument ToXml(CmsItemTransistantInjectionServicePack servicePack,
                             string root, bool appendConfigs, string[] filters, string[] uriFilters, bool recursive)
    {
        if (String.IsNullOrEmpty(root))
        {
            return null;
        }

        while (root.EndsWith("/"))
        {
            root = root.Substring(0, root.Length - 1);
        }

        StringBuilder xml = new StringBuilder();
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

        //xWriter.Formatting=Formatting.Indented;
        xWriter.WriteStartDocument();

        xWriter.WriteStartElement("CMS");
        xWriter.WriteAttributeString("root", root);
        AppendDirectory(servicePack, root, xWriter, DocumentFactory.PathInfo(root.ToPlattformPath()), appendConfigs, filters, uriFilters, String.Empty, recursive);
        xWriter.WriteEndElement(); //CMS
        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        xml.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml.ToString());

        return doc;
    }

    #region Warnings
    public event EventHandler OnParseWaring = null;
    public List<Warning> Warnings()
    {
        List<Warning> warnings = new List<Warning>();
        Warnings(DocumentFactory.PathInfo(_root.ToPlattformPath()), warnings);

        return warnings;
    }
    private void Warnings(IPathInfo di, List<Warning> warnings)
    {
        //var dt = DateTime.UtcNow;
        var directories = di.GetDirectories();
        //Console.WriteLine($"GetDirectories {di.FullName} [{directories.Count()}]:{(int)(DateTime.UtcNow - dt).TotalMilliseconds}ms");

        foreach (var subDir in directories)
        {
            Warnings(subDir, warnings);
        }

        //dt = DateTime.UtcNow;
        var files = di.GetFiles();
        //Console.WriteLine($"GetFiles {di.FullName} [{files.Count()}]:{(int)(DateTime.UtcNow - dt).TotalMilliseconds}ms");

        XmlNode schemaNode = null;

        foreach (var fi in files)
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            if (OnParseWaring != null)
            {
                OnParseWaring(this, new ParseEventArgs(String.Empty, fi.FullName));
            }

            try
            {
                var doc = new XmlDocumentWrapper();
                doc.Load(fi.FullName);

                XmlNode linkUri = doc.SelectSingleNode("config/_linkuri");
                if (linkUri != null)
                {
                    string link = (_root + "/" + linkUri.InnerText).ToPlattformPath();
                    string linkFilename = String.Empty;

                    bool exists = false;

                    if ((DocumentFactory.PathInfo(link)).Exists)
                    {
                        exists = true;
                    }
                    else if ((DocumentFactory.DocumentInfo(linkFilename = (link + ".xml").ToPlattformPath())).Exists)
                    {
                        exists = true;
                    }
                    else if ((DocumentFactory.DocumentInfo(linkFilename = (link + ".link").ToPlattformPath())).Exists)
                    {
                        exists = true;
                    }

                    if (!exists)
                    {
                        warnings.Add(new Warning(fi.FullName.Substring(_root.Length + 1).ToLower(), "Link zeigt ins Leere"));
                    }

                    var path = di.FullName.Substring(_root.Length + 1).ToLower();
                    schemaNode = schemaNode ?? this.SchemaNode(path);
                    if (!schemaNode.IsLinkTargetValid(path, linkUri.InnerText))
                    {
                        string targetName = String.Empty;
                        if (!String.IsNullOrEmpty(linkFilename))
                        {
                            try
                            {
                                var targetDoc = new XmlDocumentWrapper();
                                targetDoc.Load(linkFilename);
                                targetName = targetDoc.SelectSingleNode("config/name")?.InnerText;
                            }
                            catch { }
                        }
                        warnings.Add(new Warning($"{path} => {linkUri.InnerText} ({targetName})", "Link ist ungültig") { Level = Warning.WaringLevel.Warning });
                    }
                }
            }
            catch { }
        }
    }

    public class Warning
    {
        public enum WaringLevel
        {
            Critical,
            Warning
        }

        public string Path;
        public string Message;
        public WaringLevel Level;

        public Warning(string path, string message)
        {
            Path = path;
            Message = message;
            Level = WaringLevel.Critical;
        }
    }
    #endregion

    #region Search

    public IEnumerable<string> SearchTerm(string term)
    {
        List<string> nodes = new List<string>();
        SearchTerm(DocumentFactory.PathInfo(_root.ToPlattformPath()), term.ToLower(), nodes);
        return nodes.Select(n => n.StartsWith("/") ? n.Substring(1) : n).ToArray();
    }

    private void SearchTerm(IPathInfo di, string term, List<string> items)
    {
        string path = di.FullName.Substring(_root.Length).ToLower();
        if (path.Contains(term))
        {
            items.Add(path.Replace("\\", "/"));
        }

        foreach (var subDir in di.GetDirectories())
        {
            SearchTerm(subDir, term, items);
        }

        foreach (var fi in di.GetFiles())
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith(".") && fi.Name != ".general.xml")
            {
                continue;
            }

            try
            {
                string text = File.ReadAllText(fi.FullName);
                if (text.ToLower().Contains(term))
                {
                    if (fi.Name == ".general.xml")
                    {
                        items.Add(fi.FullName.Substring(_root.Length, fi.FullName.Length - _root.Length - "/.general.xml".Length).Replace("\\", "/"));
                    }
                    else
                    {
                        items.Add(fi.FullName.Substring(_root.Length, fi.FullName.Length - _root.Length - fi.Extension.Length).Replace("\\", "/"));
                    }
                }
            }
            catch { }
        }
    }

    #endregion

    #region Scan for Links
    public List<string> ScanForLinkParents(string uriPath)
    {
        List<string> links = new List<string>();
        ScanForLinkParents(DocumentFactory.PathInfo(_root.ToPlattformPath()), uriPath, links);
        return links;
    }
    private void ScanForLinkParents(IPathInfo di, string uriPath, List<string> links)
    {
        foreach (var subDir in di.GetDirectories())
        {
            ScanForLinkParents(subDir, uriPath, links);
        }

        foreach (var fi in di.GetFiles())
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fi.FullName);

                XmlNode linkUri = doc.SelectSingleNode("config/_linkuri");
                if (linkUri != null)
                {
                    string link = linkUri.InnerText;

                    if (link == uriPath || uriPath == "*")
                    {
                        string linkParent = fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - fi.Name.Length - 2);
                        links.Add(linkParent.Replace("\\", "/").ToLower());

                        //string relPath = fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - 1);
                        //object obj = this.SchemaNodeInstance(relPath, true);

                        //if (obj is ISchemaNode)
                        //{
                        //    links.Add(((ISchemaNode)obj).RelativePath);
                        //}
                    }
                }
            }
            catch { }
        }
    }

    public List<string> ScanForLinks(string path)
    {
        List<string> links = new List<string>();
        ScanForLinks(DocumentFactory.PathInfo((_root + @"/" + path).ToPlattformPath()), links);
        return links;
    }
    private void ScanForLinks(IPathInfo di, List<string> links)
    {
        foreach (var subDir in di.GetDirectories())
        {
            ScanForLinks(subDir, links);
        }

        foreach (var fi in di.GetFiles())
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fi.FullName);

                XmlNode linkUri = doc.SelectSingleNode("config/_linkuri");
                if (linkUri != null)
                {
                    string link = fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - fi.Extension.Length - 1);
                    links.Add(link.Replace("\\", "/").ToLower());
                }
            }
            catch { }
        }
    }
    #endregion

    public void ThinPath(string path, List<string> uris, string filter)
    {
        foreach (var fi in (DocumentFactory.PathInfo(path.ToPlattformPath()).GetFiles(filter)))
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            string uri = fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - 1 - fi.Extension.Length);
            uri = uri.Replace("\\", "/");

            bool found = false;
            foreach (string u in uris)
            {
                if (u.ToLower().Replace("\\", "/") == uri.ToLower())
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                fi.Delete();
            }
        }
    }

    public int CountConfigFiles(string path, string filter)
    {
        int counter = 0;
        foreach (var fi in (DocumentFactory.PathInfo(path.ToPlattformPath()).GetFiles(filter)))
        {
            if (fi.Extension.ToLower() == ".acl")
            {
                continue;
            }

            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            counter++;
        }
        return counter;
    }

    public event EventHandler OnAppendXmlItem = null;
    private void AppendDirectory(CmsItemTransistantInjectionServicePack servicePack,
                                 string root, XmlTextWriter xWriter, IPathInfo parent, bool appendConfigs, string[] filters, string[] uriFilters, string parentUri, bool recursive)
    {
        XmlNode schemaNode = null;

        if (filters != null &&
            parent.FullName.Length > root.Length)
        {
            try
            {
                schemaNode = this.SchemaNode(parent.FullName.Substring(root.Length + 1, parent.FullName.Length - root.Length - 1), true);
                if (!TestFilters(schemaNode, filters))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                string e = ex.Message;
            }
        }
        ItemOrder itemOrder = new ItemOrder(parent.FullName);
        foreach (string item in itemOrder.Items)
        {
            var fi = DocumentFactory.DocumentInfo((parent.FullName + @"/" + item).ToPlattformPath());
            if (fi.Exists)
            {
                try
                {
                    schemaNode = this.SchemaNode(fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - 1), true);
                    if (filters != null)
                    {
                        if (!TestFilters(schemaNode, filters))
                        {
                            continue;
                        }
                    }
                    string title = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                    if (OnAppendXmlItem != null)
                    {
                        OnAppendXmlItem(this, new ParseEventArgs(String.Empty, fi.FullName));
                    }

                    xWriter.WriteStartElement("item");
                    xWriter.WriteAttributeString("name", title);
                    xWriter.WriteAttributeString("displayname", DisplayName(servicePack, fi));
                    if (schemaNode != null)
                    {
                        if (schemaNode.Attributes["filtertype"] != null)
                        {
                            xWriter.WriteAttributeString("filtertype", schemaNode.Attributes["filtertype"].Value);
                        }

                        if (schemaNode.Attributes["itemorder"]?.Value == "true")
                        {
                            xWriter.WriteAttributeString("itemorder", "true");
                        }

                        if (schemaNode.Attributes["obsolete"]?.Value == "true")
                        {
                            xWriter.WriteAttributeString("obsolete", "true");
                        }
                    }

                    if (fi.Extension.ToLower() == ".link")
                    {
                        xWriter.WriteAttributeString("type", "link");
                        var doc = new XmlDocumentWrapper();
                        doc.Load(fi.FullName);
                        XmlNode target = doc.SelectSingleNode("config/_linkuri");
                        // Target ist immer toLower() zu setzen, weil alle Filenamen auch ToLower() gesetzt werden...
                        xWriter.WriteAttributeString("target", (target == null ? String.Empty : target.InnerText.ToLower()));
                    }
                    else
                    {
                        xWriter.WriteAttributeString("type", "file");
                    }

                    xWriter.WriteAttributeString("extension", fi.Extension);
                    if (appendConfigs)
                    {
                        StreamReader sr = new StreamReader(fi.FullName);
                        xWriter.WriteCData(sr.ReadToEnd());
                        sr.Close();
                    }
                    xWriter.WriteEndElement();
                }
                catch (Exception ex)
                {
                    throw new Exception($"File {fi.FullName}: {ex.Message}");
                }
            }
            else
            {
                var di = DocumentFactory.PathInfo((parent.FullName + @"/" + item).ToPlattformPath());
                if (di.Exists)
                {
                    try
                    {
                        string uri = String.IsNullOrEmpty(parentUri) ? di.Name : parentUri + "/" + di.Name;

                        schemaNode = this.SchemaNode(di.FullName.Substring(_root.Length + 1, di.FullName.Length - _root.Length - 1), true);
                        if (filters != null)
                        {
                            if (!TestFilters(schemaNode, filters))
                            {
                                continue;
                            }
                        }
                        if (uriFilters != null)
                        {
                            bool found = false;
                            foreach (string uriFilter in uriFilters)
                            {
                                if (uriFilter.ToLower().StartsWith(uri) || uri.ToLower().StartsWith(uriFilter))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                continue;
                            }
                        }

                        if (OnAppendXmlItem != null)
                        {
                            OnAppendXmlItem(this, new ParseEventArgs(String.Empty, di.FullName));
                        }

                        xWriter.WriteStartElement("item");
                        xWriter.WriteAttributeString("name", di.Name);
                        xWriter.WriteAttributeString("displayname", DisplayName(servicePack, di));
                        if (schemaNode != null)
                        {
                            if (schemaNode.Attributes["filtertype"] != null)
                            {
                                xWriter.WriteAttributeString("filtertype", schemaNode.Attributes["filtertype"].Value);
                            }

                            if (schemaNode.Attributes["itemorder"]?.Value == "true")
                            {
                                xWriter.WriteAttributeString("itemorder", "true");
                            }

                            if (schemaNode.Attributes["obsolete"]?.Value == "true")
                            {
                                xWriter.WriteAttributeString("obsolete", "true");
                            }
                        }

                        xWriter.WriteAttributeString("type", "directory");
                        if (recursive)
                        {
                            #region Bei Filter kann mit der Recursion aufgehört werden, wenn Filtertype gefunden wurde (Beschleunigt suche...)
                            bool found = false;
                            if (filters != null && schemaNode != null && schemaNode.Attributes["filtertype"] != null)
                            {
                                foreach (string filter in filters)
                                {
                                    if (schemaNode.Attributes["filtertype"].Value == filter)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            #endregion
                            if (!found)
                            {
                                AppendDirectory(servicePack, root, xWriter, di, appendConfigs, filters, uriFilters, uri, recursive);
                            }
                        }
                        else
                        {
                            if (di.GetDirectories().Count() > 0 || di.GetFiles().Where(f => !f.Name.StartsWith(".")).Count() > 0)
                            {
                                xWriter.WriteAttributeString("haschild", "true");
                            }
                        }
                        xWriter.WriteEndElement(); // item

                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Folder {di.FullName}:{Environment.NewLine}{ex.FullMessage()}");
                    }
                }
            }
        }
    }

    public NodeProperties GetSchemaNodeProperties(string path)
    {
        var schemaNode = SchemaNode(path, true);

        string assembly = schemaNode?.Attributes["assembly"]?.Value;
        string instance = schemaNode?.Attributes["instance"]?.Value;

        return new NodeProperties()
        {
            HasProperties = !String.IsNullOrWhiteSpace(assembly) && !String.IsNullOrWhiteSpace(instance),
            IsDeletable = schemaNode?.Attributes["deletable"]?.Value == "true",
            IsRequired = schemaNode?.Attributes["required"]?.Value == "true",
            IsRecommended = schemaNode?.Attributes["recommended"]?.Value == "true",
            IsObsolete = schemaNode?.Attributes["obsolete"]?.Value == "true",
            Description = schemaNode?.Attributes["description"]?.Value,
            VisibleIf = schemaNode?.Attributes["visible_if"]?.Value
        };
    }

    public class NodeProperties
    {
        public bool HasProperties { get; set; }
        public bool IsDeletable { get; set; }
        public bool IsRequired { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsObsolete { get; set; }
        public string Description { get; set; }
        public string VisibleIf { get; set; }
    }

    private bool TestFilters(XmlNode schemaNode, string[] filters)
    {
        if (schemaNode == null || filters == null)
        {
            return true;
        }

        foreach (string filter in filters)
        {
            if (String.IsNullOrEmpty(filter))
            {
                return true;
            }

            if (TestFilter(schemaNode, filter))
            {
                return true;
            }
        }
        return false;
    }
    private bool TestFilter(XmlNode schemaNode, string filter)
    {
        if (schemaNode == null ||
            schemaNode.Attributes == null)
        {
            return false;
        }

        if (schemaNode.Attributes["filtertype"] != null &&
            schemaNode.Attributes["filtertype"].Value == filter)
        {
            return true;
        }

        foreach (XmlNode child in schemaNode.ChildNodes)
        {
            if (TestFilter(child, filter))
            {
                return true;
            }
        }
        return false;
    }

    private string DisplayName(CmsItemTransistantInjectionServicePack servicePack, IDocumentInfo fi)
    {
        if (fi.Extension.ToLower() == ".acl")
        {
            return String.Empty;
        }

        if (fi.Name.ToLower() == "general.xml")
        {
            return "general";
        }

        if (fi.Name.ToLower() == ".general.xml")
        {
            return ".general";
        }

        if (fi.Name.ToLower() == ".versioninfo.xml")
        {
            return ".versioninfo";
        }

        string title = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

        string relPath = String.Empty;
        if (fi.Extension.ToLower() == ".link")
        {
            //Link link = new Link();
            //link.Load(DocumentFactory.Open(fi.FullName));
            //relPath = link.LinkUri;

            relPath = fi.Directory.FullName.Substring(_root.Length + 1, fi.Directory.FullName.Length - _root.Length - 1) + "/" + title;
            var linkSchemaNode = SchemaNode(relPath);
            var linkDisplayName = DisplayName(servicePack, fi, linkSchemaNode, out object linkInstance) ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(linkDisplayName) && !linkDisplayName.Contains("{0}"))
            {
                return linkDisplayName;
            }
            else
            {
                if (linkInstance is Link)
                {
                    relPath = ((Link)linkInstance).LinkUri;

                    string absPath = _root + @"\" + relPath;
                    var lfi = DocumentFactory.DocumentInfo((absPath + ".xml").ToPlattformPath());

                    string targetDisplayName;
                    if (lfi.Exists)
                    {
                        targetDisplayName = DisplayName(servicePack, lfi);
                    }
                    else
                    {

                        var targetDi = DocumentFactory.PathInfo(absPath.ToPlattformPath());
                        targetDisplayName = DisplayName(servicePack, targetDi);
                    }

                    if (linkDisplayName.Contains("{0}"))
                    {
                        targetDisplayName = String.Format(linkDisplayName, targetDisplayName);
                    }

                    return targetDisplayName;
                }
            }
        }
        else
        {
            relPath = fi.Directory.FullName.Substring(_root.Length + 1, fi.Directory.FullName.Length - _root.Length - 1) + "/" + title;
        }

        XmlNode schemaNode = SchemaNode(relPath);
        string displayName = DisplayName(servicePack, fi, schemaNode, out object nodeInstance);
        if (String.IsNullOrEmpty(displayName) && nodeInstance is IDisplayNameDefault)
        {
            displayName = ((IDisplayNameDefault)nodeInstance).DefaultDisplayName;
        }
        return displayName;
    }

    private string DisplayName(CmsItemTransistantInjectionServicePack servicePack, IPathInfo di)
    {
        string relPath = di.FullName.Substring(_root.Length + 1, di.FullName.Length - _root.Length - 1);
        XmlNode schemaNode = SchemaNode(relPath);

        if (!String.IsNullOrWhiteSpace(schemaNode?.Attributes["displayname"]?.Value))
        {
            return schemaNode.Attributes["displayname"].Value;
        }

        var fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + @"/.general.xml").ToPlattformPath());
        if (!fi.Exists)
        {
            fi = DocumentFactory.DocumentInfo((_root + @"/" + relPath + @"/general.xml").ToPlattformPath());
        }

        if (!fi.Exists)
        {
            return di.Name;
        }

        return DisplayName(servicePack, fi, schemaNode, out object nodeInstance);
    }
    private string DisplayName(CmsItemTransistantInjectionServicePack servicePack, IDocumentInfo fi, XmlNode schemaNode, out object nodeInstance)
    {
        nodeInstance = null;

        string title = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
        if (!fi.Exists || schemaNode == null)
        {
            return title;
        }

        string assembly = AttributeValue(schemaNode, "assembly");
        string instance = AttributeValue(schemaNode, "instance");
        if (!String.IsNullOrEmpty(assembly) && !String.IsNullOrEmpty(instance))
        {
            nodeInstance = Helper.GetRelInstance(assembly, instance, servicePack);
            IDisplayName obj = nodeInstance as IDisplayName;

            if (nodeInstance is Link && obj == null)
            {
                title = String.Empty;
            }
            if (nodeInstance is IPersistable)
            {
                ((IPersistable)nodeInstance).Load(DocumentFactory.Open(fi.FullName));
                if (obj != null)
                {
                    title = obj.DisplayName;
                }
            }
        }
        return title;
    }

    #region Export
    public event EventHandler OnExportNode = null;
    public event EventHandler OnMessage = null;

    async public Task<XmlDocument> Export(CmsItemTransistantInjectionServicePack servicePack,
                                          bool ignoreAuthentification = false,
                                          ParseEncryptedValue onParseBeforeEncryptValue = null)
    {
        _isDir = new Dictionary<string, bool>();

        List<ExportAuthNode> authNodes = new List<ExportAuthNode>();
        ExportAppendAcl(this.Root + @"\root.acl", authNodes);

        var xmlStream = (XmlFileStreamDocument)DocumentFactory.New(String.Empty/*this.ConnectionString*/);  // DoTo: soll auch für andere Typen funktioneren -> Hardcoded Casting

        if (onParseBeforeEncryptValue != null)
        {
            xmlStream.OnParseBeforeEncryptValue += onParseBeforeEncryptValue;
        }

        ExportDirectory(servicePack, xmlStream, this.Root, authNodes);

        #region Authentification

        if (ignoreAuthentification)
        {
            authNodes.Clear();
            // Wenn kein rootAuth Node -> Jeder darf standardmaßig alles
            authNodes.Add(new ExportAuthNode(String.Empty, new CmsUser(CmsDocument.Everyone, true)));
        }
        else
        {
            bool hasRootAuthNode = false;
            foreach (ExportAuthNode authNode in authNodes)
            {
                if (authNode.AuthPath == String.Empty)
                {
                    hasRootAuthNode = true;
                    break;
                }
            }
            if (!hasRootAuthNode)  // Wenn kein rootAuth Node -> Jeder darf standardmaßig alles
            {
                authNodes.Add(new ExportAuthNode(String.Empty, new CmsUser(CmsDocument.Everyone, true)));
            }
        }

        if (authNodes.Count > 0 && xmlStream.ParentXmlNode != null)
        {
            XmlNode aclNode = xmlStream.ParentXmlNode.OwnerDocument.CreateElement("acl");
            xmlStream.ParentXmlNode.AppendChild(aclNode);
            foreach (ExportAuthNode exportAuthNode in authNodes)
            {
                await exportAuthNode.Save(aclNode);
            }
        }

        #endregion

        return xmlStream.XmlDocument();
    }

    public void ExportDirectory(CmsItemTransistantInjectionServicePack servicePack, IStreamDocument xmlStream, IPathInfo parent, List<ExportAuthNode> authNodes)
    {
        string relPath;

        if (parent.FullName.ToLower().EqualPath(_root.ToLower()))
        {
            relPath = String.Empty;
        }
        else
        {
            relPath = parent.FullName.Substring(_root.Length + 1, parent.FullName.Length - _root.Length - 1).ToLower();
        }

        ItemOrder itemOrder = new ItemOrder(parent.FullName, true);
        foreach (string item in itemOrder.Items)
        {
            var fi = DocumentFactory.DocumentInfo((parent.FullName + @"/" + item).ToPlattformPath());
            if (fi.Exists)
            {
                string title = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length).ToLower();
                string itemName = title;

                string aclFilename = fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + ".acl";
                ExportAppendAcl(aclFilename, authNodes);

                foreach (var aclPropFile in parent.GetFiles(title + "@*.acl"))
                {
                    ExportAppendAcl(aclPropFile.FullName, authNodes, true);
                }

                if (itemName.StartsWith(".") &&
                    itemName.ToLower() != ".linktemplate")
                {
                    // .linktemplate schreiben! wichtig für GDI, wo nur Dienste parameteriert werden!
                    continue;
                }

                XmlNode schemaNode = SchemaNode(relPath + "/" + title, true);
                if (schemaNode == null)
                {
                    continue;
                }

                if (itemName.ToLower() == ".linktemplate")
                {
                    itemName = AttributeValue(schemaNode, "saveas");
                    if (String.IsNullOrEmpty(itemName))
                    {
                        continue;
                    }
                }
                //if (schemaNode.Attributes["itemname"] != null)
                //    itemName = schemaNode.Attributes["itemname"].Value;

                if (OnExportNode != null)
                {
                    OnExportNode(this, new ParseEventArgs(schemaNode.Name, fi.FullName));
                }

                string assembly = AttributeValue(schemaNode, "assembly");
                string instance = AttributeValue(schemaNode, "instance");
                IPersistable obj = null;
                if (String.IsNullOrEmpty(assembly) &&
                    String.IsNullOrEmpty(instance) &&
                    schemaNode.Name == "schema-link")
                {
                    obj = new Link();
                }
                else
                {
                    obj = Helper.GetRelInstance(assembly, instance, servicePack) as IPersistable;
                }
                if (obj != null)
                {
                    obj.Load(DocumentFactory.Open(fi.FullName));
                    if (obj is Link)
                    {
                        Link lnk = (Link)obj;
                        title = lnk.LinkUri;
                    }
                    if (!xmlStream.SetParent(relPath + "/" + itemName))
                    {
                        throw new Exception("Can't generate Xml-Node: " + relPath);
                    }

                    obj.Save(xmlStream);
                }
            }
            else
            {
                var di = DocumentFactory.PathInfo((parent.FullName + @"/" + item).ToPlattformPath());

                string aclFilename = di.FullName + ".acl";
                ExportAppendAcl(aclFilename, authNodes);
                foreach (var aclPropFile in parent.GetFiles(item + "@*.acl"))
                {
                    ExportAppendAcl(aclPropFile.FullName, authNodes, true);
                }

                if (di.Exists)
                {
                    string title = di.Name.ToLower();
                    string itemName = title;

                    XmlNode schemaNode = SchemaNode(relPath + "/" + title, true);
                    if (schemaNode == null || schemaNode.Attributes["deploy"]?.Value == "false")
                    {
                        continue;
                    }

                    //if (schemaNode.Attributes["itemname"] != null)
                    //    itemName = schemaNode.Attributes["itemname"].Value;

                    var general = DocumentFactory.DocumentInfo((di.FullName + @"/.general.xml").ToPlattformPath());
                    if (general.Exists)
                    {

                        if (OnExportNode != null)
                        {
                            OnExportNode(this, new ParseEventArgs(schemaNode.Name, di.FullName));
                        }

                        string assembly = AttributeValue(schemaNode, "assembly");
                        string instance = AttributeValue(schemaNode, "instance");
                        IPersistable obj = Helper.GetRelInstance(assembly, instance, servicePack) as IPersistable;
                        if (obj != null)
                        {
                            if (!xmlStream.SetParent(relPath + "/" + itemName))
                            {
                                throw new Exception("Can't generate Xml-Node: " + relPath);
                            }

                            obj.Load(DocumentFactory.Open(general.FullName));
                            obj.Save(xmlStream);
                        }
                    }
                    ExportDirectory(servicePack, xmlStream, di, authNodes);
                }
            }
        }
    }

    public void ExportAppendAcl(string aclFilename, List<ExportAuthNode> authNodes)
    {
        ExportAppendAcl(aclFilename, authNodes, false);
    }
    private void ExportAppendAcl(string aclFilename, List<ExportAuthNode> authNodes, bool isPropAuthNode)
    {
        if (authNodes == null)
        {
            return;
        }

        var fi = DocumentFactory.DocumentInfo(aclFilename.ToPlattformPath());
        if (fi.Exists)
        {
            string authPath = fi.FullName.Substring(_root.Length + 1, fi.FullName.Length - _root.Length - fi.Extension.Length - 1).ToLower().Replace("\\", "/");
            if (authPath == "root")
            {
                authPath = String.Empty;
            }

            //if (isPropAuthNode)
            //{
            //    int pos1 = authPath.LastIndexOf("/");
            //    if (pos1 == -1) pos1 = 0;
            //    int pos2 = authPath.IndexOf("_", pos1);
            //    if (pos2 == -1)
            //        return;

            //    authPath = authPath.Substring(0, pos2) + "/" + authPath.Substring(pos2 + 1, authPath.Length - pos2 - 1);
            //}

            string xml = fi.ReadAll();
            if (!String.IsNullOrWhiteSpace(xml))
            {
                authNodes.Add(new ExportAuthNode(this, authPath, xml));
            }
        }
    }
    public class ExportAuthNode
    {
        public string AuthPath = String.Empty;
        public NodeAuthorization NodeAuth = null;
        private readonly CMSManager _cmsManager;

        public ExportAuthNode(CMSManager cmsManager, string path, string xml)
        {
            _cmsManager = cmsManager;

            AuthPath = path;
            NodeAuth = new NodeAuthorization();
            var userListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
            var roleListBuilder = new CmsAuthItemList.UniqueItemListBuilder();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                foreach (XmlNode userNode in doc.SelectNodes("acl/user[@name and @allowed]"))
                {
                    userListBuilder.Add(new CmsUser(userNode.Attributes["name"].Value, userNode.Attributes["allowed"].Value.ToLower() == "true"));
                }

                foreach (XmlNode roleNode in doc.SelectNodes("acl/role[@name and @allowed]"))
                {
                    roleListBuilder.Add(new CmsRole(roleNode.Attributes["name"].Value, roleNode.Attributes["allowed"].Value.ToLower() == "true"));
                }

            }
            catch { }

            NodeAuth.Users = new CmsAuthItemList(userListBuilder.Build());
            NodeAuth.Roles = new CmsAuthItemList(roleListBuilder.Build());

        }
        public ExportAuthNode(string path, CmsUser user)
        {
            AuthPath = path;
            NodeAuth = new NodeAuthorization();
            NodeAuth.Users = new CmsAuthItemList([user]);
        }

        async public Task Save(XmlNode parentNode)
        {
            if (parentNode == null || parentNode.OwnerDocument == null)
            {
                return;
            }

            XmlNode authNode = parentNode.OwnerDocument.CreateElement("authnode");
            XmlAttribute attr = parentNode.OwnerDocument.CreateAttribute("value");
            attr.Value = AuthPath;
            authNode.Attributes.Append(attr);

            //var users = NodeAuth.Users;
            //var roles = NodeAuth.Roles;

            var users = new CmsAuthItemList.UniqueItemListBuilder();
            var roles = new CmsAuthItemList.UniqueItemListBuilder();
            users.AddRange(NodeAuth.Users.Items);
            roles.AddRange(NodeAuth.Roles.Items);

            if (CmsDocument.UseAuthExclusives && NodeAuth.HasExclusiveRestriction)
            {
                var fullNode = await _cmsManager.GetNodeAuthorization(this.AuthPath);

                _cmsManager.OnMessage?.Invoke(_cmsManager, new ParseEventArgs("Modify exclusive authorization", this.AuthPath));

                #region Exclusive Benutzer übernehmen

                users.Clear();
                users.AddRange(
                    NodeAuth.Users.Items
                         .Where(u => u.HasExclusivePostfix())
                         .Select(u => u.RemoveExclusivePostfixAndSetProperty()));

                users.ForEeach(user =>
                {
                    _cmsManager.OnMessage?.Invoke(_cmsManager, new ParseEventArgs($"exclusive {(user.Allowed ? "allow" : "deny")} user", user.Name));
                });

                roles.Clear();
                roles.AddRange(
                    NodeAuth.Roles.Items
                         .Where(r => r.HasExclusivePostfix())
                         .Select(r => r.RemoveExclusivePostfixAndSetProperty()));

                roles.ForEeach(role =>
                {
                    _cmsManager.OnMessage?.Invoke(_cmsManager, new ParseEventArgs($"exclusive {(role.Allowed ? "allow" : "deny")} role", role.Name));
                });

                #endregion

                #region aller Vererbten und gesetzten user außschließen 
                // (ignore == true) werden beim CheckAuthnode im WebGIS nicht mehr berücksichtigt
                // Müssen aber trotzdem gesetzt werden, damit die Vererbung aufgebrochen wird

                // Nur User/Rollen mit allowed=true auf ignored setzen. 
                // => Ausschließungen sollten trotzdem erhalten bleiben

                // ist eigentlich obsolet, weil in der API dann für jeden AuthNode IgnoreAllowedIfHasExclusives
                // aufgerufen wird, und somit alle nicht exklusiven Knoten auf ignore gesetzt werden.
                // Dieser Schritt wird noch aus Kompatibilitätsgründen gemacht: In älteren Versionen
                // wurde ReduceToExclusives nicht aufgerufen und auch das "exclusive" Attribute im XML gesetht.
                // => man sollte aber trotzdem noch auf alte (zB stable) version publiziern können und 
                // das gleiche Ergebnis erhalten.

                foreach (var user in fullNode.Users.Items.Where(u => u.IsAllowedAndNotExclusive()))
                {
                    if (users.Any(u => u.Name.Equals(user.Name)) == false)
                    {
                        users.Add(new CmsUser(user.Name, false) { Ignore = true });
                        _cmsManager.OnMessage?.Invoke(_cmsManager, new ParseEventArgs("ignore user", user.Name));
                    }
                }

                foreach (var role in fullNode.Roles.Items.Where(r => r.IsAllowedAndNotExclusive()))
                {
                    if (roles.Any(r => r.Name.Equals(role.Name)) == false)
                    {
                        roles.Add(new CmsUser(role.Name, false) { Ignore = true });
                        _cmsManager.OnMessage?.Invoke(_cmsManager, new ParseEventArgs("ignore role", role.Name));
                    }
                }

                #endregion
            }

            foreach (CmsAuthItem user in users.Build())
            {
                XmlNode userNode = parentNode.OwnerDocument.CreateElement("user");

                attr = parentNode.OwnerDocument.CreateAttribute("name");
                attr.Value = user.Name;
                userNode.Attributes.Append(attr);

                attr = parentNode.OwnerDocument.CreateAttribute("allowed");
                attr.Value = user.Allowed.ToString().ToLower();
                userNode.Attributes.Append(attr);

                if (user.Ignore == true)
                {
                    attr = parentNode.OwnerDocument.CreateAttribute("ignore");
                    attr.Value = user.Ignore.ToString().ToLower();
                    userNode.Attributes.Append(attr);
                }

                if (user.IsExclusive == true)
                {
                    attr = parentNode.OwnerDocument.CreateAttribute("exclusive");
                    attr.Value = user.IsExclusive.ToString().ToLower();
                    userNode.Attributes.Append(attr);
                }

                authNode.AppendChild(userNode);
            }

            foreach (CmsAuthItem role in roles.Build())
            {
                XmlNode roleNode = parentNode.OwnerDocument.CreateElement("role");

                attr = parentNode.OwnerDocument.CreateAttribute("name");
                attr.Value = role.Name;
                roleNode.Attributes.Append(attr);

                attr = parentNode.OwnerDocument.CreateAttribute("allowed");
                attr.Value = role.Allowed.ToString().ToLower();
                roleNode.Attributes.Append(attr);

                if (role.Ignore == true)
                {
                    attr = parentNode.OwnerDocument.CreateAttribute("ignore");
                    attr.Value = role.Ignore.ToString().ToLower();
                    roleNode.Attributes.Append(attr);
                }

                if (role.IsExclusive == true)
                {
                    attr = parentNode.OwnerDocument.CreateAttribute("exclusive");
                    attr.Value = role.IsExclusive.ToString().ToLower();
                    roleNode.Attributes.Append(attr);
                }

                authNode.AppendChild(roleNode);
            }

            parentNode.AppendChild(authNode);
        }
    }
    #endregion

    #region Helper
    private string AttributeValue(XmlNode node, string attribute)
    {
        return AttributeValue(node, attribute, String.Empty);
    }
    private string AttributeValue(XmlNode node, string attribute, string defValue)
    {
        if (node == null || node.Attributes[attribute] == null)
        {
            return defValue;
        }

        return node.Attributes[attribute].Value;
    }
    #endregion

    private ICmsApplicationSettings _settings = null;
    public ICmsApplicationSettings ApplicationSettings
    {
        get { return _settings; }
        set { _settings = value; }
    }

    #region UserManagement

    public class NodeAuthorization
    {
        public CmsAuthItemList Users = new CmsAuthItemList([]);
        public CmsAuthItemList Roles = new CmsAuthItemList([]);

        public string TargetAclPath { get; set; }

        public bool HasRestrictions
        {
            get
            {
                return (this.Users != null && this.Users.Items.Any(u => u.Allowed == false)) ||
                       (this.Roles != null && this.Roles.Items.Any(r => r.Allowed == false));
            }
        }

        public bool HasExclusiveRestriction
        {
            get
            {
                return (this.Users != null && this.Users.Items.Any(u => u.HasExclusivePostfix())) ||
                       (this.Roles != null && this.Roles.Items.Any(r => r.HasExclusivePostfix()));
            }
        }

        public IEnumerable<string> UserAndRoleNamesHiddenByExclusives()
        {
            List<string> ret = new List<string>();

            if (HasExclusiveRestriction)
            {
                if (this.Users != null)
                {
                    ret.AddRange(this.Users.Items.Where(u => u.IsAllowedAndNotExclusive()).Select(u => u.Name));
                }

                if (this.Roles != null)
                {
                    ret.AddRange(this.Roles.Items.Where(r => r.IsAllowedAndNotExclusive()).Select(r => r.Name));
                }
            }

            return ret;
        }

        public IEnumerable<string> ExclusiveUserAndRoleNames()
        {
            List<string> ret = new List<string>();

            if (HasExclusiveRestriction)
            {
                if (this.Users != null)
                {
                    ret.AddRange(this.Users.Items.Where(u => u.HasExclusivePostfix()).Select(u => u.Name));
                }

                if (this.Roles != null)
                {
                    ret.AddRange(this.Roles.Items.Where(r => r.HasExclusivePostfix()).Select(r => r.Name));
                }
            }

            return ret;
        }

        public bool HasSecurity
        {
            get
            {
                return (this.Users != null && this.Users.Items.Where(u => String.IsNullOrEmpty(u.InheritFrom)).Count() > 0) ||
                       (this.Roles != null && this.Roles.Items.Where(r => String.IsNullOrEmpty(r.InheritFrom)).Count() > 0);
            }
        }
    }

    async public Task<NodeAuthorization> GetNodeAuthorization(string relPath, string propertyTagName = "", Func<NodeAuthorization, string, string, Task> onLoaded = null)
    {
        NodeAuthorization auth = new NodeAuthorization();

        if (String.IsNullOrEmpty(relPath))
        {
            return auth;
        }

        relPath = relPath.Replace("\\", "/");
        string nodePath = relPath;

        if (!String.IsNullOrWhiteSpace(propertyTagName))
        {
            relPath = relPath.TrimRightRelativeCmsPath(1) + "/" + relPath.TrimLeftRelativeCmsPath(1) + "@" + propertyTagName;
        }

        auth.TargetAclPath = relPath;
        bool first = true;
        var userListBuilder = new CmsAuthItemList.UniqueItemListBuilder();
        var roleListBuilder = new CmsAuthItemList.UniqueItemListBuilder();

        while (!String.IsNullOrEmpty(relPath))
        {
            GetAclUsers(relPath, userListBuilder, roleListBuilder, !first);
            if (onLoaded != null)
            {
                await onLoaded(auth, nodePath, !first ? relPath : "");
            }

            first = false;

            int pos = relPath.LastIndexOf("/");
            if (pos == -1)
            {
                break;
            }
            relPath = relPath.Substring(0, pos);
        }

        GetAclUsers("root", userListBuilder, roleListBuilder, !first);
        if (onLoaded != null)
        {
            await onLoaded(auth, nodePath, !first ? "root" : "");
        }

        auth.Users = new CmsAuthItemList(userListBuilder.Build());
        auth.Roles = new CmsAuthItemList(roleListBuilder.Build());

        return auth;
    }

    private void GetAclUsers(string aclPath, CmsAuthItemList.UniqueItemListBuilder users, CmsAuthItemList.UniqueItemListBuilder roles, bool storeInherit)
    {
        try
        {
            var acl = DocumentFactory.DocumentInfo((_root + @"/" + aclPath + ".acl").ToPlattformPath());
            if (acl.Exists)
            {
                string xml = acl.ReadAll();
                XmlDocument doc = new XmlDocument();
                //doc.Load(acl.FullName);
                doc.LoadXml(xml);

                foreach (XmlNode userNode in doc.SelectNodes("acl/user[@name and @allowed]"))
                {
                    CmsUser u = new CmsUser(userNode.Attributes["name"].Value,
                                     userNode.Attributes["allowed"].Value.ToLower() == "true",
                                     (storeInherit ? aclPath : String.Empty));

                    users.Add(u);
                }

                foreach (XmlNode userNode in doc.SelectNodes("acl/role[@name and @allowed]"))
                {
                    CmsRole r = new CmsRole(userNode.Attributes["name"].Value,
                                     userNode.Attributes["allowed"].Value.ToLower() == "true",
                                     (storeInherit ? aclPath : String.Empty));

                    roles.Add(r);
                }
            }
            else if (aclPath.ToLower() == "root")  // Wenn kein rootAcl existiert darf vom Root her immer jeder alles
            {
                users.Add(new CmsUser(CmsDocument.Everyone, true, "root"));
            }
        }
        catch
        {
        }
    }

    #endregion
}
