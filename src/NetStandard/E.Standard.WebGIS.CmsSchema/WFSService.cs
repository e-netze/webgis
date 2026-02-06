using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wfs;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class WFSService : CopyableNode, IAuthentification, IClientCertification, ICreatable, IEditable, IUI, IDisplayName, IRefreshable
{
    private string _user, _pwd, _server;
    private WFS_Version _version = WFS_Version.version_1_0_0;
    private string _guid;
    private string _certificate = String.Empty, _certificatePwd = String.Empty;
    private string _onlineResource = String.Empty;
    private bool _interpretSrsAxis = true;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public WFSService(CmsItemTransistantInjectionServicePack servicePack)
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
    [DisplayName("#version")]
    public WFS_Version Version
    {
        get { return _version; }
        set { _version = value; }
    }


    [DisplayName("#interpert_srs_axis")]
    public bool InterpertSrsAxis
    {
        get { return _interpretSrsAxis; }
        set { _interpretSrsAxis = value; }
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

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _guid = (string)stream.Load("guid", String.Empty);
        //if(String.IsNullOrEmpty(_guid))
        //    _guid = Guid.NewGuid().ToString("N").ToLower();
        _server = (string)stream.Load("server", String.Empty);
        _version = (WFS_Version)stream.Load("version", (int)WFS_Version.version_1_0_0);

        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "wmsservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "wmsservice").Replace(stream.StringReplace);
        _certificate = (string)stream.Load("cert", String.Empty);
        _certificatePwd = CmsCryptoHelper.Decrypt((string)stream.Load("certpwd", String.Empty), "WmsServiceCertificatePassword").Replace(stream.StringReplace);

        _interpretSrsAxis = (bool)stream.Load("interpretsrsaxis", true);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("guid", _guid);
        stream.Save("server", _server);
        stream.Save("version", (int)_version);

        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "wmsservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "wmsservice"));
        if (!String.IsNullOrEmpty(_certificate))
        {
            stream.Save("cert", _certificate);
            stream.Save("certpwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_certificatePwd), "WmsServiceCertificatePassword"));
        }

        stream.Save("interpretsrsaxis", _interpretSrsAxis);
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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new WFSServiceControl(_servicePack, this.isInCopyMode);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region Helper
    async public Task BuildServiceInfoAsync(string fullName, bool refresh, int level = 0)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"/themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        CapabilitiesHelper capsHelper = null;
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
            if (_version == WFS_Version.version_1_0_0)
            {
                Serializer<OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType> ser = new Serializer<OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType>();
                url = WMSService.AppendToUrl(url, "VERSION=1.0.0&SERVICE=WFS&REQUEST=GetCapabilities");
                OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType caps = await ser.FromUrlAsync(
                    url,
                    _servicePack.HttpService,
                    requestAuthorization);
                capsHelper = new CapabilitiesHelper(caps);
            }
            else if (_version == WFS_Version.version_1_1_0)
            {
                Serializer<OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType> ser = new Serializer<OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType>();
                url = WMSService.AppendToUrl(url, "VERSION=1.1.0&SERVICE=WFS&REQUEST=GetCapabilities");
                OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType caps = await ser.FromUrlAsync(
                    url,
                    _servicePack.HttpService,
                    requestAuthorization);
                capsHelper = new CapabilitiesHelper(caps);
            }
        }
        catch (System.Exception)
        {
            throw;
        }
        if (capsHelper.FeatureTypeList.Length == 0)
        {
            throw new Exception("Es konnten keine Layer ausgelesen werden.\nMöchten Sie trotzden fortfahren und alle (TOC)Themen dieses Dienstes aus dem CMS löschen?");
        }

        _onlineResource = capsHelper.OnlineResource;

        #region Überprüfen, ob beim Refresh layer gelöscht/hinzugefügt werden werden
        if (refresh && level == 0)
        {
            int oldT = this.CmsManager.CountConfigFiles(di.FullName + @"/themes", "*.xml");
            int newT = capsHelper.FeatureTypeList.Length;
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

        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        List<string> urls = new List<string>();
        List<string> themeLinkUris = new List<string>();

        foreach (CapabilitiesHelper.FeatureType featureType in capsHelper.FeatureTypeList)
        {
            ServiceLayer layer = new ServiceLayer();
            layer.Id = featureType.Name;
            layer.Name = featureType.Title;
            layer.Url = Crypto.GetID();

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
                string layerFullName = di.FullName + @"/themes/" + layer.CreateAs(true) + ".xml";
                xmlStream.SaveDocument(layerFullName);

                themeLinkUri = ThemeExists(fullName, layer);
                themeLinkUris.Add(themeLinkUri);
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
                return "services/ogc/wfs/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }
    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "WFS Dienst"; }
    }

    protected override void BeforeCopy()
    {
        base.BeforeCopy();

        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }
}
