using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Extensions.Text;
using E.Standard.Json;
using E.Standard.ThreadSafe;
using E.Standard.Web.Exceptions;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.Extensions;
using E.Standard.WebGIS.CmsSchema.UI;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class ArcServerService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName, IRefreshable
{
    private string _server;
    private string _service;
    private string _serviceUrl = String.Empty;
    private readonly string _tileUrl = String.Empty;
    private string _user = String.Empty;
    private string _pwd = String.Empty;
    private string _clientId = String.Empty;
    private string _guid;
    private AGSGetSelectionMothod _selectionMethod = AGSGetSelectionMothod.Modern;
    private int _expiration = 60;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ArcServerService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        this.ExportMapFormat = AGSExportMapFormat.Json;

        this.DynamicPresentations = WebMapping.Core.ServiceDynamicPresentations.Auto;
        this.DynamicQueries = WebMapping.Core.ServiceDynamicQueries.Auto;
        this.DynamicDehavior = WebMapping.Core.DynamicDehavior.UseStrict;
    }

    #region Properties

    [DisplayName("Darstellungsvariaten bereitstellen")]
    [Description("Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.")]
    public WebMapping.Core.ServiceDynamicPresentations DynamicPresentations { get; set; }

    [DisplayName("Abfragen bereitstellen")]
    [Description("Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)")]
    public WebMapping.Core.ServiceDynamicQueries DynamicQueries { get; set; }

    [DisplayName("Dynamisches Verhalten")]
    [Description("Gibt an, wie mit Layern umgegangen wird, die nicht beim erstellen oder nach einem Refresh im CMS unter Themen gelistet werden. AutoAppendNewLayers ... neue Themen werden beim Initialisieren des Dienstes (nach einem cache/clear) der Karte hinzugefügt und können über den TOC geschalten werden. UseStrict ... nur jene Themen, die unter Themen aufgelistet sind, kommen auch in der Karte vor. SealedLayers_UseServiceDefaults ... des wird keine Layerschaltung an den Dienst übergeben. Das bewirkt, dass immer die Defaultschaltung aus dem Layer angezeigt wird. Diese Options macht nur beim Fallback(druck)services für VTC Dienste Sinn!")]
    public WebMapping.Core.DynamicDehavior DynamicDehavior { get; set; }

    [DisplayName("Service-Typ")]
    [Description("Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.")]
    public WebMapping.Core.ImageServiceType ServiceType { get; set; }

    [DisplayName("Allow QueryBuilder (Darstellungsfilter aus TOC")]
    [Description("Der Anwender kann aus dem TOC Filter als mit deinen SQL Edititor setzen.")]
    [AuthorizableProperty("allow_querybuilder", false)]
    public bool AllowQueryBuilder { get; set; }

    [DisplayName("Karten Server")]
    [Category("Service")]
    public string Server
    {
        get { return _server; }
        set { _server = value; }
    }
    [DisplayName("Karten Dienst")]
    [Category("Service")]
    public string Service
    {
        get { return _service; }
        set { _service = value; }
    }
    [DisplayName("Karten Dienst Url")]
    [Category("Service")]
    public string ServiceUrl
    {
        get { return _serviceUrl; }
        set { _serviceUrl = value; }
    }

    [DisplayName("Export Map Format")]
    [Category("Service")]
    [Description("Bei 'Json' wird das Ergebnis ins Outputverzeichnis von ArcGIS Server gelegt und dort vom Client abgeholt. Hat der Client keinen Zugriff auf dieses Output Verzeichnis, kann als Option 'Image' gewählt werden. Es wird dann vom ArcGIS Server keine Bild abgelegt sondern direkt übergeben.")]
    public AGSExportMapFormat ExportMapFormat { get; set; }

    [DisplayName("Username")]
    [Category("~Anmeldungs-Credentials")]
    public string Username
    {
        get { return _user; }
        set { _user = value; }
    }

    [DisplayName("Password")]
    [Category("~Anmeldungs-Credentials")]
    [PasswordPropertyText(true)]
    public string Password
    {
        get { return _pwd; }
        set { _pwd = value; }
    }

    [DisplayName("Token")]
    [Category("~Anmeldungs-Token")]
    [PasswordPropertyText(true)]
    [Editor(typeof(TypeEditor.TokenAuthentificationEditor), typeof(TypeEditor.ITypeEditor))]
    public string Token { get; set; }

    [Category("~Anmeldungs-Credentials")]
    [DisplayName("Ticket-Gültigkeit [min]")]
    public int TicketExpiration
    {
        get { return _expiration; }
        set { _expiration = value; }
    }

    /*
    [Category("Rest")]
    [DisplayName("Ticket-ClientId")]
    [Description("")]
    public string ClientID
    {
        get { return _clientId; }
        set { _clientId = value; }
    }
     * */

    [DisplayName("GetSelection Methode")]
    [Category("~~Selektion")]
    public AGSGetSelectionMothod GetSelectionMethod
    {
        get { return _selectionMethod; }
        set { _selectionMethod = value; }
    }

    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _server = (string)stream.Load("server", String.Empty);
        _service = (string)stream.Load("service", String.Empty);
        _serviceUrl = (string)stream.Load("serviceurl", String.Empty);
        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());
        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "agsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "agsservice").Replace(stream.StringReplace);
        _selectionMethod = (AGSGetSelectionMothod)stream.Load("getselectionmethod", (int)AGSGetSelectionMothod.Modern);

        _expiration = (int)stream.Load("expiration", 1440);
        _clientId = (string)stream.Load("clientid", _clientId);

        this.ExportMapFormat = (AGSExportMapFormat)stream.Load("exportmapformat", (int)AGSExportMapFormat.Json);
        this.DynamicPresentations = (WebMapping.Core.ServiceDynamicPresentations)stream.Load("dynamic_presentations", (int)WebMapping.Core.ServiceDynamicPresentations.Manually);
        this.DynamicQueries = (WebMapping.Core.ServiceDynamicQueries)stream.Load("dynamic_queries", (int)WebMapping.Core.ServiceDynamicQueries.Manually);
        this.ServiceType = (WebMapping.Core.ImageServiceType)stream.Load("service_type", (int)WebMapping.Core.ImageServiceType.Normal);
        this.DynamicDehavior = (WebMapping.Core.DynamicDehavior)stream.Load("dynamic_behavior", (int)WebMapping.Core.DynamicDehavior.AutoAppendNewLayers);
        this.AllowQueryBuilder = (bool)stream.Load("allow_querybuilder", false);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("server", _server ?? String.Empty);
        stream.Save("service", _service ?? String.Empty);
        stream.Save("serviceurl", _serviceUrl ?? String.Empty);
        stream.Save("guid", _guid.ToString());
        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "agsservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "agsservice"));
        stream.Save("getselectionmethod", (int)_selectionMethod);

        stream.Save("expiration", _expiration);
        stream.Save("clientid", _clientId);

        stream.Save("exportmapformat", (int)this.ExportMapFormat);
        stream.Save("dynamic_presentations", (int)this.DynamicPresentations);
        stream.Save("dynamic_queries", (int)this.DynamicQueries);
        stream.Save("service_type", (int)this.ServiceType);
        stream.Save("dynamic_behavior", (int)this.DynamicDehavior);
        stream.Save("allow_querybuilder", this.AllowQueryBuilder);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new EsriServiceControl(_servicePack, this.isInCopyMode);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + "/.general";
        }
        else
        {
            return ".general";
        }
    }
    async public Task<bool> CreatedAsync(string FullName)
    {
        await BuildServiceInfoAsync(FullName, false);
        return true;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region IRefreshable Member

    async public Task<bool> RefreshAsync(string FullName, int level)
    {
        await BuildServiceInfoAsync(FullName, true, level);
        return true;
    }

    #endregion

    #region Helper

    [Browsable(false)]
    internal bool AutoImportPresentations { get; set; }

    [Browsable(false)]
    internal bool AutoImportQueries { get; set; }

    [Browsable(false)]
    internal bool AutoImportEditing { get; set; }

    [Browsable(false)]
    public string[] ImportLayers { get; set; }

    async public Task BuildServiceInfoAsync(string fullName, bool refresh, int level = 0)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + "/themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        JsonLayers jsonLayers = null;

        try
        {
            string requestUrl = this.ServiceUrl + "/layers";
            string postBodyData = "f=pjson";
            string jsonStringAnswer = await TryPostAsync(requestUrl, postBodyData);

            jsonLayers = JSerializer.Deserialize<JsonLayers>(jsonStringAnswer); // equiv with map description
            jsonLayers.SetParentLayers();
        }
        catch (System.Exception)
        {
            throw;
        }

        List<string> urls = new List<string>();

        if (jsonLayers?.Layers == null || jsonLayers.Layers.Length == 0)
        {
            throw new System.Exception("Es konnten keine Layer ausgelesen werden.\nMöchten Sie trotzden fortfahren und alle (TOC)Themen dieses Dienstes aus dem CMS löschen?");
        }

        int counter = 0;
        List<string> existingLayerNames = ExistingThemeNames(fullName);
        foreach (var layerInfo in jsonLayers.Layers)
        {
            if (layerInfo.Type == "Group Layer" ||
                layerInfo.Type == "Annotation Layer")
            {
                continue;
            }
            counter++;
        }

        #region Überprüfen, ob beim Refresh layer gelöscht/hinzugefügt werden werden

        if (refresh && level == 0)
        {
            int oldT = this.CmsManager.CountConfigFiles(di.FullName + "/themes", "*.xml");
            int newT = counter;
            int divT = newT - oldT;
            if (divT < 0)
            {
                throw new RefreshConfirmException("Durch den Refresh " + (divT == -1 ? "wird" : "werden") + " " + (-divT).ToString() + " " + (divT == -1 ? "Thema" : "Themen") + " aus Dienst gelöscht.\nMöchten Sie trotzden fortfahren?");
            }
            else if (divT > 0)
            {
                throw new RefreshConfirmException("Durch den Refresh " + (divT == 1 ? "wird" : "werden") + " dem Dienst " + (divT).ToString() + " " + (divT == 1 ? "Thema" : "Themen") + " hinzugefügt.\nMöchten Sie fortfahren?");
            }
        }

        #endregion

        #region TOC

        TOC toc = new TOC();
        toc.Name = "default";
        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        toc.Save(xmlStream);
        var fi = DocumentFactory.DocumentInfo(di.FullName + "/tocs/" + toc.CreateAs(true) + ".xml");
        xmlStream.SaveDocument(fi.FullName);
        var di_tocs_default = fi.Directory;

        List<string> themeLinkUris = new List<string>();

        int c = 0;
        foreach (var layerInfo in jsonLayers.Layers)
        {
            if (layerInfo.Type == "Group Layer" ||
                layerInfo.Type == "Annotation Layer")
            {
                c++;
                continue;
            }

            if (ImportLayers != null && !ImportLayers.Contains(layerInfo.Id.ToString()))
            {
                continue;
            }

            var parentLayerInfo = layerInfo.ParentLayer;

            ServiceLayer layer = new ServiceLayer();
            if (parentLayerInfo != null &&
                parentLayerInfo.Type == "Annotation Layer")
            {
                layer.Name = ParentLayerName(layerInfo, jsonLayers.Layers) + parentLayerInfo.Name + " (" + layerInfo.Name + ")";
            }
            else
            {
                layer.Name = ParentLayerName(layerInfo, jsonLayers.Layers) + layerInfo.Name;
            }

            layer.Url = Crypto.GetID();
            layer.Id = layerInfo.Id.ToString();

            layer.Visible = ParentLayerVisibility(layerInfo, jsonLayers.Layers, layerInfo.DefaultVisibility);

            c++;

            string themeLinkUri = ThemeExists(fullName, layer, false, null);
            if (refresh && String.IsNullOrEmpty(themeLinkUri) && ImportLayers == null)
            {
                themeLinkUri = ThemeExists(fullName, layer, true, existingLayerNames);
            }

            bool save = true;
            if (refresh && !String.IsNullOrEmpty(themeLinkUri))
            {
                save = false;

                #region Überprüfen, ob LayerID und LayerName noch zusammenpasst!

                string eFilename = di.FullName + "/themes/" + IMSService.ExtractLastPathElement(themeLinkUri) + ".xml";
                IStreamDocument eXmlSteam = DocumentFactory.Open(eFilename);
                ServiceLayer eLayer = new ServiceLayer();
                eLayer.Load(eXmlSteam);
                if (eLayer.Id != layer.Id
                    || eLayer.Name != layer.Name
                    || eLayer.Visible != layer.Visible)
                {
                    // wenn nicht -> neu abspeichern!!!
                    xmlStream = DocumentFactory.New(di.FullName);
                    layer.Save(xmlStream);
                    xmlStream.SaveDocument(eFilename);
                }

                #endregion

                themeLinkUris.Add(themeLinkUri);
            }

            if (save)
            {
                urls.Add(layer.Url);
                xmlStream = DocumentFactory.New(di.FullName);
                layer.Save(xmlStream);
                xmlStream.SaveDocument(di.FullName + "/themes/" + layer.CreateAs(true) + ".xml");

                themeLinkUri = ThemeExists(fullName, layer, false, null);
                themeLinkUris.Add(themeLinkUri);
            }

            if (!IMSService.TocElementExists(di_tocs_default.FullName, themeLinkUri))
            {
                TocTheme tocTheme = new TocTheme();
                tocTheme.LinkUri = themeLinkUri;
                tocTheme.AliasName = layer.Name;
                tocTheme.Visible = layer.Visible;

                string tocThemeConfig = di_tocs_default.FullName + "/l" + GuidEncoder.Encode(Guid.NewGuid()).ToString().ToLower() + ".link";
                xmlStream = DocumentFactory.New(di.FullName);
                tocTheme.Save(xmlStream);
                xmlStream.SaveDocument(tocThemeConfig);
            }
        }
        if (this.CmsManager != null)
        {
            this.CmsManager.ThinPath(di.FullName + "/themes", themeLinkUris, "*.xml");
        }

        ItemOrder itemOrder = new ItemOrder(di.FullName + "/themes");
        itemOrder.Items = urls.ToArray();
        itemOrder.Save();

        #endregion

        #region Presentations

        if (refresh == false && AutoImportPresentations == true)
        {
            string rootPath = di.Parent.Parent.Parent.Parent.FullName;

            Dictionary<string, List<string>> presentationUrls = new Dictionary<string, List<string>>();
            presentationUrls.Add(String.Empty, new List<string>());

            #region Container Node

            var containerNode = new ContainerNode();
            containerNode.Url = this.Url;
            containerNode.Name = this.Name;

            string containerNodePath = rootPath + "/gdi/presentations/" + containerNode.CreateAs(true) + ".xml";
            xmlStream = DocumentFactory.New(rootPath + "/gdi/presentations/");
            containerNode.Save(xmlStream);
            xmlStream.SaveDocument(containerNodePath);

            #endregion

            foreach (var layerInfo in jsonLayers.Layers.Where(l => l.Type == "Feature Layer" ||
                                                                   l.Type == "Reaster Layer" ||
                                                                   l.Type == "Annotation SubLayer"))
            {
                string theme = layerInfo.FullName;

                #region Layer Switch

                var presentation = new Presentation(_servicePack);
                presentation.Url = Helper.ToValidUrl("dv_" + theme);
                presentation.Name = layerInfo.Name;
                presentation.LayerNames = theme;
                presentation.CmsManager = this.CmsManager;
                presentation.RelativePath = $"services/arcgisserver/mapserver/{this.Url}/presentations/{presentation.CreateAs(false)}";

                string presentationPath = di.FullName + "/presentations/" + presentation.CreateAs(false) + ".xml";
                xmlStream = DocumentFactory.New(this.ServiceUrl + "/presentations/");
                presentation.Save(xmlStream);
                xmlStream.SaveDocument(presentationPath);
                await presentation.CreatedAsync(presentationPath);

                #endregion

                #region TOC Group

                var groupNames = theme.Split('\\');
                string groupPath = String.Empty, groupUrl = String.Empty, uiGroupName = String.Empty;
                if (groupNames.Length > 1)
                {
                    var presentationGroup = new PresentationGroupGdi();
                    presentationGroup.Url = groupUrl = Helper.ToValidUrl(groupNames[0]);
                    presentationGroup.Name = groupNames[0];
                    presentationGroup.DisplayStyle = PresentationGroupStyle.Dropdown;

                    if (!presentationUrls.ContainsKey(presentationGroup.Url))
                    {
                        presentationUrls.Add(presentationGroup.Url, new List<string>());

                        string presentationGroupPath = $"{rootPath}/gdi/presentations/{containerNode.Url}/{presentationGroup.CreateAs(true)}.xml";
                        xmlStream = DocumentFactory.New("");
                        presentationGroup.Save(xmlStream);
                        xmlStream.SaveDocument(presentationGroupPath);

                        presentationUrls[String.Empty].Add(groupUrl);
                    }

                    groupPath = $"{presentationGroup.Url}/";
                    if (groupNames.Length > 2)
                    {
                        uiGroupName = String.Join("/", groupNames.Take(groupNames.Length - 1).Skip(1));
                    }
                }

                #endregion

                #region TOC Presentation

                var presentationLink = new PresentationLinkGdi();
                presentationLink.LinkUri = presentation.RelativePath;
                presentationLink.DisplayStyle = PresentationLinkCheckMode.CheckBox;
                presentationLink.Visible = true;
                presentationLink.ClientVisibility = ClientVisibility.Any;
                presentationLink.VisibleWithService = true;
                presentationLink.UIGroupName = uiGroupName;

                string presentationLinkName = Helper.NewLinkName();
                string presentationLinkPath = $"{rootPath}/gdi/presentations/{containerNode.Url}/{groupPath}{presentationLinkName}";
                xmlStream = DocumentFactory.New($"{rootPath}/gdi/presentations/{containerNode.Url}");
                presentationLink.Save(xmlStream);
                xmlStream.SaveDocument(presentationLinkPath);

                presentationUrls[groupUrl].Add(presentationLinkName);

                #endregion
            }

            #region ItemOrder

            foreach (var groupUrl in presentationUrls.Keys)
            {
                itemOrder = new ItemOrder($"{rootPath}/gdi/presentations/{containerNode.Url}{(String.IsNullOrEmpty(groupUrl) ? "" : "/" + groupUrl)}");
                itemOrder.Items = presentationUrls[groupUrl].ToArray();
                itemOrder.Save();
            }

            #endregion
        }

        #endregion

        #region Queries

        var nameurlControl = new NameUrlControl();
        if (refresh == false && AutoImportQueries == true)
        {
            urls.Clear();

            foreach (var layerInfo in jsonLayers.Layers.Where(l => l.Type == "Feature Layer"))
            {
                ServiceLayer layer = new ServiceLayer();
                layer.Name = ParentLayerName(layerInfo, jsonLayers.Layers) + layerInfo.Name;
                layer.Id = layerInfo.Id.ToString();

                var query = new Query(_servicePack);
                query.Name = layerInfo.Name;
                query.Url = nameurlControl.NameToUrl(query.Name);
                string createAs = query.CreateAs(true).Replace("\\", "/");

                if (DocumentFactory.DocumentInfo($"{di.FullName}/queries/{createAs}.xml").Exists)
                {
                    continue;
                }

                query.QueryThemeId = layerInfo.Id.ToString();
                query.CmsManager = this.CmsManager;
                query.RelativePath = $"services/arcgisserver/mapserver/{this.Url}/queries/{createAs}";

                query.AutoImportAllFields = ImportQueryTable.Dynamic;

                xmlStream = DocumentFactory.New(di.FullName);
                query.Save(xmlStream);
                xmlStream.SaveDocument(di.FullName + "/queries/" + createAs + ".xml");

                var schemaNode = this.CmsManager.SchemaNode($"services/arcgisserver/mapserver/{this.Url}/queries/{query.Url}");
                var pathInfo = DocumentFactory.PathInfo(di.FullName + "/queries/" + query.Url);
                this.CmsManager.ParseSchemaNode(_servicePack, pathInfo, schemaNode);

                await query.CreatedAsync($"services/arcgisserver/mapserver/{this.Url}/queries/{createAs}");

                urls.Add(query.Url);
            }

            itemOrder = new ItemOrder(di.FullName + "/queries");
            itemOrder.Items = urls.ToArray();
            itemOrder.Save();
        }

        #endregion

        #region Editing

        if (refresh == false && AutoImportEditing == true)
        {
            JsonFeatureLayers jsonFeatureLayers = null;
            try
            {
                string requestUrl = this.ServiceUrl.Substring(0, this.ServiceUrl.ToLower().IndexOf("/mapserver")) + "/featureserver/layers";
                string postBodyData = "f=pjson";
                string jsonStringAnswer = await TryPostAsync(requestUrl, postBodyData);

                jsonFeatureLayers = JSerializer.Deserialize<JsonFeatureLayers>(jsonStringAnswer); // equiv with map description
            }
            catch
            {

            }

            if (jsonFeatureLayers != null)
            {
                urls.Clear();

                foreach (var jsonFeatureLayer in jsonFeatureLayers.EditableLayers())
                {
                    var editingTheme = new EditingTheme(_servicePack);
                    editingTheme.Name = jsonFeatureLayer.Name;
                    editingTheme.Url = nameurlControl.NameToUrl(editingTheme.Name);
                    string createAs = editingTheme.CreateAs(true).Replace("\\", "/");

                    if (DocumentFactory.DocumentInfo($"{di.FullName}/editing/{createAs}.xml").Exists)
                    {
                        continue;
                    }

                    editingTheme.EditingThemeId = jsonFeatureLayer.Id.ToString();
                    editingTheme.AutoImportEditFields = ImportEditFields.Fields;
                    //editingTheme.JsonFeatureLayer = jsonFeatureLayer;
                    editingTheme.Srs = jsonFeatureLayer.Extent?.SpatialReference?.Wkid ?? 0;

                    editingTheme.CmsManager = this.CmsManager;
                    editingTheme.RelativePath = $"services/arcgisserver/mapserver/{this.Url}/editing/{createAs}";

                    xmlStream = DocumentFactory.New(di.FullName);
                    editingTheme.Save(xmlStream);
                    xmlStream.SaveDocument(di.FullName + "/editing/" + createAs + ".xml");

                    var schemaNode = this.CmsManager.SchemaNode($"services/arcgisserver/mapserver/{this.Url}/editing/{editingTheme.Url}");
                    var pathInfo = DocumentFactory.PathInfo(di.FullName + "/editing/" + editingTheme.Url);
                    this.CmsManager.ParseSchemaNode(_servicePack, pathInfo, schemaNode);

                    await editingTheme.CreatedAsync($"services/arcgisserver/mapserver/{this.Url}/editing/{createAs}");

                    urls.Add(editingTheme.Url);
                }

                itemOrder = new ItemOrder(di.FullName + "/editing");
                itemOrder.Items = urls.ToArray();
                itemOrder.Save();
            }
        }

        #endregion
    }

    private JsonLayer LayerInfoByID(IEnumerable<JsonLayer> infos, int id)
    {
        foreach (var info in infos)
        {
            if (info.Id == id)
            {
                return info;
            }
        }
        return null;
    }

    private string ThemeExists(string fullName, ServiceLayer layer, bool fuzzy, List<string> exisitingLayerNames)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + "/themes");
        if (!di.Exists)
        {
            return String.Empty;
        }

        string ret = String.Empty;

        foreach (var fi in di.GetFiles("*.xml"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            ServiceLayer l = new ServiceLayer();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            l.Load(xmlStream);

            if (l.Name == layer.Name)
            {
                return "services/arcgisserver/mapserver/" + this.Url + "/themes/" + l.Url;
            }
        }

        if (fuzzy)
        {
            bool foundExact = exisitingLayerNames != null && exisitingLayerNames.Contains(layer.Name);  // Layer comes later
            if (foundExact)
            {
                return String.Empty;
            }

            foreach (var fi in di.GetFiles("*.xml"))
            {
                if (fi.Name.StartsWith("."))
                {
                    continue;
                }

                ServiceLayer l = new ServiceLayer();
                IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
                l.Load(xmlStream);

                if (String.IsNullOrEmpty(ret) && ShortLayerName(l.Name) == ShortLayerName(layer.Name))
                {
                    ret = "services/arcgisserver/mapserver/" + this.Url + "/themes/" + l.Url;
                }
                else if (!String.IsNullOrEmpty(ret) && ShortLayerName(l.Name) == ShortLayerName(layer.Name) && l.Id == layer.Id)
                {
                    ret = "services/arcgisserver/mapserver/" + this.Url + "/themes/" + l.Url;
                }
            }
        }

        return ret;
    }

    private List<string> ExistingThemeNames(string fullName)
    {
        List<string> ret = new List<string>();

        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + "/themes");
        if (!di.Exists)
        {
            return ret;
        }

        foreach (var fi in di.GetFiles("*.xml"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            ServiceLayer l = new ServiceLayer();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            l.Load(xmlStream);

            ret.Add(l.Name);
        }

        return ret;
    }

    [Category("~~Soap")]
    public string SoapServiceUrl
    {
        get
        {
            return GetSoapUrl(_serviceUrl);
        }
    }

    [Category("~~Soap")]
    public string SoapServer
    {
        get
        {
            return GetSoapUrl(_server);
        }
    }
    private string GetSoapUrl(string serviceUrl)
    {
        if (serviceUrl.ToLower().Contains("/rest/"))
        {
            int pos = serviceUrl.ToLower().IndexOf("/rest/");
            string soapUrl = serviceUrl.Substring(0, pos + 1) + serviceUrl.Substring(pos + 6, serviceUrl.Length - pos - 6);
            return soapUrl;
        }

        return serviceUrl;
    }


    internal static string ShortLayerName(string layername)
    {
        if (layername.Contains("\\"))
        {
            int pos = layername.LastIndexOf("\\");
            return layername.Substring(pos + 1, layername.Length - pos - 1);
        }

        return layername;
    }

    private static JsonLayer MapLayerInfoByID(IEnumerable<JsonLayer> infos, int id)
    {
        if (id < 0)
        {
            return null;
        }

        foreach (var info in infos)
        {
            if (info.Id == id)
            {
                return info;
            }
        }
        return null;
    }

    private string ParentLayerName(JsonLayer layer, IEnumerable<JsonLayer> layers)
    {
        if (layer == null || layers == null || layer.ParentLayer == null)
        {
            return String.Empty;
        }

        return ParentLayerName(layer.ParentLayer, layers) + layer.ParentLayer.Name + "\\";
    }

    private bool ParentLayerVisibility(JsonLayer layer, IEnumerable<JsonLayer> layers, bool layerDefaultVisiblity)
    {
        if (layerDefaultVisiblity == false)
        {
            return false;
        }

        if (layer?.ParentLayer == null)
        {
            return layerDefaultVisiblity;
        }

        if (layer.ParentLayer.DefaultVisibility == false)
        {
            return false;
        }

        return ParentLayerVisibility(layer.ParentLayer, layers, layerDefaultVisiblity);
    }

    private string _tokenParam = String.Empty;

    async internal Task<string> TryPostAsync(string requestUrl, string postBodyData)
    {
        int i = 0;
        while (true)
        {
            try
            {
                string tokenParameter = String.Empty;
                if (!String.IsNullOrWhiteSpace(_tokenParam))
                {
                    tokenParameter = (String.IsNullOrWhiteSpace(postBodyData) ? "" : "&") + "token=" + _tokenParam;
                }

                string ret = String.Empty;
                try
                {
                    ret = await _servicePack.HttpService.PostFormUrlEncodedStringAsync(
                                                                          AppendReverseProxyTokenParameter(requestUrl),
                                                                          $"{postBodyData}{tokenParameter}",
                                                                          new RequestAuthorization() { Credentials = _credentials });
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("(403)") ||
                        ex.Message.Contains("(498)") ||
                        ex.Message.Contains("(499)"))
                    {
                        throw new TokenRequiredException();
                    }
                    throw;
                }
                catch (HttpServiceException httpEx)
                {
                    if (httpEx.StatusCode == HttpStatusCode.Forbidden /* 403 */ ||
                        (int)httpEx.StatusCode == 498 ||
                        (int)httpEx.StatusCode == 499)
                    {
                        throw new TokenRequiredException();
                    }

                    throw;
                }
                if (ret.Contains("\"error\":"))
                {
                    JsonError error = JSerializer.Deserialize<JsonError>(ret);
                    if (error.error == null)
                    {
                        throw new System.Exception("Unknown error");
                    }

                    if (error.error.code == 499 || error.error.code == 498 || error.error.code == 403) // Token Required (499), Invalid Token (498), No user Persmissions (403)
                    {
                        throw new TokenRequiredException();
                    }

                    throw new System.Exception("Error:" + error.error.code + "\n" + error.error.message);
                }
                return ret;
            }
            catch (TokenRequiredException ex)
            {
                await HandleTokenExceptionAsync(i, ex);
            }
            i++;
        }
    }

    async private Task HandleTokenExceptionAsync(int i, TokenRequiredException ex)
    {
        if (i < 3)  // drei mal probieren lassen
        {
            var _ = await RefreshTokenAsync();
        }
        else
        {
            throw ex;
        }
    }

    private static readonly object _refreshTokenLocker = new object();
    private static readonly ThreadSafeDictionary<string, string> _tokenParams = new ThreadSafeDictionary<string, string>();
    private bool _tokenRequired = false;
    ICredentials _credentials = null;

    async public Task<string> RefreshTokenAsync()
    {
        //dotNETConnector conn = new dotNETConnector(
        //                        System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/dotNETConnector.xml", String.Empty, String.Empty);

        string currentParameter = _tokenParam;
        //lock (_refreshTokenLocker)
        {
            string dictKey = this.Server + "/" + this._user;

            if (_tokenParams.ContainsKey(dictKey) && _tokenParams[dictKey] != currentParameter)
            {
                _tokenParam = _tokenParams[dictKey];
            }
            else
            {
                string serviceUrl = this.ServiceUrl;
                if (String.IsNullOrWhiteSpace(serviceUrl))
                {
                    serviceUrl = GetServerName(this.Server, true) + "/";
                }

                int pos = serviceUrl.ToLower().IndexOf("/rest/");
                string tokenServiceUrl = serviceUrl.Substring(0, pos) + "/tokens/generateToken";

                //
                // 15.07.2019
                // Bei komplizerteren Passwörtern mit Sonderzeichen muss man URL Encoden
                // Da trat bisher nur beim gView Server auf. Die Frage ist, ob man das auch beim Esri AGS Server machen muss
                // Falls bei Kunden einmal irgendwas nicht geht, sollte man das überprüfen
                //
                string tokenParams = $"request=gettoken&username={this._user}&password={this._pwd.UrlEncodePassword()}&expiration={this.TicketExpiration}&f=json";

                string tokenResponse = String.Empty;
                while (true)
                {
                    try
                    {
                        tokenResponse = await _servicePack.HttpService.PostFormUrlEncodedStringAsync(tokenServiceUrl,
                                                                          tokenParams
                                                                          /*,new RequestAuthorization() { Credentials = _credentials }*/);
                        break;
                    }
                    catch (WebException we)
                    {
                        if (we.Message.Contains("(502)") && tokenServiceUrl.StartsWith("http://"))
                        {
                            tokenServiceUrl = "https:" + tokenServiceUrl.Substring(5);
                            continue;
                        }
                        throw;
                    }
                }
                if (tokenResponse.Contains("\"error\":"))
                {
                    JsonError error = JSerializer.Deserialize<JsonError>(tokenResponse);
                    if (error.error == null)
                    {
                        throw new Exception("GetToken-Error: unknown error");
                    }
                    else
                    {
                        throw new System.Exception("GetToken-Error:" + error.error.code + "\n" + error.error.message + "\n" +
                            (error.error.details != null ? String.Empty : error.error.details?.ToString()) +
                            "\nUser=" + _user);
                    }
                }
                else
                {
                    JsonSecurityToken jsonToken = JSerializer.Deserialize<JsonSecurityToken>(tokenResponse);
                    if (jsonToken.token != null)
                    {
                        _tokenParam = jsonToken.token;
                        _tokenParams.Add(dictKey, _tokenParam);
                    }
                }
            }

            _tokenRequired = !String.IsNullOrEmpty(_tokenParam);


            if (!String.IsNullOrEmpty(_user) && _tokenRequired == false)
            {
                _credentials = new NetworkCredential(_user, _pwd);
            }
            else
            {
                _credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            }
        }

        return _tokenParam;
    }

    async public Task<string[]> GetServicesAsync(string type = "mapserver", string folder = "")
    {
        string server = GetServerName(this.Server, true);

        List<string> services = new List<string>();

        if (server.ToLower().EndsWith("/mapserver") && server.Split('/').Length > 2)
        {
            var response = await TryPostAsync(server + "?f=json", String.Empty);
            if (response.Trim().StartsWith("{"))
            {
                services.Add(server.Split('/')[server.Split('/').Length - 2]);
            }
        }
        else
        {
            var response = await TryPostAsync(server + "/services" + (String.IsNullOrEmpty(folder) ? "" : "/" + folder) + "?f=json", String.Empty);

            var jsonServices = JSerializer.Deserialize<JsonServices>(response);
            if (jsonServices.Services != null)
            {
                services.AddRange(jsonServices.Services.Where(s => s.Type.ToLower() == type.ToLower()).Select(s => s.Name));
            }

            if (jsonServices.Folders != null && jsonServices.Folders.Count() > 0)
            {
                foreach (var subFolder in jsonServices.Folders)
                {
                    try
                    {
                        services.AddRange(await GetServicesAsync(type, !String.IsNullOrEmpty(folder) ? folder + "/" + subFolder : subFolder));
                    }
                    catch (Exception)
                    {
                        // ignore Errors here. A folder can be authorized => not an error, just dont list services if user not authorized
                    }
                }
            }
        }

        return services.ToArray();
    }

    public string GetServerName(string server, bool rest = true)
    {
        if (server.IsSecretPlaceholder())
        {
            // do nothing
        }
        else
        {
            if (!server.Contains("/"))
            {
                server += "/arcgis" + (rest ? "/rest" : "");
            }

            if (rest && !server.ToLower().EndsWith("/rest") && !server.ToLower().EndsWith("/mapserver"))
            {
                server += "/rest";
            }

            if (!server.ToLower().StartsWith("http://") && !server.ToLower().StartsWith("https://"))
            {
                server = CmsSchemaGlobals.ServicesDefaultUrlScheme + server;
            }
        }

        return server;
    }

    async public Task<IEnumerable<string>> GetLayerFieldNamesAsync(string layerId)
    {
        string service = this.ServiceUrl + "/layers?f=json";
        if (!service.ToLower().Contains("/rest/services/"))
        {
            service = service.Replace("/services/", "/rest/services/");
        }

        string response = await TryPostAsync(service, String.Empty);

        JsonLayers jsonLayers = JSerializer.Deserialize<JsonLayers>(response);

        var layer = jsonLayers.Layers.Where(l => l.Id.ToString() == layerId).FirstOrDefault();
        if (layer == null)
        {
            return new string[0];
        }

        return layer.Fields.Select(f => f.Name);
    }

    async public Task<IEnumerable<JsonField>> GetAllLayerFields()
    {
        string service = this.ServiceUrl + "/layers?f=json";
        if (!service.ToLower().Contains("/rest/services/"))
        {
            service = service.Replace("/services/", "/rest/services/");
        }

        string response = await TryPostAsync(service, String.Empty);

        JsonLayers jsonLayers = JSerializer.Deserialize<JsonLayers>(response);

        var allFields = jsonLayers.Layers.Where(l => l?.Fields != null).SelectMany(l => l.Fields).ToArray();

        return allFields.DistinctBy(f => f.Name);
    }

    async public Task<IEnumerable<JsonField>> GetLayerFieldsAsync(string layerId)
    {
        string service = this.ServiceUrl + "/layers?f=json";
        if (!service.ToLower().Contains("/rest/services/"))
        {
            service = service.Replace("/services/", "/rest/services/");
        }

        string response = await TryPostAsync(service, String.Empty);

        JsonLayers jsonLayers = JSerializer.Deserialize<JsonLayers>(response);

        var layer = jsonLayers.Layers.Where(l => l.Id.ToString() == layerId).FirstOrDefault();
        if (layer == null)
        {
            return new JsonField[0];
        }

        return layer.Fields.Select(f => f);
    }

    async public Task<IEnumerable<(string, string)>> GetLayerFieldsAndAliasesAsync(string layerId)
    {
        string service = this.ServiceUrl + "/layers?f=json";
        if (!service.ToLower().Contains("/rest/services/"))
        {
            service = service.Replace("/services/", "/rest/services/");
        }

        string response = await TryPostAsync(service, String.Empty);

        JsonLayers jsonLayers = JSerializer.Deserialize<JsonLayers>(response);

        var layer = jsonLayers.Layers.Where(l => l.Id.ToString() == layerId).FirstOrDefault();
        if (layer == null)
        {
            return new List<(string, string)>();
        }

        return layer.Fields.Select(f => (f.Name, String.IsNullOrWhiteSpace(f.Alias) ? f.Name : f.Alias));
    }

    async public Task<IEnumerable<JsonLayer>> GetLayersWithGroupLayernamesAsync()
    {
        try
        {
            string service = $"{this.ServiceUrl}/layers?f=json";
            if (!service.ToLower().Contains("/rest/services/"))
            {
                service = service.Replace("/services/", "/rest/services/");
            }

            string response = await TryPostAsync(service, String.Empty);

            var jsonLayers = JSerializer.Deserialize<JsonLayers>(response);
            return jsonLayers?
                .Layers?
                .Where(l => l.Type != "Group Layer" && l.Type != "Annotation Layer")
                .Select(l =>
                {
                    l.Name = $"{ParentLayerName(l, jsonLayers.Layers)}{l.Name}";
                    return l;
                })
                ?? Array.Empty<JsonLayer>();
        }
        catch
        {
            return Array.Empty<JsonLayer>();
        }
    }

    async public Task<(string description, string mapName)> GetServiceDescriptionAsync()
    {
        try
        {
            string server = GetServerName(this.Server, true);

            List<string> services = new List<string>();

            var response = await TryPostAsync(this.ServiceUrl + "?f=json", String.Empty);
            var jsonService = JSerializer.Deserialize<JsonService>(response);

            StringBuilder description = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(jsonService.ServiceDescription))
            {
                description.Append(jsonService.ServiceDescription + Environment.NewLine);
            }
            if (!String.IsNullOrWhiteSpace(jsonService.Description) && jsonService.Description != jsonService.ServiceDescription)
            {
                description.Append(jsonService.Description + Environment.NewLine);
            }
            if (!String.IsNullOrWhiteSpace(jsonService.CopyrightText))
            {
                description.Append(jsonService.CopyrightText + Environment.NewLine);
            }

            return (description.ToString(), jsonService.MapName);
        }
        catch (Exception ex)
        {
            return ("Exception: " + ex.Message, String.Empty);
        }
    }

    async public Task<int> GetServiceSRef()
    {
        string service = this.ServiceUrl + "?f=json";
        if (!service.ToLower().Contains("/rest/services/"))
        {
            service = service.Replace("/services/", "/rest/services/");
        }

        string response = await TryPostAsync(service, String.Empty);

        JsonService jsonService = JSerializer.Deserialize<JsonService>(response);

        if (jsonService?.SpatialReference != null)
        {
            return jsonService.SpatialReference.Wkid;
        }

        return 0;
    }

    #endregion

    protected override void BeforeCopy()
    {
        base.BeforeCopy();
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "ArcGIS Server Dienst"; }
    }

    #region Helper

    private string AppendReverseProxyTokenParameter(string url)
    {
        if (!String.IsNullOrWhiteSpace(this.Token))
        {
            string tokenParameter = this.Token.Contains("=") ? this.Token : $"token={this.Token}";

            url += (url.Contains("?") ? "&" : "?") + tokenParameter;
        }

        return url;
    }

    #endregion
}