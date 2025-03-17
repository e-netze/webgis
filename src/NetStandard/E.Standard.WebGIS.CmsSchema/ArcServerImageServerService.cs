﻿using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class ArcServerImageServerService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName
{
    private string _server;
    private string _service;
    private string _serviceUrl = String.Empty;
    private readonly string _tileUrl = String.Empty;
    private string _user = String.Empty;
    private string _pwd = String.Empty;
    private string _guid;

    private ArcIS_ImageFormat _imageFormat = ArcIS_ImageFormat.jpgpng;
    private ArcIS_PixelType _pixelType = ArcIS_PixelType.UNKNOWN;
    private string _nodata = String.Empty;
    private ArcIS_NoDataInterpretation _nodataInterpretation = ArcIS_NoDataInterpretation.esriNoDataMatchAny;
    private ArcIS_Interpolation _interpolation = ArcIS_Interpolation.RSP_BilinearInterpolation;
    private string _compressQaulity = String.Empty;
    private string _bandIDs = String.Empty;
    private string _mosaicRule = String.Empty;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ArcServerImageServerService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        this.DynamicPresentations = WebMapping.Core.ServiceDynamicPresentations.Auto;
        this.DynamicQueries = WebMapping.Core.ServiceDynamicQueries.Auto;
    }

    #region Properties

    [DisplayName("Darstellungsvariaten bereitstellen")]
    [Description("Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.")]
    public WebMapping.Core.ServiceDynamicPresentations DynamicPresentations { get; set; }
    [DisplayName("Abfragen bereitstellen")]
    [Description("Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)")]
    public WebMapping.Core.ServiceDynamicQueries DynamicQueries { get; set; }

    [DisplayName("Service-Typ")]
    [Description("Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.")]
    public WebMapping.Core.ImageServiceType ServiceType { get; set; }

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



    [DisplayName("Username")]
    [Category("Anmeldungs-Credentials")]
    public string Username
    {
        get { return _user; }
        set { _user = value; }
    }

    [DisplayName("Password")]
    [Category("Anmeldungs-Credentials")]
    [PasswordPropertyText(true)]
    public string Password
    {
        get { return _pwd; }
        set { _pwd = value; }
    }

    [DisplayName("Token")]
    [Category("Anmeldungs-Token")]
    [PasswordPropertyText(true)]
    [Editor(typeof(TypeEditor.TokenAuthentificationEditor), typeof(TypeEditor.ITypeEditor))]
    public string Token { get; set; }

    [Category("Image Server Properties")]
    public ArcIS_ImageFormat ImageFormat
    {
        get { return _imageFormat; }
        set { _imageFormat = value; }
    }

    [Category("Image Server Properties")]
    public ArcIS_PixelType PixelType
    {
        get { return _pixelType; }
        set { _pixelType = value; }
    }

    [Category("Image Server Properties")]
    public string NoData
    {
        get { return _nodata; }
        set { _nodata = value; }
    }

    [Category("Image Server Properties")]
    public ArcIS_NoDataInterpretation NoDataInterpretation
    {
        get { return _nodataInterpretation; }
        set { _nodataInterpretation = value; }
    }

    [Category("Image Server Properties")]
    public ArcIS_Interpolation Interpolation
    {
        get { return _interpolation; }
        set { _interpolation = value; }
    }

    [Category("Image Server Properties")]
    public string CompressionQuality
    {
        get { return _compressQaulity; }
        set { _compressQaulity = value; }
    }

    [Category("Image Server Properties")]
    public string BandIDs
    {
        get { return _bandIDs; }
        set { _bandIDs = value; }
    }

    [Category("Image Server Properties")]
    public string MosaicRule
    {
        get { return _mosaicRule; }
        set { _mosaicRule = value; }
    }

    [Category("Image Server Properties")]
    [DisplayName("RenderingRule (ExportImage/Legend)")]
    [Description("Diese RenderingRule wird für die Darstellung des Dienstes und der Legende verwendet")]
    public string RenderingRule
    {
        get;
        set;
    }

    [Category("Image Server Properties")]
    [DisplayName("RenderingRule (Identify)")]
    [Description("Diese RenderingRule wird beim Identify verwendet. Gibt es für diese RenderingRule auch einen RasterAttributeTable, wird dieser zum bestimmen des angezeigten Wertes verwendet.")]
    public string RenderingRuleIdentify
    {
        get;
        set;
    }

    [Category("Image Server Identify")]
    [DisplayName("Pixel Aliasname")]
    [Description("Wird ein Identify auf den Dienst ausgeführt wird beim Ergebnis statt 'Pixel' dieser Wert in der Ergbnisstabelle angezeigt. Dies sollte ein Name sein, der das Ergebnis genauer beschreibt, zB Höhe [m]")]
    public string PixelAliasname
    {
        get; set;
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

        _imageFormat = (ArcIS_ImageFormat)stream.Load("imageformat", (int)ArcIS_ImageFormat.jpgpng);
        _pixelType = (ArcIS_PixelType)stream.Load("pixeltype", (int)ArcIS_PixelType.UNKNOWN);
        _nodata = (string)stream.Load("nodata", String.Empty);
        _nodataInterpretation = (ArcIS_NoDataInterpretation)stream.Load("nodatainterpretation", (int)ArcIS_NoDataInterpretation.esriNoDataMatchAny);
        _interpolation = (ArcIS_Interpolation)stream.Load("interpretation", (int)ArcIS_Interpolation.RSP_BilinearInterpolation);
        _compressQaulity = (string)stream.Load("compressqualitity", String.Empty);
        _bandIDs = (string)stream.Load("bandids", String.Empty);
        _mosaicRule = (string)stream.Load("mosaicrule", String.Empty);

        this.RenderingRule = (string)stream.Load("renderingrule", String.Empty);
        this.RenderingRuleIdentify = (string)stream.Load("renderingrule_identify", String.Empty);

        this.PixelAliasname = (string)stream.Load("pixel_aliasname", String.Empty);

        this.ServiceType = (WebMapping.Core.ImageServiceType)stream.Load("service_type", (int)WebMapping.Core.ImageServiceType.Normal);

        this.DynamicPresentations = (WebMapping.Core.ServiceDynamicPresentations)stream.Load("dynamic_presentations", (int)WebMapping.Core.ServiceDynamicPresentations.Manually);
        this.DynamicQueries = (WebMapping.Core.ServiceDynamicQueries)stream.Load("dynamic_queries", (int)WebMapping.Core.ServiceDynamicQueries.Manually);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("server", _server);
        stream.Save("service", _service);
        stream.Save("serviceurl", _serviceUrl);
        stream.Save("guid", _guid.ToString());
        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "agsservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "agsservice"));

        stream.Save("imageformat", (int)_imageFormat);
        stream.Save("pixeltype", (int)_pixelType);
        stream.Save("nodata", _nodata);
        stream.Save("nodatainterpretation", (int)_nodataInterpretation);
        stream.Save("interpretation", (int)_interpolation);
        stream.Save("compressqualitity", _compressQaulity);
        stream.Save("bandids", _bandIDs);
        stream.Save("mosaicrule", _mosaicRule);

        stream.Save("renderingrule", this.RenderingRule ?? String.Empty);
        stream.Save("renderingrule_identify", this.RenderingRuleIdentify ?? String.Empty);

        stream.Save("pixel_aliasname", this.PixelAliasname ?? String.Empty);

        stream.Save("service_type", (int)this.ServiceType);

        stream.Save("dynamic_presentations", (int)this.DynamicPresentations);
        stream.Save("dynamic_queries", (int)this.DynamicQueries);
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
    public Task<bool> CreatedAsync(string FullName)
    {
        BuildServiceInfo(FullName);
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region Helper

    public void BuildServiceInfo(string fullName)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + "/themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        TOC toc = new TOC();
        toc.Name = "default";
        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        toc.Save(xmlStream);
        var fi = DocumentFactory.DocumentInfo(di.FullName + "/tocs/" + toc.CreateAs(true) + ".xml");
        xmlStream.SaveDocument(fi.FullName);
        var di_tocs_default = fi.Directory;

        ServiceLayer layer = new ServiceLayer();
        layer.Name = this.Service.Replace("/", "-");

        layer.Url = Crypto.GetID();
        layer.Id = "0";

        layer.Visible = true;

        xmlStream = DocumentFactory.New(di.FullName);
        layer.Save(xmlStream);
        xmlStream.SaveDocument(di.FullName + "/themes/" + layer.CreateAs(true) + ".xml");

        string themeLinkUri = ThemeExists(fullName, layer);

        TocTheme tocTheme = new TocTheme();
        tocTheme.LinkUri = themeLinkUri;
        tocTheme.AliasName = layer.Name;
        tocTheme.Visible = layer.Visible;

        string tocThemeConfig = di_tocs_default.FullName + "l" + GuidEncoder.Encode(Guid.NewGuid()).ToString().ToLower() + ".link";
        xmlStream = DocumentFactory.New(di.FullName);
        tocTheme.Save(xmlStream);
        xmlStream.SaveDocument(tocThemeConfig);

        ItemOrder itemOrder = new ItemOrder(di.FullName + "/themes");
        itemOrder.Save();
    }
    private string ThemeExists(string fullName, ServiceLayer layer)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + "/themes");
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
                return "services/arcgisserver/imageserverservice/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
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
}
