using E.Standard.CMS.Core;
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

public class WMTSService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName
{
    private string _server = String.Empty, _tileLayer = String.Empty, _tileMatrixSet = String.Empty, _imageFormat = String.Empty, _tileStyle = String.Empty;
    private string _user, _pwd;
    private string _guid;
    private TileGridRendering _rendering = TileGridRendering.Quality;
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public WMTSService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
        this.MaxLevel = -1;
    }

    [Category("#category_server")]
    [DisplayName("#server")]
    public string Server
    {
        get { return _server; }
        set { _server = value; }
    }

    [Category("#category_tile_layer")]
    [DisplayName("#tile_layer")]
    public string TileLayer
    {
        get { return _tileLayer; }
        set { _tileLayer = value; }
    }

    [Category("#category_tile_matrix_set")]
    [DisplayName("#tile_matrix_set")]
    public string TileMatrixSet
    {
        get { return _tileMatrixSet; }
        set { _tileMatrixSet = value; }
    }

    [Category("#category_image_format")]
    [DisplayName("#image_format")]
    public string ImageFormat
    {
        get { return _imageFormat; }
        set { _imageFormat = value; }
    }

    [Category("#category_tile_style")]
    [DisplayName("#tile_style")]
    public string TileStyle
    {
        get { return _tileStyle; }
        set { _tileStyle = value; }
    }

    [Category("#category_ticket_server")]
    [DisplayName("#ticket_server")]
    public string[] ResourceURLs { get; set; }

    [Category("#category_ticket_server")]
    [DisplayName("#ticket_server")]
    public string TicketServer
    {
        get;
        set;
    }

    [DisplayName("#rendering")]
    public TileGridRendering Rendering
    {
        get { return _rendering; }
        set { _rendering = value; }
    }

    [DisplayName("#max_level")]
    public int MaxLevel { get; set; }

    [DisplayName("#hide_beyond_max_level")]
    public bool HideBeyondMaxLevel { get; set; }


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

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _guid = (string)stream.Load("guid", String.Empty);
        //if(String.IsNullOrEmpty(_guid))
        //    _guid = Guid.NewGuid().ToString("N").ToLower();
        _server = (string)stream.Load("server", String.Empty);
        _tileMatrixSet = (string)stream.Load("tilematrixset", String.Empty);
        _imageFormat = (string)stream.Load("imageformat", String.Empty);
        _tileLayer = (string)stream.Load("layer", String.Empty);
        _tileStyle = (string)stream.Load("style", String.Empty);

        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "wmtsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "wmtsservice").Replace(stream.StringReplace);
        TicketServer = (string)stream.Load("TicketServer", String.Empty);

        _rendering = (TileGridRendering)stream.Load("rendering", (int)TileGridRendering.Quality);

        MaxLevel = (int)stream.Load("maxlevel", -1);
        HideBeyondMaxLevel = (bool)stream.Load("hide_beyond_maxlevel", false);

        string resourceUrls = (string)stream.Load("resourceurls", String.Empty);
        if (!String.IsNullOrWhiteSpace(resourceUrls))
        {
            this.ResourceURLs = resourceUrls.Split(';');
        }
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("guid", _guid);
        stream.Save("server", _server);
        stream.Save("tilematrixset", _tileMatrixSet);
        stream.Save("imageformat", _imageFormat);
        stream.Save("layer", _tileLayer);
        stream.Save("style", _tileStyle);

        stream.Save("maxlevel", this.MaxLevel);
        stream.Save("hide_beyond_maxlevel", this.HideBeyondMaxLevel);

        if (!String.IsNullOrWhiteSpace(_user))
        {
            stream.Save("user", _user);
        }

        if (!String.IsNullOrWhiteSpace(_pwd))
        {
            stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "wmtsservice"));
        }

        if (!String.IsNullOrWhiteSpace(this.Token))
        {
            stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "wmtsservice"));
        }

        if (!String.IsNullOrWhiteSpace(TicketServer))
        {
            stream.Save("TicketServer", TicketServer.Trim());
        }

        stream.Save("rendering", (int)_rendering);

        if (this.ResourceURLs != null)
        {
            stream.Save("resourceurls", String.Join(";", this.ResourceURLs));
        }
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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new WMTSServiceControl(_servicePack, this.isInCopyMode);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region Helper

    public void BuildServiceInfo(string fullName)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"/themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        TOC toc = new TOC();
        toc.Name = "default";
        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        toc.Save(xmlStream);
        var fi = DocumentFactory.DocumentInfo(di.FullName + @"/tocs/" + toc.CreateAs(true) + ".xml");
        xmlStream.SaveDocument(fi.FullName);
        var di_tocs_default = fi.Directory;

        ServiceLayer layer = new ServiceLayer();
        layer.Name = _tileMatrixSet;

        layer.Url = Crypto.GetID();
        layer.Id = "0";

        layer.Visible = true;

        xmlStream = DocumentFactory.New(di.FullName);
        layer.Save(xmlStream);
        xmlStream.SaveDocument(di.FullName + @"/themes/" + layer.CreateAs(true) + ".xml");

        string themeLinkUri = ThemeExists(fullName, layer);

        TocTheme tocTheme = new TocTheme();
        tocTheme.LinkUri = themeLinkUri;
        tocTheme.AliasName = layer.Name;
        tocTheme.Visible = layer.Visible;

        string tocThemeConfig = di_tocs_default.FullName + @"/l" + GuidEncoder.Encode(Guid.NewGuid()).ToString().ToLower() + ".link";
        xmlStream = DocumentFactory.New(di.FullName);
        tocTheme.Save(xmlStream);
        xmlStream.SaveDocument(tocThemeConfig);

        ItemOrder itemOrder = new ItemOrder(di.FullName + @"/themes");
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
                return "services/ogc/wmts/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "WMTS Dienst"; }
    }
}
