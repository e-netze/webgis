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
public class ArcServerWmtsService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName
{
    private string _guid;
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ArcServerWmtsService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
        this.MaxLevel = -1;
    }

    #region Properties

    [Category("WMTS")]
    [DisplayName("Dienst Server/Url")]
    public string Server { get; set; }

    [Category("WMTS")]
    [DisplayName("Tile-Layer")]
    public string TileLayer { get; set; }

    [Category("WMTS")]
    [DisplayName("Tile-Matrix-Set")]
    public string TileMatrixSet { get; set; }

    [Category("WMTS")]
    [DisplayName("Image Format")]
    public string ImageFormat { get; set; }

    [Category("WMTS")]
    [DisplayName("Tile-Style")]
    public string TileStyle { get; set; }

    [Category("WMTS")]
    [DisplayName("ResourceURLs")]
    public string[] ResourceURLs { get; set; }

    [DisplayName("Rendering")]
    [Description("Für Luftbilder 'Quality' verwenden. Für Ortspläne (mit Text) 'Readablility'...")]
    public TileGridRendering Rendering { get; set; }

    [DisplayName("Max. Level")]
    [Description("Der höchste Level, der für diesen Dienst verwendet werden kann. Ein Wert kleiner als 0, gibt an, dass das maximale Level dem maximalen Matrixset Level aus den Capabilities entspricht.")]
    public int MaxLevel { get; set; }

    [DisplayName("Unter Max. Level verbergen")]
    [Description("Zoomt der Anwender weiter in die Karte, als dieser Tiling Dienst zur Verfügung steht, werden die Tiles nicht mehr angezeigt. Per Default (Wert = false) wird der Dienst trotzdem angezeigt und die Tiles entsprechend \"vergrößert/unscharf\" dargestellt.")]
    public bool HideBeyondMaxLevel { get; set; }

    #endregion

    #region IAuthentification Member

    [DisplayName("Username")]
    [Category("Anmeldungs-Credentials")]
    public string Username { get; set; }

    [DisplayName("Password")]
    [Category("Anmeldungs-Credentials")]
    [PasswordPropertyText(true)]
    public string Password { get; set; }

    [Browsable(false)]
    [DisplayName("Token")]
    [Category("Anmeldungs-Credentials")]
    [PasswordPropertyText(true)]
    [Editor(typeof(TypeEditor.TokenAuthentificationEditor), typeof(TypeEditor.ITypeEditor))]
    public string Token { get; set; }

    [Category("~Anmeldungs-Credentials")]
    [DisplayName("Token-Gültigkeit [min]")]
    public int TokenExpiration { get; set; } = 60;

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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new ArcServerWmtsServiceControl(_servicePack, this.isInCopyMode);
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

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());

        this.Server = (string)stream.Load("server", String.Empty);
        this.TileMatrixSet = (string)stream.Load("tilematrixset", String.Empty);
        this.ImageFormat = (string)stream.Load("imageformat", String.Empty);
        this.TileLayer = (string)stream.Load("layer", String.Empty);
        this.TileStyle = (string)stream.Load("style", String.Empty);
        this.Rendering = (TileGridRendering)stream.Load("rendering", (int)TileGridRendering.Quality);
        this.MaxLevel = (int)stream.Load("maxlevel", -1);
        this.HideBeyondMaxLevel = (bool)stream.Load("hide_beyond_maxlevel", false);
        this.TokenExpiration = (int)stream.Load("token_expiration", 60);

        string resourceUrls = (string)stream.Load("resourceurls", String.Empty);
        if (!String.IsNullOrWhiteSpace(resourceUrls))
        {
            this.ResourceURLs = resourceUrls.Split(';');
        }

        this.Username = (string)stream.Load("user", String.Empty);
        this.Password = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "agsservice").Replace(stream.StringReplace);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("guid", _guid.ToString());

        stream.Save("server", this.Server);
        stream.Save("tilematrixset", this.TileMatrixSet);
        stream.Save("imageformat", this.ImageFormat);
        stream.Save("layer", this.TileLayer);
        stream.Save("style", this.TileStyle);
        stream.Save("rendering", (int)this.Rendering);

        stream.Save("maxlevel", this.MaxLevel);
        stream.Save("hide_beyond_maxlevel", this.HideBeyondMaxLevel);

        stream.Save("token_expiration", this.TokenExpiration);

        if (this.ResourceURLs != null)
        {
            stream.Save("resourceurls", String.Join(";", this.ResourceURLs));
        }

        stream.SaveOrRemoveIfEmpty("user", this.Username);
        stream.SaveOrRemoveIfEmpty("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Password), "agsservice"));
    }

    #endregion

    #region Overrides

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "ArcGIS Server WMTS Service"; }
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
        layer.Name = TileMatrixSet;

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
                return $"services/arcgisserver/wmtsservice/{this.Url}/themes/{l.Url}";
            }
        }

        return String.Empty;
    }

    #endregion
}
