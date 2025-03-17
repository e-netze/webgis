using E.Standard.ArcXml;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using E.Standard.WebGIS.CmsSchema.Legacy;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class IMSService : CopyableNode, IAuthentification, ICreatable, IEditable, IUI, IDisplayName, IRefreshable
{
    private string _server, _service, _user, _pwd;
    private string _guid;
    //private CommaFormat _commaFormat = CommaFormat.Default;
    private IMSLocale _locale = new IMSLocale();

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public IMSService(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        this.DynamicPresentations = WebMapping.Core.ServiceDynamicPresentations.Auto;
        this.DynamicQueries = WebMapping.Core.ServiceDynamicQueries.Auto;
        this.DynamicDehavior = WebMapping.Core.DynamicDehavior.UseStrict;
    }

    #region Properties
    [DisplayName("Karten Server")]
    public string Server
    {
        get { return _server; }
        set { _server = value; }
    }
    [DisplayName("Karten Dienst")]
    public string Service
    {
        get { return _service; }
        set { _service = value; }
    }

    [DisplayName("Dynamische Darstellungsvariaten")]
    [Description("Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.")]
    public WebMapping.Core.ServiceDynamicPresentations DynamicPresentations { get; set; }

    [DisplayName("Dynamische Abfragen")]
    [Description("Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)")]
    public WebMapping.Core.ServiceDynamicQueries DynamicQueries { get; set; }

    [DisplayName("Dynamisches Verhalten")]
    [Description("Gibt an, wie mit Layern umgegangen wird, die nicht beim erstellen oder nach einem Refresh im CMS unter Themen gelistet werden. AutoAppendNewLayers ... neue Themen werden beim Initialisieren des Dienstes (nach einem cache/clear) der Karte hinzugefügt und können über den TOC geschalten werden. UseStrict ... nur jene Themen, die unter Themen aufgelistet sind, kommen auch in der Karte vor.")]
    public WebMapping.Core.DynamicDehavior DynamicDehavior { get; set; }

    [DisplayName("Service-Typ")]
    [Description("Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.")]
    public WebMapping.Core.ImageServiceType ServiceType { get; set; }

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

    //[Browsable(true)]
    //[Category("Localisierung")]
    //[DisplayName("Komma-Format")]
    //[Description("Hier kann gegeben werden, wie ein Komma für Dienste interpretiert werden soll.\nDefault=Wert wird aus dem LOCALE Tag von GET_SERVICE_INFO übernommen\nForceComma=Beistrich als Komma\nForcePoint=Punkt als Komma")] 
    //public CommaFormat CommaFormat
    //{
    //    get { return _commaFormat; }
    //    set { _commaFormat = value; }
    //}
    [Browsable(true)]
    [Category("~Lokalisierung")]
    [DisplayName("IMS Service LOCALE überschreiben")]
    [Description("Hier kann gegeben werden, wie ein Komma für Dienste interpretiert werden soll.\nKein Wert ... Lokalisierung wird aus dem LOCALE Tag von GET_SERVICE_INFO übernommen\nde-AT ... Beistrich als Komma, en-US ... Punkt als Komma")]
    [Editor(typeof(TypeEditor.IMSLocaleEditor), typeof(TypeEditor.ITypeEditor))]
    public IMSLocale OverrideLocal
    {
        get { return _locale; }
        set { _locale = value; }
    }
    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _server = (string)stream.Load("server", String.Empty);
        _service = (string)stream.Load("service", String.Empty);
        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());
        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "imsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "imsservice").Replace(stream.StringReplace);

        _locale.FromString((string)stream.Load("overridelocale", String.Empty));
        //_commaFormat = (CommaFormat)stream.Load("commaformat", (int)CommaFormat.Default);

        this.DynamicPresentations = (WebMapping.Core.ServiceDynamicPresentations)stream.Load("dynamic_presentations", (int)WebMapping.Core.ServiceDynamicPresentations.Manually);
        this.DynamicQueries = (WebMapping.Core.ServiceDynamicQueries)stream.Load("dynamic_queries", (int)WebMapping.Core.ServiceDynamicQueries.Manually);
        this.ServiceType = (WebMapping.Core.ImageServiceType)stream.Load("service_type", (int)WebMapping.Core.ImageServiceType.Normal);
        this.DynamicDehavior = (WebMapping.Core.DynamicDehavior)stream.Load("dynamic_behavior", (int)WebMapping.Core.DynamicDehavior.AutoAppendNewLayers);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("server", _server);
        stream.Save("service", _service);
        stream.Save("guid", _guid.ToString());
        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "imsservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "imsservice"));

        stream.Save("overridelocale", _locale.ToString());
        //stream.Save("commaformat", (int)_commaFormat);

        stream.Save("dynamic_presentations", (int)this.DynamicPresentations);
        stream.Save("dynamic_queries", (int)this.DynamicQueries);
        stream.Save("service_type", (int)this.ServiceType);
        stream.Save("dynamic_behavior", (int)this.DynamicDehavior);
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
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
        }
    }
    async public Task<bool> CreatedAsync(string FullName)
    {
        //DirectoryInfo di = (new FileInfo(FullName)).Directory;

        //DirectoryInfo di_themes = new DirectoryInfo(di.FullName + @"\themes");
        //if (di_themes.Exists == false)
        //    di_themes.Create();

        //IMSServerInfo si = new IMSServerInfo(Server, 5300, Encoding.Default, true);
        //si.GetLayerInfo(Service, true);

        //List<string> urls = new List<string>();
        //for (int i = 0; i < si.layerCount; i++)
        //{
        //    string layerName=String.Empty, layerID=String.Empty;
        //    bool layerVisible=true;

        //    if (!si.getLayer(i, ref layerName, ref layerID, ref layerVisible))
        //        continue;

        //    ServiceLayer layer = new ServiceLayer();
        //    layer.Name = layerName;
        //    layer.Url = Crypto.GetID();
        //    layer.Id = layerID;
        //    layer.Visible = layerVisible;

        //    urls.Add(layer.Url);
        //    XmlStreamDocument xmlStream = new XmlStreamDocument();
        //    layer.Save(xmlStream);
        //    xmlStream.SaveDocument(di.FullName + @"\themes\" + layer.CreateAs(true) + ".xml");
        //}
        //ItemOrder itemOrder = new ItemOrder(di.FullName + @"\themes");
        //itemOrder.Items = urls.ToArray();
        //itemOrder.Save();

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
    async public Task BuildServiceInfoAsync(string fullName, bool refresh, int level = 0)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"\themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        var connectionProperties = new ArcAxlConnectionProperties()
        {
            AuthUsername = _user,
            AuthPassword = _pwd,
            Token = this.Token,
            CheckUmlaut = true,
            Timeout = 25
        };

        IMSServerInfo serviceInfo = new IMSServerInfo(_servicePack.HttpService,
                                             connectionProperties,
                                             Server, Encoding.Default);

        if (!await serviceInfo.GetLayerInfoAsync(Service))
        {
            throw new Exception(serviceInfo.ErrorMessage);
        }
        if (serviceInfo.LayerCount == 0)
        {
            throw new Exception("Es konnten keine Layer ausgelesen werden");
        }

        List<string> urls = new List<string>();
        List<string> themeLinkUris = new List<string>();

        #region Überprüfen, ob beim Refresh layer gelöscht/hinzugefügt werden werden
        if (refresh && level == 0)
        {
            int oldT = this.CmsManager.CountConfigFiles(di.FullName + @"\themes", "*.xml");
            int newT = serviceInfo.LayerCount;
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

        for (int i = 0; i < serviceInfo.LayerCount; i++)
        {
            string layerName = String.Empty, layerID = String.Empty;
            bool layerVisible = true;

            if (!serviceInfo.GetLayer(i, ref layerName, ref layerID, ref layerVisible))
            {
                continue;
            }

            ServiceLayer layer = new ServiceLayer();
            layer.Name = layerName;
            layer.Url = Crypto.GetID();
            layer.Id = layerID;
            layer.Visible = layerVisible;

            string themeLinkUri = ThemeExists(fullName, layer);
            bool save = true;
            if (refresh && !String.IsNullOrEmpty(themeLinkUri))
            {
                save = false;

                #region Überprüfen, ob LayerID noch zusammenpasst!
                string eFilename = di.FullName + @"\themes\" + IMSService.ExtractLastPathElement(themeLinkUri) + ".xml";
                IStreamDocument eXmlSteam = DocumentFactory.Open(eFilename);
                ServiceLayer eLayer = new ServiceLayer();
                eLayer.Load(eXmlSteam);
                if (eLayer.Id != layer.Id)
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
                string layerFullName = di.FullName + @"\themes\" + layer.CreateAs(true) + ".xml";
                xmlStream.SaveDocument(layerFullName);

                themeLinkUri = ThemeExists(fullName, layer);
                themeLinkUris.Add(themeLinkUri);
            }

            if (!TocElementExists(di_tocs_default.FullName, themeLinkUri))
            {
                TocTheme tocTheme = new TocTheme();
                tocTheme.LinkUri = themeLinkUri;
                tocTheme.AliasName = layer.Name;
                tocTheme.Visible = layer.Visible;

                string tocThemeConfig = di_tocs_default.FullName + @"\l" + GuidEncoder.Encode(Guid.NewGuid()).ToLower() + ".link";
                xmlStream = DocumentFactory.New(di.FullName);
                tocTheme.Save(xmlStream);
                xmlStream.SaveDocument(tocThemeConfig);
            }
        }

        if (this.CmsManager != null)
        {
            this.CmsManager.ThinPath(di.FullName + @"\themes", themeLinkUris, "*.xml");
        }

        ItemOrder itemOrder = new ItemOrder(di.FullName + @"\themes");
        itemOrder.Items = urls.ToArray();
        itemOrder.Save();
    }

    private string ThemeExists(string fullName, ServiceLayer layer)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + @"\themes");
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
                return "services/ims/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }

    static internal string ExtractLastPathElement(string path)
    {
        int pos = path.Replace("\\", "/").LastIndexOf("/");
        if (pos == -1)
        {
            return path;
        }

        return path.Substring(pos + 1, path.Length - pos - 1);
    }
    static internal bool TocElementExists(string fullName, string layerFullName)
    {
        var di = DocumentFactory.PathInfo(fullName);
        if (!di.Exists)
        {
            return false;
        }

        // Früher waren die TOC Layer xml-Files
        foreach (var fi in di.GetFiles("*.xml"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            TocTheme theme = new TocTheme();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            theme.Load(xmlStream);

            if (theme.LinkUri.ToLower() == layerFullName.ToLower())
            {
                return true;
            }
        }
        // Jetzt sind es Link Files
        foreach (var fi in di.GetFiles("*.link"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            TocTheme theme = new TocTheme();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            theme.Load(xmlStream);

            if (theme.LinkUri.ToLower() == layerFullName.ToLower())
            {
                return true;
            }
        }
        foreach (var sub in di.GetDirectories())
        {
            if (TocElementExists(sub.FullName, layerFullName))
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "IMS Dienst"; }
    }

    protected override void BeforeCopy()
    {
        base.BeforeCopy();

        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }
}

public class IMSLocale
{
    private string _country = String.Empty;
    private string _language = String.Empty;

    public string Country
    {
        get { return _country; }
        set { _country = value; }
    }
    public string Language
    {
        get { return _language; }
        set { _language = value; }
    }

    public void FromString(string locale)
    {
        if (locale.Contains("-"))
        {
            _language = locale.Split('-')[0];
            _country = locale.Split('-')[1];
        }
        else
        {
            _language = _country = String.Empty;
        }
    }
    public override string ToString()
    {
        if (String.IsNullOrEmpty(_country) ||
            String.IsNullOrEmpty(_language))
        {
            return String.Empty;
        }

        return _language + "-" + _country;
    }
}
