using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wms;
using E.Standard.OGC.Schema.wms_1_1_1;
using E.Standard.OGC.Schema.wms_1_3_0;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using E.Standard.WebMapping.Core.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class WMSService : CopyableNode, IAuthentification, IClientCertification, ICreatable, IEditable, IUI, IDisplayName, IRefreshable
{
    private string _user, _pwd, _server, _imageformat, _gfiformat;
    private WMS_Version _version;
    private string _guid;
    private string _certificate = String.Empty, _certificatePwd = String.Empty;
    private int _featureCount = 30;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public WMSService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        TicketServer = String.Empty;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        this.DynamicPresentations = WebMapping.Core.ServiceDynamicPresentations.Auto;
        this.DynamicQueries = WebMapping.Core.ServiceDynamicQueries.Auto;
        this.DynamicDehavior = WebMapping.Core.DynamicDehavior.UseStrict;
    }

    #region Properties

    [DisplayName("#dynamic_presentations")]
    public WebMapping.Core.ServiceDynamicPresentations DynamicPresentations { get; set; }

    [DisplayName("#dynamic_queries")]
    public WebMapping.Core.ServiceDynamicQueries DynamicQueries { get; set; }

    [DisplayName("#dynamic_dehavior")]
    public WebMapping.Core.DynamicDehavior DynamicDehavior { get; set; }

    [DisplayName("#service_type")]
    public WebMapping.Core.ImageServiceType ServiceType { get; set; }

    [DisplayName("#layer_order")]
    public WMS_LayerOrder LayerOrder { get; set; }

    [DisplayName("#vendor")]
    public WMS_Vendor Vendor { get; set; }

    [DisplayName("#server")]
    [Category("#category_server")]
    public string Server
    {
        get { return _server; }
        set { _server = value; }
    }
    [DisplayName("#version")]
    [Category("#category_version")]
    public WMS_Version Version
    {
        get { return _version; }
        set { _version = value; }
    }
    [DisplayName("#image_format")]
    [Category("#category_image_format")]
    public string ImageFormat
    {
        get { return _imageformat; }
        set { _imageformat = value; }
    }
    [DisplayName("#get_feature_info_format")]
    [Category("#category_get_feature_info_format")]
    public string GetFeatureInfoFormat
    {
        get { return _gfiformat; }
        set { _gfiformat = value; }
    }
    [DisplayName("#get_feature_info_feature_count")]
    [Category("#category_get_feature_info_feature_count")]
    public int GetFeatureInfoFeatureCount
    {
        get { return _featureCount; }
        set { _featureCount = value; }
    }

    [DisplayName("#s_l_d_version")]
    [Category("#category_s_l_d_version")]
    public SLD_Version SLDVersion { get; set; }

    [Category("~~#category_ticket_server")]
    [DisplayName("#ticket_server")]
    public string TicketServer
    {
        get;
        set;
    }

    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _server = (string)stream.Load("server", String.Empty);
        _version = (WMS_Version)stream.Load("version", (int)WMS_Version.version_1_1_1);
        _imageformat = (string)stream.Load("getmapformat", "image/png");
        _gfiformat = (string)stream.Load("getfeatureinfoformat", "text/html");
        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());
        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "wmsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "wmsservice").Replace(stream.StringReplace);
        _certificate = (string)stream.Load("cert", String.Empty);
        _certificatePwd = CmsCryptoHelper.Decrypt((string)stream.Load("certpwd", String.Empty), "WmsServiceCertificatePassword").Replace(stream.StringReplace);
        _featureCount = (int)stream.Load("featurecount", 30);
        
        this.SLDVersion = (SLD_Version)stream.Load("sld_version", (int)SLD_Version.unused);
        this.LayerOrder = (WMS_LayerOrder)stream.Load("layerorder", (int)WMS_LayerOrder.Up);
        this.Vendor = (WMS_Vendor)stream.Load("vendor", (int)WMS_Vendor.Unknown);

        this.DynamicPresentations = (WebMapping.Core.ServiceDynamicPresentations)stream.Load("dynamic_presentations", (int)WebMapping.Core.ServiceDynamicPresentations.Manually);
        this.DynamicQueries = (WebMapping.Core.ServiceDynamicQueries)stream.Load("dynamic_queries", (int)WebMapping.Core.ServiceDynamicQueries.Manually);
        this.ServiceType = (WebMapping.Core.ImageServiceType)stream.Load("service_type", (int)WebMapping.Core.ImageServiceType.Normal);
        this.DynamicDehavior = (WebMapping.Core.DynamicDehavior)stream.Load("dynamic_behavior", (int)WebMapping.Core.DynamicDehavior.AutoAppendNewLayers);

        TicketServer = (string)stream.Load("TicketServer", String.Empty);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("server", _server);
        stream.Save("version", (int)_version);
        stream.Save("getmapformat", (_imageformat != null ? _imageformat : "image/png"));
        stream.Save("getfeatureinfoformat", (_gfiformat != null ? _gfiformat : "text/html"));
        stream.Save("guid", _guid.ToString());

        if (!String.IsNullOrWhiteSpace(_user))
        {
            stream.Save("user", _user);
        }

        if (!String.IsNullOrWhiteSpace(_pwd))
        {
            stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "wmsservice"));
        }

        if (!String.IsNullOrWhiteSpace(this.Token))
        {
            stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "wmsservice"));
        }

        if (!String.IsNullOrWhiteSpace(_certificate))
        {
            stream.Save("cert", _certificate);
        }

        if (!String.IsNullOrWhiteSpace(_certificatePwd))
        {
            stream.Save("certpwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_certificatePwd), "WmsServiceCertificatePassword"));
        }

        stream.Save("featurecount", _featureCount);

        if (SLDVersion != SLD_Version.unused)
        {
            stream.Save("sld_version", (int)SLDVersion);
        }

        stream.Save("layerorder", (int)this.LayerOrder);
        stream.Save("vendor", (int)this.Vendor);

        stream.Save("dynamic_presentations", (int)this.DynamicPresentations);
        stream.Save("dynamic_queries", (int)this.DynamicQueries);
        stream.Save("service_type", (int)this.ServiceType);
        stream.Save("dynamic_behavior", (int)this.DynamicDehavior);

        stream.Save("TicketServer", TicketServer.Trim());
    }

    #endregion

    #region IAuthentification Member

    [DisplayName("#username")]
    [Category("#category_username")]
    public string Username
    {
        get { return _user; }
        set { _user = value; }
    }

    [DisplayName("#password")]
    [Category("#category_password")]
    [PasswordPropertyText(true)]
    public string Password
    {
        get { return _pwd; }
        set { _pwd = value; }
    }

    [DisplayName("#token")]
    [Category("#category_token")]
    [PasswordPropertyText(true)]
    [Editor(typeof(TypeEditor.TokenAuthentificationEditor), typeof(TypeEditor.ITypeEditor))]
    public string Token { get; set; }

    #endregion

    #region IClientCertification
    [Category("#category_client_certificate")]
    [DisplayName("#client_certificate")]
    [Editor(typeof(TypeEditor.ClientCertificateEditor), typeof(TypeEditor.ITypeEditor))]
    public string ClientCertificate
    {
        get { return _certificate; }
        set { _certificate = value; }
    }
    [Browsable(false)]
    public string ClientCertificatePassword
    {
        get { return _certificatePwd; }
        set { _certificatePwd = value; }
    }
    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"/.general";
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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new WMSServiceControl(_servicePack, this.isInCopyMode);
        ip.InitParameter = this;

        return ip;
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

    async public Task<bool> RefreshAsync(string fullName, int level)
    {
        await BuildServiceInfoAsync(fullName, true, level);

        return true;
    }

    #endregion

    #region Helper

    [Browsable(false)]
    public string[] ImportLayers { get; set; }

    async public Task BuildServiceInfoAsync(string fullName, bool refresh, int level = 0)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"/themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        CapabilitiesHelper capsHelper = null;
        TicketClient ticketClient = null;
        String ticket = String.Empty;

        try
        {
            RequestAuthorization requestAuthorization = null;

            if ((!String.IsNullOrEmpty(_user) &&
                 !String.IsNullOrEmpty(_pwd)) ||
                ClientCertificateControl.X509CertificateByName(_certificate, _certificatePwd) != null)
            {
                requestAuthorization = new RequestAuthorization()
                {
                    Username = _user,
                    Password = _pwd,
                    ClientCerticate = ClientCertificateControl.X509CertificateByName(_certificate, _certificatePwd)
                };
            }

            string url = _server;

            ticketClient = !String.IsNullOrEmpty(TicketServer.Trim()) ? new TicketClient(TicketServer) : null;
            if (ticketClient != null)
            {
                url = AppendToUrl(url, "ogc_ticket=" + (ticket = ticketClient.Login(_user, _pwd)));
            }

            if (_version == WMS_Version.version_1_1_1)
            {
                Serializer<WMT_MS_Capabilities> ser = new Serializer<WMT_MS_Capabilities>();
                url = AppendToUrl(url, "VERSION=1.1.1&SERVICE=WMS&REQUEST=GetCapabilities");
                WMT_MS_Capabilities caps = await ser.FromUrlAsync(url, _servicePack.HttpService, requestAuthorization);
                capsHelper = new CapabilitiesHelper(caps);
            }
            else if (_version == WMS_Version.version_1_3_0)
            {
                Serializer<WMS_Capabilities> ser = new Serializer<WMS_Capabilities>();

                ser.AddReplaceNamespace("https://www.opengis.net/wms", "http://www.opengis.net/wms");
                ser.AddReplaceNamespace("https://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink");
                ser.AddReplaceNamespace("https://www.w3.org/2001/XMLSchema-instance", "http://www.w3.org/2001/XMLSchema-instance");

                url = AppendToUrl(url, "VERSION=1.3.0&SERVICE=WMS&REQUEST=GetCapabilities");
                WMS_Capabilities caps = await ser.FromUrlAsync(url, _servicePack.HttpService, requestAuthorization);
                capsHelper = new CapabilitiesHelper(caps);
            }
        }
        catch (System.Exception)
        {
            throw;
        }
        finally
        {
            if (ticketClient != null && !String.IsNullOrEmpty(ticket))
            {
                ticketClient.Logout(ticket);
            }
        }

        if (capsHelper.LayersWithStyle.Count == 0)
        {
            throw new System.Exception("Es konnten keine Layer ausgelesen werden.\nMöchten Sie trotzden fortfahren und alle (TOC)Themen dieses Dienstes aus dem CMS löschen?");
        }

        #region Überprüfen, ob beim Refresh layer gelöscht/hinzugefügt werden werden

        if (refresh && level == 0)
        {
            int oldT = this.CmsManager.CountConfigFiles(di.FullName + @"/themes", "*.xml");
            int newT = capsHelper.LayersWithStyle.Count;
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

        TOC toc = new TOC();
        toc.Name = "default";
        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        toc.Save(xmlStream);
        var fi = DocumentFactory.DocumentInfo(di.FullName + @"/tocs/" + toc.CreateAs(true) + ".xml");
        xmlStream.SaveDocument(fi.FullName);
        var di_tocs_default = fi.Directory;

        #region Remove Default Toc Elements
        foreach (var tocDi in fi.Directory.GetDirectories())
        {
            try { tocDi.Delete(true); }
            catch { }
        }
        foreach (var tocFi in fi.Directory.GetFiles("*.xml"))
        {
            if (tocFi.Name.StartsWith("."))
            {
                continue;
            }

            try { tocFi.Delete(); }
            catch { }
        }
        #endregion
        //IMSServerInfo si = new IMSServerInfo(ConnectorType.ServletExec, Server, 5300, Encoding.Default, true);
        //si.setAuthentification(_user, _pwd);

        //si.GetLayerInfo(Service, true);

        List<string> urls = new List<string>();
        List<string> themeLinkUris = new List<string>();
        Dictionary<string, string> subDirs = new Dictionary<string, string>();
        Dictionary<string, int> subDirsOrder = new Dictionary<string, int>();
        subDirsOrder.Add("", 1);

        foreach (OGC.Schema.wms.CapabilitiesHelper.WMSLayer wmslayer in capsHelper.LayersWithStyle)
        {
            if (ImportLayers != null && !ImportLayers.Contains(wmslayer.Name))
            {
                continue;
            }

            OGC.Schema.wms.CapabilitiesHelper.WMSLayer oLayer = capsHelper.LayerByName(wmslayer.Name);
            if (oLayer == null)
            {
                continue;
            }

            string layerName = wmslayer.Title, layerId = wmslayer.Name;
            bool layerVisible = false;

            ServiceLayer layer = new ServiceLayer();
            layer.Name = layerName;
            layer.Url = Crypto.GetID();
            layer.Id = layerId;
            layer.Visible = layerVisible;

            string themeLinkUri = ThemeExists(fullName, layer);
            bool save = true;

            if (refresh && !String.IsNullOrEmpty(themeLinkUri))
            {
                save = false;

                #region Überprüfen, ob LayerID noch zusammenpasst!

                string eFilename = di.FullName + @"/themes/" + IMSService.ExtractLastPathElement(themeLinkUri) + ".xml";
                IStreamDocument eXmlSteam = DocumentFactory.Open(eFilename);
                ServiceLayer eLayer = new ServiceLayer();

                eLayer.Load(eXmlSteam);
                if (eLayer.Id != layer.Id
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
                string layerFullName = di.FullName + @"/themes/" + layer.CreateAs(true) + ".xml";
                xmlStream.SaveDocument(layerFullName);

                themeLinkUri = ThemeExists(fullName, layer);
                themeLinkUris.Add(themeLinkUri);
            }

            if (!IMSService.TocElementExists(di_tocs_default.FullName, themeLinkUri))
            {
                string[] parts = oLayer.Title.Split('/');
                string subDir = String.Empty, subDirAlias = String.Empty;
                bool subCreated = false;
                int subOrder = 0;
                for (int i = 0; i < parts.Length - (oLayer.Styles.Count > 1 ? 0 : 1); i++)
                {
                    subDirAlias = (i == 0) ? parts[i] : subDirAlias + "/" + parts[i];
                    if (subDirs.ContainsKey(subDirAlias))
                    {
                        subDir = subDirs[subDirAlias];
                        subOrder = subDirsOrder[subDirAlias];
                    }
                    else
                    {
                        TocGroup tocGroup = new TocGroup();
                        tocGroup.CheckMode = (i < parts.Length - 1 ? TocGroupCheckMode.CheckBox : TocGroupCheckMode.OptionBox);
                        tocGroup.Name = parts[i];
                        string tocGroupConfig = di_tocs_default.FullName + subDir + @"/s" + subOrder.ToString().PadLeft(5, '0') + tocGroup.CreateAs(true) + ".xml";
                        var tFi = DocumentFactory.DocumentInfo(tocGroupConfig);
                        if (!tFi.Directory.Exists)
                        {
                            tFi.Directory.Create();
                        }
                        xmlStream = DocumentFactory.New(di.FullName);
                        tocGroup.Save(xmlStream);
                        xmlStream.SaveDocument(tocGroupConfig);
                        subDir = tFi.Directory.FullName.Substring(di_tocs_default.FullName.Length, tFi.Directory.FullName.Length - di_tocs_default.FullName.Length);
                        subCreated = true;
                        subDirs.Add(subDirAlias, subDir);
                        subDirsOrder.Add(subDirAlias, 1);
                    }
                }

                string aliasName = (oLayer.Styles.Count > 1 && capsHelper.LayerStyleByName(wmslayer.Name) != null) ? capsHelper.LayerStyleByName(wmslayer.Name).Title : layer.Name;
                TocTheme tocTheme = new TocTheme();
                tocTheme.LinkUri = themeLinkUri;
                tocTheme.AliasName = aliasName;
                tocTheme.Visible = (oLayer.Styles.Count > 1) ? (subCreated & layer.Visible) : layer.Visible;

                string tocThemeConfig = di_tocs_default.FullName + subDir + @"/l" + subOrder.ToString().PadLeft(5, '0') + GuidEncoder.Encode(Guid.NewGuid()).ToLower() + ".xml";
                xmlStream = DocumentFactory.New(di.FullName);
                tocTheme.Save(xmlStream);
                xmlStream.SaveDocument(tocThemeConfig);
                subOrder = subDirsOrder[subDirAlias]++;
            }
        }

        if (this.CmsManager != null)
        {
            this.CmsManager.ThinPath(di.FullName + @"/themes", themeLinkUris, "*.xml");
        }

        ItemOrder itemOrder = new ItemOrder(di.FullName + @"/themes");
        itemOrder.Items = urls.ToArray();
        itemOrder.Save();
    }

    private string ThemeExists(string fullName, ServiceLayer layer)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + @"/themes");

        if (!di.Exists)
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

            if (l.Name == layer.Name)
            {
                return "services/ogc/wms/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }

    internal static string AppendToUrl(string url, string parameter)
    {
        url = url.Trim();

        if (url.EndsWith("?"))
        {
            return $"{url}{parameter}";
        }

        if (url.Contains("?"))
        {
            return $"{url}&{parameter}";
        }

        return $"{url}?{parameter}";
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "WMS Dienst"; }
    }

    protected override void BeforeCopy()
    {
        base.BeforeCopy();

        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }
}
