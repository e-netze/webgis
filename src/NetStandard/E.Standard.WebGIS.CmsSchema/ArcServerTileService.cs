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

public class ArcServerTileService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName
{
    private string _server, _service, _serviceConfigUrl = String.Empty, _tileUrl = String.Empty, _user = String.Empty, _pwd = String.Empty;
    private string _mapname = "Layers", _layer = "_alllayer";
    private string _guid;
    private TileGridRendering _rendering = TileGridRendering.Quality;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ArcServerTileService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }

    #region Properties
    [DisplayName("#server")]
    public string Server
    {
        get { return _server; }
        set { _server = value; }
    }
    [DisplayName("#service")]
    public string Service
    {
        get { return _service; }
        set { _service = value; }
    }
    [DisplayName("#service_config_url")]
    public string ServiceConfigUrl
    {
        get { return _serviceConfigUrl; }
        set { _serviceConfigUrl = value; }
    }
    [DisplayName("#tile_url")]
    public string TileUrl
    {
        get { return _tileUrl; }
        set { _tileUrl = value; }
    }
    [DisplayName("#map_name")]
    public string MapName
    {
        get { return _mapname; }
        set { _mapname = value; }
    }
    [DisplayName("#layer_name")]
    public string LayerName
    {
        get { return _layer; }
        set { _layer = value; }
    }

    [DisplayName("#rendering")]
    public TileGridRendering Rendering
    {
        get { return _rendering; }
        set { _rendering = value; }
    }

    //[DisplayName("Max. Level")]
    //[Description("Der höchste Level, der für diesen Dienst verwendet werden kann. Ein Wert kleiner als 0, gibt an, dass das maximale Level dem maximalen Matrixset Level aus den Capabilities entspricht.")]
    //public int MaxLevel { get; set; }

    [DisplayName("#hide_beyond_max_level")]
    public bool HideBeyondMaxLevel { get; set; }

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

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _server = (string)stream.Load("server", String.Empty);
        _service = (string)stream.Load("service", String.Empty);
        _serviceConfigUrl = (string)stream.Load("serviceconfigurl", String.Empty);
        _tileUrl = (string)stream.Load("tileurl", String.Empty);
        _mapname = (string)stream.Load("mapname", String.Empty);
        _layer = (string)stream.Load("layer", String.Empty);

        _rendering = (TileGridRendering)stream.Load("rendering", (int)TileGridRendering.Quality);

        //MaxLevel = (int)stream.Load("maxlevel", (int)-1);
        HideBeyondMaxLevel = (bool)stream.Load("hide_beyond_maxlevel", false);

        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());

        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "agsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "agsservice").Replace(stream.StringReplace);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("server", _server);
        stream.Save("service", _service);
        stream.Save("serviceconfigurl", _serviceConfigUrl);
        stream.Save("tileurl", _tileUrl);
        stream.Save("mapname", _mapname);
        stream.Save("layer", _layer);

        stream.Save("rendering", (int)_rendering);

        //stream.Save("maxlevel", this.MaxLevel);
        stream.Save("hide_beyond_maxlevel", this.HideBeyondMaxLevel);

        stream.Save("guid", _guid.ToString());

        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "agsservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "agsservice"));
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new ArcServerTilingServiceControl(_servicePack, this.isInCopyMode);
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
        layer.Name = "_alllayers";

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

        string tocThemeConfig = di_tocs_default.FullName + "/l" + GuidEncoder.Encode(Guid.NewGuid()).ToString().ToLower() + ".link";
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
                return "services/arcgisserver/tileservice/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }
    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "ArcGIS Server Tiling Dienst"; }
    }

    #region IAuthentification Member

    [Browsable(false)]
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
}
