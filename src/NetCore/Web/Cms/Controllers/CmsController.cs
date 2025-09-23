using Cms.AppCode.Extensions;
using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using Cms.Models.Elastic;
using Cms.Models.Json;
using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Core.UI;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.Schema.Reflection;
using E.Standard.CMS.UI;
using E.Standard.CMS.UI.Controls;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Cms.Controllers;

[ApplicationSecurity]
public class CmsController : ApplicationSecurityController
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICryptoService _crypto;
    private readonly IHttpService _http;
    private readonly ICmsLogger _cmsLogger;
    private readonly IEnumerable<ICustomCmsSecurityService> _customSecurityServices;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public CmsController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            ICryptoService crypto,
            IHttpService http,
            CmsItemInjectionPackService instanceService,
            ICmsLogger cmsLogger,
            IEnumerable<ICustomCmsSecurityService> customCmsSecurityServices,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _ccs = ccs;
        _crypto = crypto;
        _http = http;
        _cmsLogger = cmsLogger;

        _servicePack = instanceService.ServicePack;
        _customSecurityServices = customCmsSecurityServices;
    }

    public IActionResult Index(string id = "")
    {
        try
        {
            XmlDocument doc = _ccs.CMS[id].ToXml(_servicePack, false, false);
            XmlNode rootNode = doc.SelectSingleNode("CMS[@root]");

            return View(new CmsModel()
            {
                CmsId = id,
                RootName = DocumentFactory.PathInfo(rootNode.Attributes["root"].Value).Name,
                CanImport = DocumentFactory.CanImport(rootNode.Attributes["root"].Value),
                CanClear = DocumentFactory.CanImport(rootNode.Attributes["root"].Value)
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Navigtion

    async public Task<IActionResult> ChildNodes(string path, string id = "", string copyNodePath = "")
    {
        try
        {
            path = path ?? String.Empty;
            while (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            string subPath = String.Empty;
            XmlDocument doc = null, parentDoc = null;
            XmlNode rootNode = null;

            List<NavItem> navItems = new List<NavItem>();

            string cmsDisplayName = _ccs.CMS[id].CmsDisplayName;

            if (!String.IsNullOrWhiteSpace(path))
            {
                doc = _ccs.CMS[id].ToXml(_servicePack, _ccs.CMS[id].ConnectionString + "/" + subPath, true, false);
                rootNode = doc.SelectSingleNode("CMS[@root]");

                navItems.Add(new NavItem(/*DocumentFactory.PathInfo(rootNode.Attributes["root"].Value).Name*/cmsDisplayName, String.Empty));
            }
            foreach (var pathPart in path.Split('/'))
            {
                if (!String.IsNullOrWhiteSpace(subPath))
                {
                    subPath += "/";
                }

                subPath += pathPart;

                doc = _ccs.CMS[id].ToXml(_servicePack, _ccs.CMS[id].ConnectionString + "/" + subPath, false, false);
                rootNode = doc.SelectSingleNode("CMS[@root]");

                if (String.IsNullOrWhiteSpace(subPath))
                {
                    navItems.Add(new NavItem(/*DocumentFactory.PathInfo(rootNode.Attributes["root"].Value).Name*/cmsDisplayName, String.Empty));
                }
                else
                {
                    string name = DocumentFactory.PathInfo(rootNode.Attributes["root"].Value).Name;
                    bool? obsolete = null;
                    if (parentDoc != null)
                    {
                        var itemNode = parentDoc.SelectSingleNode("CMS/item[@displayname and @name='" + name + "']");
                        if (itemNode != null)
                        {
                            name = itemNode.Attributes["displayname"].Value;
                            obsolete = itemNode.Attributes["obsolete"]?.Value == "true" ? true : null;
                        }
                    }
                    navItems.Add(new NavItem(_ccs.Translate(id, name), subPath, obsolete));
                }

                parentDoc = doc;
            }

            //ItemOrder itemOrder = new ItemOrder(CmsGlobals.CMS[id].RootDirectory + @"/" + path);
            //bool itemOrderExists = itemOrder.Exists;
            var schemaNode = _ccs.CMS[id].SchemaNode(path);
            bool itemsOrderable = schemaNode.Attributes["itemorder"]?.Value == "true";

            List<Node> nodes = new List<Node>();

            object parentSchemaNodeInstance = _ccs.CMS[id].GetSchemaNodeProperties(path).HasProperties ?
                _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, initialize: true) :
                null;

            foreach (XmlNode xmlNode in rootNode.ChildNodes)
            {
                if (xmlNode.Attributes["name"] == null ||
                    xmlNode.Attributes["type"] == null)
                {
                    continue;
                }

                string name = xmlNode.Attributes["name"].Value;
                if (name.StartsWith(".") &&
                    name.ToLower() != ".linktemplate") // Versteckte Dateien
                {
                    continue;
                }

                string aliasName = _ccs.Translate(id, xmlNode.Attributes["displayname"].Value.Replace("_", " "));

                var schemaNodeProperties = _ccs.CMS[id].GetSchemaNodeProperties(path + "/" + name);

                if (schemaNodeProperties?.VisibleIf?.CheckCondition(parentSchemaNodeInstance) == false)
                {
                    continue;
                }

                switch (xmlNode.Attributes["type"].Value)
                {
                    case "directory":
                        var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path + "/" + name);
                        var node = new Node(name, aliasName, path + "/" + name)
                        {
                            HasChildren = true,
                            HasProperties = schemaNodeProperties.HasProperties,
                            IsDeletable = schemaNodeProperties.IsDeletable,
                            IsRefreshable = instance is IRefreshable,
                            IsCopyable = instance is ICopyable,
                            Obsolete = xmlNode.Attributes["obsolete"]?.Value == "true" || schemaNodeProperties.IsObsolete ? true : null,
                            IsRequired = schemaNodeProperties.IsRequired == true ? true : null,
                            IsRecommended = schemaNodeProperties.IsRecommended == true ? true : null,
                            HasContent = xmlNode.Attributes["haschild"]?.Value == "true",
                            PrimaryPropertyValue = instance.CmsUIPrimaryDisplayPropertyValue()
                        };

                        #region Add Aphapetic

                        if (itemsOrderable)
                        {
                            nodes.Add(node);
                        }
                        else
                        {
                            bool added = false;
                            for (int i = 0; i < nodes.Count; i++)
                            {
                                if (node.Obsolete.HasValue && !nodes[i].Obsolete.HasValue)
                                {
                                    continue;
                                }

                                if (aliasName.CompareTo(nodes[i].AliasName) < 0)
                                {
                                    nodes.Insert(i, node);
                                    added = true;
                                    break;
                                }
                            }
                            if (!added)
                            {
                                nodes.Add(node);
                            }
                        }

                        #endregion

                        break;
                    case "link":
                        string target = (xmlNode.Attributes["target"] != null ? xmlNode.Attributes["target"].Value : String.Empty);

                        if (String.IsNullOrWhiteSpace(aliasName))
                        {
                            var targetInstance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, target, true, true);
                            if (targetInstance is IDisplayName && !String.IsNullOrWhiteSpace(((IDisplayName)targetInstance).DisplayName))
                            {
                                aliasName = ((IDisplayName)targetInstance).DisplayName;
                            }
                            else if (targetInstance is NameUrl)
                            {
                                aliasName = ((NameUrl)targetInstance).Name;
                            }
                        }
                        nodes.Add(new Node(name, aliasName, path + "/" + name)
                        {
                            Target = target,
                            IsTargetValid = schemaNode.IsLinkTargetValid(path, target),
                            HasProperties = schemaNodeProperties.HasProperties,
                            IsDeletable = schemaNodeProperties.IsDeletable,
                            Obsolete = xmlNode.Attributes["obsolete"]?.Value == "true" || schemaNodeProperties.IsObsolete ? true : null
                        });
                        break;
                    case "file":
                        var fileInstance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path + "/" + name);
                        nodes.Add(new Node(name, aliasName, path + "/" + name)
                        {
                            HasProperties = schemaNodeProperties.HasProperties,
                            IsDeletable = schemaNodeProperties.IsDeletable,
                            Obsolete = xmlNode.Attributes["obsolete"]?.Value == "true" || schemaNodeProperties.IsObsolete ? true : null,
                            IsCopyable = fileInstance is ICopyable,
                            PrimaryPropertyValue = fileInstance.CmsUIPrimaryDisplayPropertyValue()
                        });
                        //ConfigListViewItem lvConfig = new ConfigListViewItem(name, xmlNode.Attributes["extension"].Value, aliasName);
                        //tnRoot.FileItems.Add(lvConfig);
                        break;
                }
            }

            if (/*_ccs.CMS[id].GetSchemaNodeProperties(path).HasProperties*/parentSchemaNodeInstance != null)
            {
                nodes.Insert(0, new Node(".", navItems.Last().Name + " (Eigenschaften)", path));
            }

            #region Tools

            List<NodeTool> nodeTools = new List<NodeTool>();
            foreach (var creatable in CreateableSchemaNodes(schemaNode))
            {
                nodeTools.Add(new NodeTool()
                {
                    Action = "new",
                    Name = creatable.Attributes["name"]?.Value,
                    Prompt = creatable.Attributes["prompt"]?.Value ?? "Neues Element...",
                    Path = path
                });
            }
            foreach (var linkable in LinkableSchemaNodes(schemaNode))
            {
                nodeTools.Add(new NodeTool()
                {
                    Action = "link",
                    Name = linkable.Attributes["name"]?.Value,
                    Prompt = linkable.Attributes["prompt"]?.Value ?? "Element(e) hinzufügen...",
                    Path = path
                });
            }
            if (!String.IsNullOrEmpty(copyNodePath))
            {
                try
                {
                    var copyPaths = String.IsNullOrWhiteSpace(copyNodePath) ? null : JSerializer.Deserialize<string[]>(copyNodePath);

                    foreach (var copyPath in copyPaths)
                    {
                        string targetpath = copyPath, action = "paste";

                        if (copyPath.StartsWith("+"))
                        {
                            targetpath = copyPath.Substring(1);
                            action = "paste";
                        }
                        else if (copyPath.StartsWith("-"))
                        {
                            targetpath = copyPath.Substring(1);
                            action = "cut";
                        }

                        var copySchemaNode = _ccs.CMS[id].SchemaNode(targetpath);
                        if (copySchemaNode != null)
                        {
                            foreach (var childSchemaNode in schemaNode.ChildNodes)
                            {
                                if (childSchemaNode is XmlElement &&
                                    ((XmlElement)childSchemaNode).Attributes != null &&
                                    ((XmlElement)childSchemaNode).Attributes["filtertype"] != null &&
                                    ((XmlElement)childSchemaNode).Attributes["filtertype"].Value == copySchemaNode.Attributes["filtertype"]?.Value)
                                {
                                    var copySchemaInstance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, targetpath, true, true, existingOnly: true) as NameUrl;
                                    if (copySchemaInstance != null)
                                    {
                                        nodeTools.Add(new NodeTool()
                                        {
                                            Action = action,
                                            Name = targetpath,
                                            Prompt = copySchemaInstance.Name,
                                            Path = path
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            #endregion

            #region Description

            string descriptionPath = _ccs.CMS[id].GetSchemaNodeProperties(path)?.Description;
            string description = String.Empty;

            if (!String.IsNullOrWhiteSpace(descriptionPath))
            {
                try
                {
                    var fi = new System.IO.FileInfo("schemes/" + _ccs.CMS[id].CmsSchemaName + "/" + descriptionPath);
                    if (fi.Exists)
                    {
                        description = System.IO.File.ReadAllText(fi.FullName);
                    }
                }
                catch { }
            }

            #endregion

            foreach (var node in nodes)
            {
                var auth = await _ccs.CMS[id].GetNodeAuthorization(node.Path, onLoaded: async (auth, authNodePath, inheritPath) =>
                {
                    foreach (var customSecurityService in _customSecurityServices)
                    {
                        await customSecurityService.OnAclFileLoaded(id, String.IsNullOrEmpty(inheritPath) ? authNodePath : inheritPath, auth, inheritPath);
                    }
                });
                node.HasSecurity = auth.HasSecurity;
                node.HasSecurityRestrictions = auth.HasRestrictions;
                node.HasSecurityExclusiveRestrictions = auth.HasExclusiveRestriction;
            }

            return Json(new NodeCollection()
            {
                Nodes = nodes,
                Orderable = itemsOrderable,
                NavItems = navItems,
                NodeTools = nodeTools,
                Path = path,
                Description = description
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #endregion

    #region Edit

    private const string TypePropertyName = "__type__";

    public IActionResult NodeProperties(string path, string subProperty = "", string id = "", string data = "")
    {
        try
        {
            var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, true, true);
            if (instance == null)
            {
                throw new Exception("Can't initialize node instance");
            }

            string displayName = null;
            if (!String.IsNullOrWhiteSpace(subProperty))
            {
                var subProperties = subProperty.Split('.');
                for (int s = 0; s < subProperties.Length; s++)
                {
                    if (s == subProperties.Length - 1)  // Properties from last instance
                    {
                        var propertyGridData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
                        LoadPropertyGridData(instance, propertyGridData);
                    }

                    var subPropertyInfo = instance.GetType().GetProperty(subProperties[s]);
                    if (subPropertyInfo == null)
                    {
                        throw new Exception("Unknown sub property '" + subProperties[s] + "'");
                    }

                    instance = subPropertyInfo.GetValue(instance);

                    var displayNameAttribute = subPropertyInfo.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
                    if (displayNameAttribute != null)
                    {
                        displayName = displayNameAttribute.LocalizedDisplayName();
                    }
                }
            }

            List<NodeProperty> nodeProperties = new List<NodeProperty>();
            nodeProperties.Add(new NodeProperty()
            {
                Name = TypePropertyName,
                Value = instance.GetType().ToFullTypeName(),
                IsHidden = true
            });

            foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
            {
                var browsable = propertyInfo.GetCustomAttribute<BrowsableAttribute>();
                if (browsable != null && browsable.Browsable == false)
                {
                    continue;
                }

                var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
                var categoryAttribute = propertyInfo.GetCustomAttribute<CategoryAttribute>();
                var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
                var editorAttribute = propertyInfo.GetCustomAttribute<EditorAttribute>();
                var readOnlyAttribute = propertyInfo.GetCustomAttribute<ReadOnlyAttribute>();
                var passwordAttribute = propertyInfo.GetCustomAttribute<PasswordPropertyTextAttribute>();
                var secretAttribute = propertyInfo.GetCustomAttribute<SecretPropertyAttribute>();
                var authAttribute = propertyInfo.GetCustomAttribute<AuthorizablePropertyAttribute>();
                var obsoleteAttribute = propertyInfo.GetCustomAttribute<ObsoleteCmsPropepertyAttribute>();

                object val = propertyInfo.GetValue(instance);

                var editProperty = new NodeProperty()
                {
                    Name = propertyInfo.Name,
                    DisplayName = displayNameAttribute?.LocalizedDisplayName() ?? propertyInfo.Name,
                    Category = categoryAttribute?.Category ?? "Allgemein",
                    Description = descriptionAttribute?.Description ?? String.Empty,
                    ReadOnly = (propertyInfo.CanWrite == false) || (readOnlyAttribute != null && readOnlyAttribute.IsReadOnly == true),
                    HasEditor = editorAttribute != null,
                    IsPassword = (passwordAttribute != null && passwordAttribute.Password == true) ? true : null,
                    IsSecret = secretAttribute != null ? true : null,
                    AuthTagName = authAttribute != null ? authAttribute.TagName : null,
                    IsObsolote = obsoleteAttribute != null ? true : null
                };

                if (propertyInfo.PropertyType.IsEnum)
                {
                    editProperty.Value = propertyInfo.GetValue(instance)?.ToString();
                    editProperty.DomainValues = Enum.GetNames(propertyInfo.PropertyType);
                }
                else if (propertyInfo.PropertyType.IsArray)
                {
                    if (!propertyInfo.PropertyType.GetElementType().IsValueTypeOrString())
                    {
                        editProperty.Value = PropertyComplexValueToString(val);  //val?.ToString();
                        editProperty.IsComplexProperty = true;
                    }
                    else
                    {
                        editProperty.Value = val;
                    }
                }
                else if (!propertyInfo.PropertyType.IsValueTypeOrString())
                {
                    editProperty.Value = PropertyComplexValueToString(val); // val?.ToString();
                    editProperty.IsComplexProperty = true;
                }
                else
                {
                    editProperty.Value = propertyInfo.GetValue(instance);
                }

                nodeProperties.Add(editProperty);
            }

            return Json(new NodeProperties()
            {
                DisplayName = displayName ?? (instance is IDisplayName ? ((IDisplayName)instance).DisplayName : subProperty),
                Properties = nodeProperties,
                Path = path,
                SubProperty = subProperty,
                IsReadonly = instance?.GetType().GetCustomAttribute<ReadOnlyAttribute>() != null ? true : null
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult NodePropertiesCommit(string path, string data, string subProperty = "", string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "NodeProperties", "Commit",
                           id, path);

            string absolutePath = "";
            var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, true, true, out absolutePath);
            if (instance == null)
            {
                throw new Exception("Can't initialize node instance");
            }

            if (!String.IsNullOrWhiteSpace(subProperty))
            {
                var subProperties = subProperty.Split('.');
                for (int s = 0; s < subProperties.Length; s++)
                {
                    var subPropertyInfo = instance.GetType().GetProperty(subProperties[s]);
                    if (subPropertyInfo == null)
                    {
                        throw new Exception("Unknown sub property '" + subProperties[s] + "'");
                    }

                    instance = subPropertyInfo.GetValue(instance);
                }

                var propertyGridData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);

                var typeNameValue = propertyGridData.Where(p => p.Name == TypePropertyName).FirstOrDefault();
                if (typeNameValue != null && typeNameValue.Value != instance.GetType().ToFullTypeName())
                {
                    // zb bei Tabellen spalten kann es vorkommen, dass die Typen nicht zusammen passen. Wenn der Anwender da den FeldType (Feld, Hotlink, ...) ändert.
                    // Kann kommt aus dem nocht gespeicherten Knoten ein falscher Type daher. 
                    // In dem Fall versuchen eine neuen Instanz des eigentlich gewünschten Typs zu erzeugen.

                    var instanceType = Type.GetType(typeNameValue.Value);
                    instance = instanceType.CmsCreateInstance(_servicePack);
                }

                LoadPropertyGridData(instance, propertyGridData);

                return Json(new
                {
                    value = PropertyComplexValueToString(instance)
                });
            }
            else if (!String.IsNullOrWhiteSpace(absolutePath))
            {
                var propertyGridData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
                LoadPropertyGridData(instance, propertyGridData);

                if (instance is IPersistable)
                {
                    IPersistable persitable = (IPersistable)instance;

                    if (persitable is ISchemaNode)
                    {
                        ((ISchemaNode)persitable).CmsManager = _ccs.CMS[id];
                        ((ISchemaNode)persitable).RelativePath = path;
                    }

                    if (persitable is IAutoCreatable)
                    {
                        ((IAutoCreatable)persitable).AutoCreate();
                    }
                    else
                    {
                        IStreamDocument xmlStream = DocumentFactory.New(absolutePath);
                        persitable.Save(xmlStream);
                        string fullName = absolutePath;
                        xmlStream.SaveDocument(fullName);
                    }

                    return Json(new { success = true });
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult NodeDelete(string path, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Node", "Delete", id, path);

            return Json(new { success = _ccs.CMS[id].DeleteNode(path) });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult NodeOrder(string path, string nodes, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Node", "Order", id, path);

            string[] nodePaths = JSerializer.Deserialize<string[]>(nodes);

            List<string> order = new List<string>();
            foreach (string nodePath in nodePaths)
            {
                string absolutePath;
                var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, nodePath, true, true, out absolutePath);

                var fi = DocumentFactory.DocumentInfo(absolutePath);
                if (fi.Exists)
                {
                    if (fi.Name.StartsWith(".") || fi.Name == "general.xml")
                    {
                        order.Add(fi.Directory.Name);
                    }
                    else
                    {
                        order.Add(fi.Name);
                    }
                }
                else
                {
                    var di = DocumentFactory.PathInfo(absolutePath);
                    if (di.Exists)
                    {
                        order.Add(di.Name);
                    }
                    else
                    {
                        throw new Exception("Can't assign path " + nodePath);
                    }
                }
            }

            ItemOrder itemOrder = new ItemOrder(_ccs.CMS[id].ConnectionString + "/" + path, order.ToArray());
            itemOrder.Save();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    async public Task<IActionResult> NodeRefresh(string path, int level, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Node", "Refresh", id, path);

            string configPath;
            var obj = _ccs.CMS[id].Clone(_servicePack,
                                         _ccs.GetCmsSecrets(_servicePack, id))
                                  .SchemaNodeInstance(_servicePack, path, true, true, out configPath) as IRefreshable;
            if (obj != null)
            {
                if (obj is ISchemaNode)
                {
                    ((ISchemaNode)obj).CmsManager = _ccs.CMS[id];
                    ((ISchemaNode)obj).RelativePath = path;
                }

                await obj.RefreshAsync(configPath, level);
            }

            return Json(new { success = true });
        }
        catch (RefreshConfirmException rcex)
        {
            return Json(new { success = false, confirm = rcex.Message, level = level });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Security

    async public Task<IActionResult> NodeSecurity(string path, string tagName = "", string id = "")
    {
        try
        {
            var cms = _ccs.CMS[id];

            var nodeAuth = await cms.GetNodeAuthorization(path, tagName, async (auth, authNodePath, inheritPath) =>
            {
                foreach (var customSecurityService in _customSecurityServices)
                {
                    await customSecurityService.OnAclFileLoaded(id, String.IsNullOrEmpty(inheritPath) ? authNodePath : inheritPath, auth, inheritPath);
                }
            });


            var control = new SecurityControl(_http, _crypto, _customSecurityServices, nodeAuth);
            control.SetWebgisInstance(_ccs.Instance.WebGisPortalInstance);
            List<UIStyleSetter> uiStyleSetters = new List<UIStyleSetter>(new UIStyleSetter[]

            {
                    new UIStyleSetter()
                    {
                        Selector=$"li[data-path='{ path }'] .node-security",
                        ClassName="node-has-security",
                        Append=nodeAuth.HasSecurity
                    },
                    new UIStyleSetter()
                    {
                        Selector=$"li[data-path='{ path }'] .node-security",
                        ClassName="node-has-security-restrictions",
                        Append=nodeAuth.HasRestrictions
                    },
                    new UIStyleSetter()
                    {
                        Selector=$"li[data-path='{ path }'] .node-security",
                        ClassName="node-has-security-exclusive-restrictions",
                        Append=nodeAuth.HasExclusiveRestriction
                    }
            });

            foreach (var name in nodeAuth.UserAndRoleNamesHiddenByExclusives())
            {
                uiStyleSetters.Add(new UIStyleSetter()
                {
                    Selector = $"tr[data-control-name='{name}'].cms-form-table",
                    ClassName = "hidden-from-exclusives",
                    Append = true
                });
            }

            foreach (var name in nodeAuth.ExclusiveUserAndRoleNames())
            {
                uiStyleSetters.Add(new UIStyleSetter()
                {
                    Selector = $"tr[data-control-name='{name}'].cms-form-table",
                    ClassName = "exclusive",
                    Append = true
                });
            }

            return Json(new
            {
                displayName = "Knoten Security",
                path = nodeAuth.TargetAclPath,
                name = "~",
                controls = new IUIControl[] { control },
                styleSetters = uiStyleSetters,
                buttonClickMethod = "NodeSecurityCommit",
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    async public Task<IActionResult> NodeSecurityCommit(string path, string name, string btn, string data, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "NodeSecurity", "Commit", id, path);

            var cms = _ccs.CMS[id];

            var nodeAuth = await cms.GetNodeAuthorization(path, onLoaded: async (auth, authNodePath, inheritPath) =>
            {
                foreach (var customSecurityService in _customSecurityServices)
                {
                    await customSecurityService.OnAclFileLoaded(id, String.IsNullOrEmpty(inheritPath) ? authNodePath : inheritPath, auth, inheritPath);
                }
            });

            var control = new SecurityControl(_http, _crypto, _customSecurityServices, nodeAuth);
            var securityData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);

            #region Apply Data

            foreach (var nv in securityData)
            {
                var input = control.GetControl(nv.Name) as IInputUIControl;
                if (input != null)
                {
                    input.Value = nv.Value;
                }

                control.SetDirty(false);
            }

            #endregion

            var users = new CmsAuthItemList.UniqueItemListBuilder(nodeAuth.Users.Items);
            var roles = new CmsAuthItemList.UniqueItemListBuilder(nodeAuth.Roles.Items);

            foreach (var nv in securityData)
            {
                if (nv.Name.StartsWith(SecurityControl.CheckBoxUserPrefix))
                {
                    string userName = nv.Name.Substring(SecurityControl.CheckBoxUserPrefix.Length);
                    var user = nodeAuth.Users.Items.Where(u => u.Name == userName).FirstOrDefault();
                    if (String.IsNullOrWhiteSpace(user.InheritFrom))
                    {
                        user.Allowed = bool.Parse(nv.Value);
                    }
                    else if (nv.Name == btn)
                    {
                        users.Remove(user);
                        users.Add(new CmsUser(user.Name, bool.Parse(nv.Value)));
                    }
                }
                else if (nv.Name.StartsWith(SecurityControl.CheckBoxRolePrefix))
                {
                    string roleName = nv.Name.Substring(SecurityControl.CheckBoxRolePrefix.Length);
                    var role = nodeAuth.Roles.Items.Where(r => r.Name == roleName).FirstOrDefault();
                    if (String.IsNullOrWhiteSpace(role.InheritFrom))
                    {
                        role.Allowed = bool.Parse(nv.Value);
                    }
                    else if (nv.Name == btn)
                    {
                        roles.Remove(role);
                        roles.Add(new CmsRole(role.Name, bool.Parse(nv.Value)));
                    }
                }
            }

            if (btn == SecurityControl.ButtonRemoveUserName)
            {
                var user = users.FirstOfDefault(u => u.Name == name);
                if (String.IsNullOrWhiteSpace(user.InheritFrom))
                {
                    users.Remove(user);
                }
            }
            else if (btn == SecurityControl.ButtonRemoveRoleName)
            {
                var role = roles.FirstOfDefault(r => r.Name == name);
                if (String.IsNullOrWhiteSpace(role.InheritFrom))
                {
                    roles.Remove(role);
                }
            }
            else if (btn == SecurityControl.ButtonAddUser && !String.IsNullOrWhiteSpace(control.NewUserName))
            {
                users.Add(new CmsUser(control.NewUserName, true));
            }
            else if (btn == SecurityControl.ButtonAddRole && !String.IsNullOrWhiteSpace(control.NewRoleName))
            {
                roles.Add(new CmsRole(control.NewRoleName, true));
            }
            else
            {
                var button = control.GetControl(btn) as IClickUIControl;
                if (button != null)
                {
                    button.FireClick();

                    var dirtyControls = control.GetDitryControls();
                    return Json(new { controls = dirtyControls });
                }
            }

            nodeAuth.Users = new CmsAuthItemList(users.Build());
            nodeAuth.Roles = new CmsAuthItemList(roles.Build());

            foreach (var customSecurityService in _customSecurityServices)
            {
                var closestInstanceResult = _ccs.CMS[id].ClosestInstance(_servicePack, path);
                await customSecurityService.BeforeSaveAclFile(id, path, nodeAuth, closestInstanceResult.instance, closestInstanceResult.instancePath);
            }

            WriteAcl(cms, path, nodeAuth);

            foreach (var customSecurityService in _customSecurityServices)
            {
                await customSecurityService.OnAclFileLoaded(id, path, nodeAuth, "");
            }

            return await NodeSecurity(path, id: id);
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    private void WriteAcl(CMSManager cms, string path, CMSManager.NodeAuthorization nodeAuth)
    {
        XmlDocument doc = new XmlDocument();
        XmlNode aclNode = doc.CreateElement("acl");
        doc.AppendChild(aclNode);

        foreach (var user in nodeAuth.Users.Items.Where(u => String.IsNullOrWhiteSpace(u.InheritFrom)))
        {
            XmlNode userNode = doc.CreateElement("user");

            XmlAttribute nameAttr = doc.CreateAttribute("name");
            nameAttr.Value = user.Name;
            userNode.Attributes.Append(nameAttr);

            XmlAttribute allowedAttr = doc.CreateAttribute("allowed");
            allowedAttr.Value = user.Allowed.ToString().ToLower();
            userNode.Attributes.Append(allowedAttr);

            aclNode.AppendChild(userNode);
        }
        foreach (var rolw in nodeAuth.Roles.Items.Where(r => String.IsNullOrWhiteSpace(r.InheritFrom)))
        {
            XmlNode userNode = doc.CreateElement("role");

            XmlAttribute nameAttr = doc.CreateAttribute("name");
            nameAttr.Value = rolw.Name;
            userNode.Attributes.Append(nameAttr);

            XmlAttribute allowedAttr = doc.CreateAttribute("allowed");
            allowedAttr.Value = rolw.Allowed.ToString().ToLower();
            userNode.Attributes.Append(allowedAttr);

            aclNode.AppendChild(userNode);
        }

        string fileName = cms.ConnectionString + @"/" + path + ".acl";
        var fi = DocumentFactory.DocumentInfo(fileName);
        if (fi.Exists && aclNode.ChildNodes.Count == 0)
        {
            fi.Delete();
        }

        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }
        else if (aclNode.ChildNodes.Count > 0)
        {
            string xml = doc.OuterXml;
            fi.Write(xml);
            //doc.Save(fileName);
        }
    }

    public IActionResult FormAutoComplete(string name, string path, string data, string id = "")
    {
        try
        {
            var control = SecurityControl.Empty(_http, _crypto, _customSecurityServices);
            var input = control.GetControl(name) as InputAutoComplete;

            if (input != null)
            {
                control.LoadData(data);

                var items = input.FireAutocomplete(id, base.GetCloudUserIdFromCookie(id));

                return Json(items.ToArray());
            }

            return null;
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #endregion

    #region Type Editor

    async public Task<IActionResult> NodePropertyEditor(string path, string property, string data, string id = "")
    {
        try
        {
            var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, false, true);

            string displayName = null;
            var subProperties = property.Split('.');
            for (int s = 0; s < subProperties.Length; s++)
            {
                if (s == subProperties.Length - 1)  // Properties from last instance
                {
                    var propertyGridData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
                    LoadPropertyGridData(instance, propertyGridData);
                }
                else
                {
                    var subPropertyInfo = instance.GetType().GetProperty(subProperties[s]);
                    if (subPropertyInfo == null)
                    {
                        throw new Exception("Unknown sub property '" + subProperties[s] + "'");
                    }

                    instance = subPropertyInfo.GetValue(instance);
                }
            }

            var propertyInfo = instance.GetType().GetProperty(property.Split('.').Last());
            var editorAttribute = propertyInfo?.GetCustomAttribute<EditorAttribute>();
            var authArrayAttribute = propertyInfo.GetCustomAttribute<AuthorizablePropertyArrayAttribute>();

            var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute != null)
            {
                displayName = displayNameAttribute.LocalizedDisplayName();
            }

            if (editorAttribute != null)
            {
                var editor = Type.GetType(editorAttribute.EditorTypeName).CmsCreateInstance(_servicePack) as ITypeEditor;
                if (editor is IUITypeEditor)
                {
                    var cmsManager = _ccs.CMS[id]?.Clone(_servicePack, _ccs.GetCmsSecrets(_servicePack, id));
                    var uiControl = editor is IUITypeEditorAsync ?
                        await ((IUITypeEditorAsync)editor).GetUIControlAsync(new TypeEditorContext(cmsManager, path, instance, property.Split('.').Last())) :
                        ((IUITypeEditor)editor).GetUIControl(new TypeEditorContext(cmsManager, path, instance, property.Split('.').Last()));

                    if (authArrayAttribute != null)
                    {
                        foreach (GroupBox group in uiControl.AllControls().Where(c => c is GroupBox && !String.IsNullOrEmpty(((GroupBox)c).ItemUrl)))
                        {
                            group.InsertControl(0, new SecurityButton(path, authArrayAttribute.TagName + "_" + group.ItemUrl));
                        }
                    }

                    return Json(new
                    {
                        displayName = displayName ?? property,
                        path = path,
                        name = property,
                        controls = new IUIControl[] { uiControl },
                        buttonClickMethod = "NodePropertyEditorButtonClick"
                    });
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult NodePropertyEditorCommit(string path, string property, string data, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "NodePropertyEditor", "Commit", id, path);

            var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, false, true);
            if (instance == null)
            {
                throw new Exception("Can't initialize node instance");
            }

            var subProperties = property.Split('.');
            for (int s = 0; s < subProperties.Length; s++)
            {
                if (s == subProperties.Length - 1)  // Properties from last instance
                {
                    //var propertyGridData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
                    //LoadPropertyGridData(instance, propertyGridData);
                }
                else
                {
                    var subPropertyInfo = instance.GetType().GetProperty(subProperties[s]);
                    if (subPropertyInfo == null)
                    {
                        throw new Exception("Unknown sub property '" + subProperties[s] + "'");
                    }

                    instance = subPropertyInfo.GetValue(instance);
                }
            }

            var propertyInfo = instance.GetType().GetProperty(property.Split('.').Last());
            var editorAttribute = propertyInfo?.GetCustomAttribute<EditorAttribute>();

            if (editorAttribute == null)
            {
                throw new Exception("no editor attribute for this attribute");
            }

            var editor = Type.GetType(editorAttribute.EditorTypeName).CmsCreateInstance(_servicePack) as ITypeEditor;
            if (editor == null)
            {
                throw new Exception("Can't intialize editor instance");
            }

            editor.LoadData(data);

            string result = null;
            if (propertyInfo.PropertyType.IsValueTypeOrString())
            {
                if (editor.ResultValue.GetType().IsArray)  // ListBox gibt immer ein Array zurück (auch bei Single select)
                {
                    if (((Array)editor.ResultValue).Length > 0)
                    {
                        result = ((Array)editor.ResultValue).GetValue(0).ToString();
                    }
                    else
                    {
                        result = String.Empty;
                    }
                }
                else
                {
                    result = editor.ResultValue?.ToString();
                }
            }
            else
            {
                result = PropertyComplexValueToString(editor.ResultValue);
            }

            return Json(new
            {
                value = result
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    async public Task<IActionResult> NodePropertyEditorButtonClick(string path, string name, string btn, string data, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "NodePropertyEditor", "ButtonClick", id, path, btn);

            var instance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path, false, true);

            var propertyInfo = instance.GetType().GetProperty(name);
            var editorAttribute = propertyInfo?.GetCustomAttribute<EditorAttribute>();

            if (editorAttribute != null)
            {
                var editor = Type.GetType(editorAttribute.EditorTypeName).CmsCreateInstance(_servicePack) as ITypeEditor;
                if (editor is IUITypeEditor)
                {
                    var cmsManager = _ccs.CMS[id]?.Clone(_servicePack, _ccs.GetCmsSecrets(_servicePack, id));
                    var control = editor is IUITypeEditorAsync ?
                        await ((IUITypeEditorAsync)editor).GetUIControlAsync(new TypeEditorContext(cmsManager, path, instance, name)) :
                        ((IUITypeEditor)editor).GetUIControl(new TypeEditorContext(cmsManager, path, instance, name));

                    if (control != null)
                    {
                        control.LoadData(data);

                        var button = control.GetControl(btn) as IClickUIControl;
                        if (button != null)
                        {
                            button.FireClick();

                            var dirtyControls = control.GetDitryControls();
                            return Json(new { controls = dirtyControls });
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #endregion

    #region Helper

    private void LoadPropertyGridData(object instance, IEnumerable<NameValue> propertyGridData)
    {
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            try
            {
                if (propertyInfo.SetMethod == null)
                {
                    continue;
                }

                var browsable = propertyInfo.GetCustomAttribute<System.ComponentModel.BrowsableAttribute>();
                if (browsable != null && browsable.Browsable == false)
                {
                    if (instance is AuthorizableArrayItem && propertyInfo.Name == "ItemGuid")
                    {
                        // Set this value
                    }
                    else
                    {
                        continue;
                    }
                }

                var propData = propertyGridData.Where(p => p.Name == propertyInfo.Name).FirstOrDefault();
                if (propData == null)
                {
                    throw new Exception("Data for property '" + propertyInfo.Name + "' is missing");
                }

                string propDataValue = DecryptComplex(propData.Value);

                if (propertyInfo.PropertyType == typeof(string))
                {
                    propertyInfo.SetValue(instance, propDataValue);
                }
                else if (propertyInfo.PropertyType.IsEnum)
                {
                    object val = Enum.Parse(propertyInfo.PropertyType, propDataValue);
                    propertyInfo.SetValue(instance, val);
                }
                else if (propertyInfo.PropertyType == typeof(double))
                {
                    propertyInfo.SetValue(instance, double.Parse(propDataValue.Replace(",", "."), CmsGlobals.Nhi));
                }
                else if (propertyInfo.PropertyType == typeof(float))
                {
                    propertyInfo.SetValue(instance, float.Parse(propDataValue.Replace(",", "."), CmsGlobals.Nhi));
                }
                else if (propertyInfo.PropertyType.IsValueType)
                {
                    object val = Convert.ChangeType(propDataValue, propertyInfo.PropertyType);
                    propertyInfo.SetValue(instance, val);
                }
                else
                {
                    var existingObject = propertyInfo.GetValue(instance);
                    if (existingObject != null) // zB bei TableColumn: Data Attribute ist Field, Hotlink usw immer mit anderen Attribute (abhängig vom ColumnType) -> hier in das bestehenden Type serialisieren
                    {
                        var val = JSerializer.Deserialize(propDataValue, existingObject.GetType());
                        propertyInfo.SetValue(instance, val);
                    }
                    else
                    {
                        var val = JSerializer.Deserialize(propDataValue, propertyInfo.PropertyType);
                        propertyInfo.SetValue(instance, val);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Property '" + propertyInfo.Name + "': " + ex.Message);
            }
        }
    }

    private ITypeEditor GetTypeEditor(object instance, string propertyName)
    {
        var propertyInfo = instance.GetType().GetProperty(propertyName);
        var editorAttribute = propertyInfo?.GetCustomAttribute<EditorAttribute>();

        if (editorAttribute != null)
        {
            var editor = Type.GetType(editorAttribute.EditorTypeName).CmsCreateInstance(_servicePack) as ITypeEditor;
        }

        return null;
    }

    private string PropertyComplexValueToString(object val)
    {
        return EncryptComplex(JSerializer.Serialize(val));
    }

    private string EncryptComplex(string val)
    {
        return $"complex#{_crypto.EncryptTextDefault(val, resultStringType: CryptoResultStringType.Hex)}";
    }

    private string DecryptComplex(string complex)
    {
        if (complex.StartsWith("complex#"))
        {
            complex = _crypto.DecryptTextDefault(complex.Substring(8));
        }

        return complex;
    }

    #endregion

    #endregion

    #region Tools

    public IActionResult ToolClick(string path, string name, string action, string id = "")
    {
        try
        {
            var schemaNode = _ccs.CMS[id].SchemaNode(path);
            XmlNode toolSchemaNode;

            if (action == "new")
            {
                #region Create new object

                if (name == null)
                {
                    toolSchemaNode = schemaNode.SelectSingleNode("schema-node[not(@name)]");
                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-file[not(@name)]");
                    }

                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-link[not(@name)]");
                    }
                }
                else
                {
                    toolSchemaNode = schemaNode.SelectSingleNode("schema-node[@name='" + name + "']");
                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-file[@name='" + name + "']");
                    }

                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-link[@name='" + name + "']");
                    }
                }

                ICreatable creatable = _ccs.CMS[id].SchemaNodeInstance(_servicePack, toolSchemaNode) as ICreatable;
                if (creatable is SchemaNode)
                {
                    ((SchemaNode)creatable).CmsManager = _ccs.CMS[id].Clone(_servicePack,
                                                                            _ccs.GetCmsSecrets(_servicePack, id));
                    ((SchemaNode)creatable).RelativePath = path;
                }

                if (creatable is IUI)
                {
                    var control = ((IUI)creatable).GetUIControl(true);
                    return Json(new
                    {
                        displayName = toolSchemaNode.Attributes["prompt"]?.Value ?? "Neues Element",
                        path = path,
                        name = name,
                        controls = control.ChildControls
                    });
                }

                #endregion
            }
            else if (action == "link")
            {
                #region Add Links

                toolSchemaNode = schemaNode.SelectSingleNode("schema-link[@name='" + name + "']");

                return Json(new
                {
                    displayName = toolSchemaNode.Attributes["prompt"]?.Value ?? "Links",
                    path = path,
                    name = name,
                    controls = new IUIControl[] {
                        new LazyNavTree() {
                            SingleSelect=toolSchemaNode.Attributes["onlyone"]?.Value == "true"
                        }
                    }
                });



                #endregion
            }
            else if (action == "paste" || action == "cut")
            {
                var copyNodeInstance = _ccs.CMS[id].SchemaNodeInstance(_servicePack, name, true, true);
                if (copyNodeInstance is SchemaNode)
                {
                    ((SchemaNode)copyNodeInstance).CmsManager = _ccs.CMS[id].Clone(_servicePack,
                                                                            _ccs.GetCmsSecrets(_servicePack, id));
                    ((SchemaNode)copyNodeInstance).RelativePath = path;
                }

                if (copyNodeInstance is IUI)
                {
                    var control = ((IUI)copyNodeInstance).GetUIControl(true);
                    NameUrlControl nameUrlControl = null;
                    if (control is NameUrlControl)
                    {
                        nameUrlControl = (NameUrlControl)control;
                    }
                    else if (control is NameUrlUserConrol)
                    {
                        nameUrlControl = ((NameUrlUserConrol)control).NameUrlControlInstance;
                    }
                    if (nameUrlControl == null)
                    {
                        throw new Exception("Can't copy node: no url/name");
                    }
                    var copyControl = new NameUrlControl(nameUrlControl);

                    return Json(new
                    {
                        displayName = $"Element {(action == "cut" ? "verschieben" : "kopieren")}",
                        path = path,
                        name = name,
                        controls = copyControl.ChildControls
                    });
                }
            }

            throw new Exception($"Unknown action {action}");
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult ToolButtonClick(string path, string name, string btn, string data, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Tool", "ButtonCLick", id, path, btn, name);

            var schemaNode = _ccs.CMS[id].SchemaNode(path);
            var toolSchemaNode = schemaNode.SelectSingleNode("schema-node[@name='" + name + "']");
            if (toolSchemaNode == null)
            {
                toolSchemaNode = schemaNode.SelectSingleNode("schema-file[@name='" + name + "']");
            }

            var formData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
            var secrets = _ccs.GetCmsSecrets(_servicePack, id);

            ICreatable creatable = _ccs.CMS[id].SchemaNodeInstance(_servicePack, toolSchemaNode) as ICreatable;
            if (creatable is IUI)
            {
                var control = ((IUI)creatable).GetUIControl(false);
                if (control != null)
                {
                    formData.ApplyTo(control, secrets);
                    control.SetDirty(false);

                    var button = control.GetControl(btn) as IClickUIControl;
                    if (button != null)
                    {
                        button.FireClick();

                        var dirtyControls = control.GetDitryControls();
                        return Json(new { controls = dirtyControls });
                    }
                }
            }

            return Json(new { });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    async public Task<IActionResult> ToolCommit(string path, string action, string name, string data, string id = "")
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Tool", "Commit", id, path, action, name);

            if (action == "new")
            {
                #region Create new item

                var schemaNode = _ccs.CMS[id].SchemaNode(path);
                XmlNode toolSchemaNode = null;

                if (name == null)
                {
                    toolSchemaNode = schemaNode.SelectSingleNode("schema-node[not(@name)]");
                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-file[not(@name)");
                    }
                }
                else
                {
                    toolSchemaNode = schemaNode.SelectSingleNode("schema-node[@name='" + name + "']");
                    if (toolSchemaNode == null)
                    {
                        toolSchemaNode = schemaNode.SelectSingleNode("schema-file[@name='" + name + "']");
                    }
                }

                ICreatable creatable = _ccs.CMS[id].SchemaNodeInstance(_servicePack, toolSchemaNode) as ICreatable;
                var secrets = _ccs.GetCmsSecrets(_servicePack, id);

                if (creatable is SchemaNode)
                {
                    ((SchemaNode)creatable).CmsManager = _ccs.CMS[id].Clone(_servicePack, secrets);
                    ((SchemaNode)creatable).RelativePath = path;
                }

                #region Apply Data

                var formData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);
                formData.ApplyAndSumit(creatable, secrets: secrets, applySecrets: false);

                #endregion

                string createAs = creatable.CreateAs(true);
                if (creatable is ISchemaNode)
                {
                    ((ISchemaNode)creatable).CmsManager = _ccs.CMS[id].Clone(_servicePack, secrets);
                    ((ISchemaNode)creatable).RelativePath = path + "/" + createAs;
                }

                if (creatable is IAutoCreatable)
                {
                    ((IAutoCreatable)creatable).AutoCreate();
                }
                else
                {

                    createAs = createAs.Replace(@"\", "/");
                    string fullName = _ccs.CMS[id].ConnectionString + "/" + path + @"/" + createAs + ".xml";

                    if (DocumentFactory.DocumentInfo(fullName).Exists)
                    {
                        throw new Exception("Object with this  name/url already exists");
                    }

                    IStreamDocument xmlStream = DocumentFactory.New(_ccs.CMS[id].ConnectionString);
                    creatable.Save(xmlStream);

                    xmlStream.SaveDocument(fullName);
                    if (createAs.Contains(@"/"))
                    {
                        var di = DocumentFactory.PathInfo(_ccs.CMS[id].ConnectionString + "/" + path + @"/" + createAs.Substring(0, createAs.IndexOf(@"/")));
                        _ccs.CMS[id].ParseSchemaNode(_servicePack, di, toolSchemaNode);
                    }

                    formData.ApplyAndSumit(creatable, secrets, path);
                    await creatable.CreatedAsync(fullName);
                }

                return Json(new JsonSuccess());

                #endregion
            }
            else if (action == "link")
            {
                #region Insert Links

                var schemaNode = _ccs.CMS[id].SchemaNode(path);
                var toolSchemaNode = schemaNode.SelectSingleNode("schema-link[@name='" + name + "']");

                var cms = _ccs.CMS[id];
                string[] targetPaths = JSerializer.Deserialize<string[]>(data);
                foreach (var targetPath in targetPaths)
                {
                    //
                    //  Link kann auch Eigenschaften haben (PresentationLink: Mode, Affectiong, usw.)
                    //  Darum versuche einen Instanz zu erstellen, damit diese Eigenschaften auch beim Anlegen erzeugt werden
                    //
                    var link = _ccs.CMS[id].SchemaNodeInstance(_servicePack, path + "/" + (schemaNode.Attributes["name"]?.Value ?? ""), CmsNodeType.Link) as Link;
                    if (link != null)
                    {
                        link.LinkUri = targetPath;
                    }
                    else
                    {
                        link = new Link(targetPath);
                    }

                    object templObj = cms.SchemaNodeInstance(_servicePack, targetPath + @"\.linktemplate", true);
                    if (templObj != null &&
                        templObj.GetType().Equals(link.GetType()))
                    {
                        IStreamDocument templStream = cms.XmlStream(targetPath + @"\.linktemplate");
                        if (templStream != null)
                        {
                            string uri = link.LinkUri;
                            link.Load(templStream);
                            link.LinkUri = uri;
                        }
                    }

                    if (toolSchemaNode.Attributes["uniquelinks"] != null &&
                        toolSchemaNode.Attributes["uniquelinks"].Value == "true")
                    {
                        foreach (var fi in (DocumentFactory.PathInfo(cms.ConnectionString + "/" + path).GetFiles()))
                        {
                            if (fi.Name.StartsWith(".") ||
                                fi.Extension.ToLower() == ".acl")
                            {
                                continue;
                            }

                            try
                            {
                                Link existingLink = new Link();
                                IStreamDocument linkStream = DocumentFactory.Open(fi.FullName);
                                existingLink.Load(linkStream);
                                if (existingLink.LinkUri.ToLower() == link.LinkUri.ToLower())
                                {
                                    throw new Exception("Objekt ist bereits in der Liste vorhanden...");
                                }
                            }
                            catch { }
                        }
                    }
                    if (toolSchemaNode.Attributes["onlyone"] != null &&
                        toolSchemaNode.Attributes["onlyone"].Value == "true")
                    {
                        foreach (var fi in (DocumentFactory.PathInfo(cms.ConnectionString + "/" + path)).GetFiles("*.link"))
                        {
                            fi.Delete();
                        }
                    }


                    IStreamDocument targetStream = cms.XmlStream(link.LinkUri);
                    link.LoadParent(targetStream);

                    // z.B. für geoland: Nach Einfügen eines Themas in den TOC können auch noch andere Themen mit gleicher ID eingefügt werden...
                    //if (link.HasAdditionalLinks(cms.RootDirectory, ((NavTreeNode)tvNav.SelectedNode).UriPath))
                    //{
                    //    string parentUriPath = item.UriPath;
                    //    ICreatable groupForAdditionals = link.GroupForAdditianalLinks();
                    //    if (groupForAdditionals != null)
                    //    {
                    //        XmlStreamDocument groupStream = new XmlStreamDocument();
                    //        groupForAdditionals.Save(groupStream);
                    //        FileInfo groupFi = new FileInfo(parentUriPath + @"/" + groupForAdditionals.CreateAs(true) + ".xml");
                    //        groupStream.SaveDocument(groupFi.FullName);
                    //        parentUriPath = groupFi.Directory.FullName;
                    //    }
                    //    foreach (Link additialLink in link.AdditionalLinks(cms.RootDirectory, ((NavTreeNode)tvNav.SelectedNode).UriPath))
                    //    {
                    //        XmlStreamDocument additionalTargetStream = cms.XmlStream(additialLink.LinkUri);
                    //        additialLink.LoadParent(additionalTargetStream);
                    //        XmlStreamDocument xmlStream = new XmlStreamDocument();
                    //        additialLink.Save(xmlStream);
                    //        xmlStream.SaveDocument(parentUriPath + @"\" + Helper.NewLinkName());
                    //    }
                    //}
                    //else
                    {
                        string newLinkName = Helper.NewLinkName();
                        //if (name.IndexOf("*") > 0)  // themes_*
                        //    newLinkName = name.Replace("*", CMSManager.WildcardAnyPlaceholder + newLinkName);

                        IStreamDocument xmlStream = DocumentFactory.New(_ccs.CMS[id].ConnectionString);
                        link.Save(xmlStream);
                        xmlStream.SaveDocument(cms.ConnectionString + "/" + path + @"/" + newLinkName);
                    }
                }
                #endregion

                return Json(new JsonSuccess());
            }
            else if (action == "paste" || action == "cut")
            {
                var copyable = _ccs.CMS[id].SchemaNodeInstance(_servicePack, name, true, true) as ICopyable;
                if (copyable == null)
                {
                    throw new Exception("Object is not copyable");
                }
                if (copyable is SchemaNode)
                {
                    ((SchemaNode)copyable).CmsManager = _ccs.CMS[id].Clone(_servicePack, _ccs.GetCmsSecrets(_servicePack, id));
                    ((SchemaNode)copyable).RelativePath = path; // the path for creating the UI etc...
                }

                #region Apply Data

                var formData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);

                if (copyable is IUI)
                {
                    var control = ((IUI)copyable).GetUIControl(false);
                    if (control != null)
                    {
                        foreach (var nv in formData)
                        {
                            var input = control.GetControl(nv.Name) as IInputUIControl;
                            if (input != null)
                            {
                                input.Value = nv.Value;
                            }
                        }
                    }

                    //if (control is ISubmit)
                    //{
                    //    ((ISubmit)control).Submit();
                    //}
                }

                #endregion

                if (copyable is SchemaNode)
                {
                    ((SchemaNode)copyable).RelativePath = name;  // not path!!! path is the parent
                }

                copyable.CopyCmsManager = _ccs.CMS[id];
                copyable.CopyTo(path);

                if (action == "cut")
                {
                    _ccs.CMS[id].DeleteNode(name);
                }

                return Json(new JsonSuccess());
            }
            else
            {
                throw new Exception("Unknown action: " + action);
            }

        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #endregion

    #region Copy Paste

    public IActionResult NodeCopy(string path)
    {
        _cmsLogger.Log(this.GetCurrentUsername(),
                       "Node", "Copy", String.Empty, path);

        return base.Json(new
        {
            success = true,
            copyNodePath = $"+{path}"
        });
    }

    public IActionResult NodeCut(string path)
    {
        _cmsLogger.Log(this.GetCurrentUsername(),
                       "Node", "Cut", String.Empty, path);

        return base.Json(new
        {
            success = true,
            copyNodePath = $"-{path}"
        });
    }

    #endregion

    #region Link Tree

    public IActionResult LinkNodes(string path, string name, string id = "")
    {
        try
        {
            var schemaNode = _ccs.CMS[id].SchemaNode(path);
            var toolSchemaNode = schemaNode.SelectSingleNode("schema-link[@name='" + name + "']");

            string[] filters = toolSchemaNode.Attributes["filter"].Value.Split(';').Select(f => f.Split('|')[1]).ToArray();
            string[] uriFilters = UriFilters(id, toolSchemaNode, path);

            var doc = _ccs.CMS[id].ToXml(_servicePack, false, filters, uriFilters, true);

            List<NavTreeNode> rootNodes = new List<NavTreeNode>();

            foreach (var filter in filters)
            {
                var filterNodes = doc.SelectNodes("//*[@filtertype='" + filter + "']");
                foreach (XmlNode filterNode in filterNodes)
                {
                    var parentNode = GetNavTreeNode(id, GetXmlPath(filterNode.ParentNode), rootNodes, doc);
                    if (parentNode != null)
                    {
                        parentNode.Add(new NavTreeNode()
                        {
                            Name = filterNode.Attributes["name"]?.Value,
                            AliasName = filterNode.Attributes["displayname"]?.Value,
                            Path = GetXmlPath(filterNode),
                            Selectable = true
                        });
                    }
                }
            }

            return Json(new
            {
                treenodes = rootNodes
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Helper

    private string GetXmlPath(XmlNode node)
    {
        string path = String.Empty;

        while (node?.Attributes["name"] != null)
        {
            path = node.Attributes["name"].Value + (!String.IsNullOrEmpty(path) ? "/" : "") + path;
            node = node.ParentNode;
        }

        return path;
    }

    private string GetXmlAliasname(string id, string path, XmlDocument doc)
    {
        XmlNode current = null;
        foreach (string name in path.Split('/'))
        {
            if (current == null)
            {
                current = doc.SelectSingleNode("CMS/item[@name='" + name + "']");
            }
            else
            {
                current = current.SelectSingleNode("item[@name='" + name + "']");
            }

            if (current == null)
            {
                break;
            }
        }

        if (current == null)
        {
            return String.Empty;
        }

        return _ccs.Translate(id, current.Attributes["displayname"]?.Value ?? current.Attributes["name"]?.Value);
    }

    private NavTreeNode GetNavTreeNode(string id, string path, List<NavTreeNode> rootNodes, XmlDocument doc)
    {
        NavTreeNode currentNode = null;
        string currentPath = String.Empty;

        foreach (string name in path.Split('/'))
        {
            currentPath += (!String.IsNullOrEmpty(currentPath) ? "/" : "") + name;

            if (currentNode == null)  // rootNodes
            {
                currentNode = rootNodes?.Where(n => n.Name == name).FirstOrDefault();
                if (currentNode == null)
                {
                    currentNode = new NavTreeNode()
                    {
                        Name = name,
                        Path = currentPath,
                        AliasName = GetXmlAliasname(id, currentPath, doc)
                    };
                    rootNodes.Add(currentNode);
                }
            }
            else
            {
                var node = currentNode.Nodes?.Where(n => n.Name == name).FirstOrDefault();
                if (node == null)
                {
                    node = new NavTreeNode()
                    {
                        Name = name,
                        Path = currentPath,
                        AliasName = GetXmlAliasname(id, currentPath, doc)
                    };
                    currentNode.Add(node);
                }
                currentNode = node;
            }
        }

        return currentNode;
    }

    #endregion

    #endregion

    #region Search 

    async public Task<IActionResult> Search(string id, string term, string path)
    {
        try
        {
            if (!String.IsNullOrWhiteSpace(_ccs.Instance.ElasticSearchEndpoint))
            {
                var service = _ccs.Instance.ElasticSearchEndpoint + "/" + id.CmsIdToElasticSerarchIndexName() + "/_search?size=1000&q=" + AnalyseTerm(term);

                string json = await _http.GetStringAsync(service);
                var response = JSerializer.Deserialize<ElasticSearchResponse>(json);

                if (response.Hits != null && response.Hits.Items != null)
                {
                    var items = response.Hits.Items.ToList();
                    items.Sort(new ElasticItemSorter(term, path));

                    return Json(items.Take(20).Select(i =>
                    {
                        string thumbnail = "";
                        switch (i.Source.Category)
                        {
                            case "directory":
                                thumbnail = Url.Content("/images/enter-26.png");
                                break;
                            case "file":
                                thumbnail = Url.Content("/images/config-26.png");
                                break;
                            case "link":
                                thumbnail = Url.Content("/images/link-26.png");
                                break;
                        }

                        return new
                        {
                            suggested_text = i.Source.SuggestedText,
                            subtext = i.Source.SubText,
                            path = i.Source.Path,
                            category = i.Source.Category,
                            thumbnail = thumbnail
                        };
                    }).ToArray());
                }

            }
            return Json(new object[]{
                new
                {
                    suggested_text = "Leider:",
                    subtext="Keine Suchindex parametriert",
                    category=""
                }
            });
        }
        catch (Exception ex)
        {
            List<object> items = new List<object>();
            items.Add(new
            {
                suggested_text = "EXCEPTION",
                subtext = ex.Message,
                category = ""
            });
            if (!String.IsNullOrWhiteSpace(_ccs.Instance.ElasticSearchEndpoint))
            {
                items.Add(new
                {
                    suggested_text = "Solution",
                    subtext = "(Re)create Index",
                    category = "",
                    cmd = "Setup/IndexCms/" + id //Url.Action("IndexCms", "Setup", new { id = id })
                });
            }
            return Json(items);
        }
    }

    private class ElasticItemSorter : IComparer<Item>
    {
        public ElasticItemSorter(string term, string path)
        {
            this.Term = term?.ToLower() ?? String.Empty;
            this.Path = path?.ToLower() ?? String.Empty;

            if (this.Path.StartsWith("/"))
            {
                this.Path = this.Path.Substring(1);
            }
        }

        public string Path { get; private set; }
        public string Term { get; set; }

        public int Compare(Item x, Item y)
        {
            #region Compare Text

            string xText = x?.Source?.SuggestedText.ToLower() ?? String.Empty;
            string yText = y?.Source?.SuggestedText.ToLower() ?? String.Empty;

            if (xText.Contains(this.Term) &&
               !yText.Contains(this.Term))
            {
                return -1;
            }

            if (!xText.Contains(this.Term) &&
                yText.ToLower().Contains(this.Term))
            {
                return 1;
            }

            #endregion

            #region Compare Path

            string xPath = x?.Source?.Path.ToLower() ?? String.Empty;
            string yPath = y?.Source?.Path.ToLower() ?? String.Empty;

            if (xPath.StartsWith("/"))
            {
                xPath = xPath.Substring(1);
            }

            if (yPath.StartsWith("/"))
            {
                yPath = yPath.Substring(1);
            }

            if (xPath.StartsWith(this.Path) &&
               !yPath.StartsWith(this.Path))
            {
                return -1;
            }

            if (!xPath.StartsWith(this.Path) &&
                yPath.ToLower().StartsWith(this.Path))
            {
                return 1;
            }

            if (xPath.Length < yPath.Length)
            {
                return -1;
            }

            if (xPath.Length > yPath.Length)
            {
                return 1;
            }

            #endregion

            return 0;
        }
    }

    #region Helper

    private string AnalyseTerm(string term)
    {
        if (String.IsNullOrWhiteSpace(term))
        {
            return String.Empty;
        }

        term = term.Trim().Replace("-", " ").Replace(" ", " ");

        while (term.Contains("  "))
        {
            term = term.Replace("  ", " ");
        }

        List<string> singleTerms = new List<string>(term.Split(' '));


        StringBuilder sb = new StringBuilder();
        foreach (var suffix in new string[] { "", "*" })
        {
            if (sb.Length > 0)
            {
                sb.Append(" OR ");
            }

            if (singleTerms.Count > 0)
            {
                sb.Append("(");
            }

            for (int i = 0; i < singleTerms.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" AND ");
                }

                sb.Append(AnalyseGerman(singleTerms[i], suffix));
            }
            if (singleTerms.Count > 0)
            {
                sb.Append(")");
            }
        }

        return sb.ToString();
    }

    private string AnalyseGerman(string word, string suffix)
    {
        List<string> words = new List<string>(new string[] { word });

        if (word.Contains("ss"))
        {
            words.Add(word.Replace("ss", "ß"));
        }
        if (word.Contains("ß"))
        {
            words.Add(word.Replace("ß", "ss"));
        }

        if (words.Count == 1)
        {
            return word + suffix;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("(");
        for (int i = 0; i < words.Count; i++)
        {
            if (String.IsNullOrWhiteSpace(words[i]))
            {
                continue;
            }

            if (i > 0)
            {
                sb.Append(" OR ");
            }

            sb.Append(words[i] + suffix);
        }
        sb.Append(")");

        return sb.ToString();
    }

    #endregion

    #endregion

    #region Secrets

    public IActionResult SecretPlaceholders(string id)
    {
        return Json(_ccs.GetCmsSecrets(_servicePack, id).AllKeys.ToArray());
    }

    public IActionResult VerifySecretsPassword(string id, string pw)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes($"secrets:{_ccs.SecretsPassword(id)}");
        passwordBytes = System.Security.Cryptography.SHA512.Create().ComputeHash(passwordBytes);
        if (pw == Convert.ToBase64String(passwordBytes))
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, errorMessage = "Wrong password" });
    }

    #endregion

    #region Helper

    public IEnumerable<XmlNode> CreateableSchemaNodes(XmlNode schemaNode)
    {
        List<XmlNode> creatables = new List<XmlNode>();

        foreach (XmlNode creatable in schemaNode.SelectNodes("schema-node[@createmode='user']"))
        {
            creatables.Add(creatable);
        }
        foreach (XmlNode creatable in schemaNode.SelectNodes("schema-file[@createmode='user']"))
        {
            creatables.Add(creatable);
        }

        return creatables;
    }

    public IEnumerable<XmlNode> LinkableSchemaNodes(XmlNode schemaNode)
    {
        List<XmlNode> linkables = new List<XmlNode>();

        foreach (XmlNode linkable in schemaNode.SelectNodes("schema-link[@filter]"))
        {
            linkables.Add(linkable);
        }
        //foreach (XmlNode appendable in schemaNode.SelectNodes("schema-node[@filter]"))
        //{
        //    appendables.Add(appendable);
        //}

        return linkables;
    }

    private string[] UriFilters(string id, XmlNode schemaNode, string parentUriPath)
    {
        StringBuilder uriFilters = new StringBuilder();

        #region urifilterlinks

        if (schemaNode.Attributes["urifilterlinks"] != null)
        {
            string urifilterslinks = schemaNode.Attributes["urifilterlinks"].Value;
            var di = DocumentFactory.PathInfo(_ccs.CMS[id].ConnectionString + @"/" + parentUriPath);
            while (urifilterslinks.StartsWith("../"))
            {
                urifilterslinks = urifilterslinks.Substring(3, urifilterslinks.Length - 3);
                di = di.Parent;
            }
            di = DocumentFactory.PathInfo(di.FullName + @"/" + urifilterslinks);
            foreach (var fi in di.GetFiles("*.link"))
            {
                Link link = new Link();
                IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
                link.Load(xmlStream);

                if (uriFilters.Length > 0)
                {
                    uriFilters.Append("|");
                }

                uriFilters.Append(link.LinkUri);
            }
        }
        #endregion

        #region urifilterpath

        if (schemaNode.Attributes["urifilterpath"] != null)
        {
            string urifilterpath = schemaNode.Attributes["urifilterpath"].Value;
            var di = DocumentFactory.PathInfo(_ccs.CMS[id].ConnectionString + @"/" + parentUriPath);
            while (urifilterpath.StartsWith("../"))
            {
                urifilterpath = urifilterpath.Substring(3, urifilterpath.Length - 3);
                di = di.Parent;
            }
            di = DocumentFactory.PathInfo(di.FullName + @"/" + urifilterpath);
            urifilterpath = di.FullName.Substring(_ccs.CMS[id].ConnectionString.Length + 1, di.FullName.Length - _ccs.CMS[id].ConnectionString.Length - 1);
            if (uriFilters.Length > 0)
            {
                uriFilters.Append("|");
            }

            uriFilters.Append(urifilterpath.Replace(@"\", "/"));
        }

        #endregion

        if (uriFilters.Length > 0)
        {
            return uriFilters.ToString().Split('|');
        }

        return null;
    }

    #endregion
}