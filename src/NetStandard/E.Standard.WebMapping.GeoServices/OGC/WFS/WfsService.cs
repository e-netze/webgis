using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wfs;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.OGC.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.OGC.WFS;

public class WfsService : IMapService, IMapServiceSupportedCrs, IMapServiceMetadataInfo
{
    internal IMap _map;
    internal WFS_Version _version = WFS_Version.version_1_0_0;
    internal GML.GmlVersion _gmlVersion = GML.GmlVersion.v1;

    internal string _GF_HttpGet, _GF_HttpPost;

    private string _name = String.Empty;
    private string _id = String.Empty;
    private string _server = String.Empty;
    private string _url = String.Empty;
    private string _authUser = String.Empty, _authPassword = String.Empty;
    private int _timeout = 20, _featureCount = 30;
    private System.Security.Cryptography.X509Certificates.X509Certificate _x509certificate = null;
    private readonly LayerCollection _layers;
    private bool _interpretSrsAxis = true;

    public WfsService(WFS_Version version)
    {
        _version = version;
        _layers = new LayerCollection(this);
        ShowInToc = true;
    }

    #region IService Member

    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }

    public string Url
    {
        get
        {
            return _url;
        }
        set
        {
            _url = value;
        }
    }

    public string Server
    {
        get { return _server; }
    }

    public string Service
    {
        get; private set;
    }

    public string ServiceShortname { get { return this.Service; } }

    public string ID
    {
        get { return _id; }
    }

    public float InitialOpacity
    {
        get
        {
            return 0.0f;
        }
        set
        {

        }
    }
    public float OpacityFactor { get; set; } = 1f;

    public bool ShowInToc { get; set; }

    public bool CanBuffer
    {
        get { return false; }
    }

    public bool UseToc
    {
        get
        {
            return false;
        }
        set
        {

        }
    }

    public LayerCollection Layers
    {
        get { return _layers; }
    }

    public Envelope InitialExtent
    {
        get { return null; }
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.VectorService; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        _id = serviceID;
        _server = server;
        this.Service = String.IsNullOrWhiteSpace(url) ? "WMS" : url;

        _authUser = authUser;
        _authPassword = authPwd;

        return true;
    }

    async public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        _map = map;
        CapabilitiesHelper capsHelper = null;

        GML.GeometryTranslator.Init();

        var httpService = requestContext.Http;

        try
        {
            string url = _server;
            if (_version == WFS_Version.version_1_0_0)
            {
                Serializer<Standard.OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType> ser = new Serializer<Standard.OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType>();
                url = AppendToUrl(url, "VERSION=1.0.0&SERVICE=WFS&REQUEST=GetCapabilities");
                Standard.OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType caps = await ser.FromUrlAsync(url, httpService, new RequestAuthorization() { Username = _authUser, Password = _authPassword, ClientCerticate = _x509certificate });
                capsHelper = new CapabilitiesHelper(caps);

                _gmlVersion = GML.GmlVersion.v1;
            }
            else if (_version == WFS_Version.version_1_1_0)
            {
                Serializer<Standard.OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType> ser = new Serializer<Standard.OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType>();
                url = AppendToUrl(url, "VERSION=1.1.0&SERVICE=WFS&REQUEST=GetCapabilities");
                Standard.OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType caps = await ser.FromUrlAsync(url, httpService, new RequestAuthorization() { Username = _authUser, Password = _authPassword, ClientCerticate = _x509certificate });
                capsHelper = new CapabilitiesHelper(caps);

                _gmlVersion = GML.GmlVersion.v3;
            }
        }
        catch (System.Exception /*ex*/)
        {
            return false;
        }
        if (capsHelper == null)
        {
            return false;
        }

        _GF_HttpGet = capsHelper.GetFeatureTypeOnlineResourceHttpGet;
        _GF_HttpPost = capsHelper.GetFeatureTypeOnlineResourceHttpPost;

        #region Get Feature Layers with one request -> Faster

        try
        {
            string typeNames = String.Join(",", capsHelper.FeatureTypeList.Select(f => f.Name).ToArray());

            string xml = null;

            string versionString = "1.0.0";
            switch (_version)
            {
                case WFS_Version.version_1_1_0:
                    versionString = "1.1.0";
                    break;
            }
            string url = AppendToUrl(_server, "VERSION=" + versionString + "&SERVICE=WFS&REQUEST=DescribeFeatureType&TYPENAME=" + typeNames);
            //xml = await dotNETConnector.DownloadXmlAsync(url, _conn, null);
            xml = await httpService.GetStringAsync(url, new RequestAuthorization(_authUser, _authPassword));

            DescribeFeatureHelper dfh = new DescribeFeatureHelper(xml);

            foreach (CapabilitiesHelper.FeatureType featureType in capsHelper.FeatureTypeList)
            {
                OgcWfsLayer layer = new OgcWfsLayer(featureType.Title, featureType.Name, this, queryable: true);
                if (!dfh.TypeExisits(layer.ID))
                {
                    throw new Exception("Unknown layer: " + layer.ID);
                }

                layer.TargetNamespace = dfh.TargetNamespace;

                foreach (DescribeFeatureHelper.Field field in dfh.TypeFields(layer.ID))
                {
                    FieldType fType = FieldType.String;

                    if (field.Type == "shape" || field.Type == "gml:GeometryPropertyType")
                    {
                        fType = FieldType.Shape;
                    }
                    Field f = new Field(field.Name, fType);

                    layer.Fields.Add(f);
                }

                _layers.Add(layer);
            }
        }
        catch (Exception /*ex*/)
        {
            _layers.Clear();
        }

        #endregion

        if (_layers.Count == 0)
        {
            #region Classical: Each layer extra -> slow

            foreach (CapabilitiesHelper.FeatureType featureType in capsHelper.FeatureTypeList)
            {
                OgcWfsLayer layer = new OgcWfsLayer(featureType.Title, featureType.Name, this, queryable: true);
                _layers.Add(layer);

                #region Fields

                string xml = null;
                if (_version == WFS_Version.version_1_0_0)
                {
                    string url = AppendToUrl(_server, "VERSION=1.0.0&SERVICE=WFS&REQUEST=DescribeFeatureType&TYPENAME=" + layer.ID);

                    xml = await httpService.GetStringAsync(url, new RequestAuthorization(_authUser, _authPassword));
                }
                else if (_version == WFS_Version.version_1_1_0)
                {
                    string url = AppendToUrl(_server, "SERVICE=WFS");
                    string post = layer.CreateDescribeFeatureType1_1_0("text/xml; subtype=gml/3.1.1");

                    xml = await httpService.PostXmlAsync(url, post, new RequestAuthorization(_authUser, _authPassword));
                }

                DescribeFeatureHelper dfh = new DescribeFeatureHelper(xml);
                layer.TargetNamespace = dfh.TargetNamespace;

                foreach (DescribeFeatureHelper.Field field in dfh.TypeFields(layer.ID))
                {
                    FieldType fType = FieldType.String;

                    if (field.Type == "shape" || field.Type == "gml:GeometryPropertyType")
                    {
                        fType = FieldType.Shape;
                    }
                    Field f = new Field(field.Name, fType);

                    layer.Fields.Add(f);
                }

                #endregion
            }

            #endregion
        }

        return true;
    }

    public bool IsDirty
    {
        get
        {
            return false;
        }
        set
        {
        }
    }

    public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(new ErrorResponse(_map.Services.IndexOf(this), this.ID, "Not Implemented!", ""));
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(new ServiceResponse(_map.Services.IndexOf(this), _id));
    }

    public int Timeout
    {
        get
        {
            return _timeout;
        }
        set
        {
            _timeout = value;
        }
    }

    public IMap Map
    {
        get { return _map; }
    }

    public double MinScale
    {
        get
        {
            return 0D;
        }
        set
        {

        }
    }

    public double MaxScale
    {
        get
        {
            return 0D;
        }
        set
        {

        }
    }

    private string _collectionId = String.Empty;
    public string CollectionId
    {
        get
        {
            return _collectionId;
        }
        set
        {
            _collectionId = value;
        }
    }

    private bool _checkSpatialConstraints = false;
    public bool CheckSpatialConstraints
    {
        get { return _checkSpatialConstraints; }
        set { _checkSpatialConstraints = value; }
    }

    public bool IsBaseMap
    {
        get
        {
            return false;
        }
        set
        {
        }
    }

    public BasemapType BasemapType
    {
        get
        {
            return BasemapType.Normal;
        }
        set
        {
        }
    }

    public string BasemapPreviewImage { get; set; }

    private int[] _supportedCrs = null;
    public int[] SupportedCrs
    {
        get { return _supportedCrs; }
        set { _supportedCrs = value; }
    }

    #endregion

    #region IClone Member

    public IMapService Clone(IMap parent)
    {
        WfsService clone = new WfsService(_version);

        clone._map = parent ?? _map;

        clone._id = _id;
        clone._server = _server;
        clone._name = _name;

        clone._version = _version;
        clone._gmlVersion = _gmlVersion;

        foreach (ILayer layer in _layers)
        {
            if (layer == null)
            {
                continue;
            }

            clone._layers.Add(layer.Clone(clone));
        }
        clone._authUser = _authUser;
        clone._authPassword = _authPassword;

        clone.ShowInToc = ShowInToc;

        clone._featureCount = _featureCount;

        clone._checkSpatialConstraints = _checkSpatialConstraints;
        clone._collectionId = _collectionId;
        clone._url = _url;
        clone._x509certificate = _x509certificate;

        clone._GF_HttpGet = _GF_HttpGet;
        clone._GF_HttpPost = _GF_HttpPost;

        clone._interpretSrsAxis = _interpretSrsAxis;
        clone._supportedCrs = _supportedCrs;
        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;
        clone.CopyrightInfoId = this.CopyrightInfoId;
        clone.MetadataLink = this.MetadataLink;

        return clone;
    }

    #endregion

    #region IServiceCopyrightInfo 

    public string CopyrightInfoId { get; set; }
    public string MetadataLink { get; set; }

    #endregion

    #region Properties
    public System.Security.Cryptography.X509Certificates.X509Certificate X509Certificate
    {
        get { return _x509certificate; }
        set { _x509certificate = value; }
    }

    public int GetFeatureInfoFeatureCount
    {
        get { return _featureCount; }
        set { _featureCount = value; }
    }

    public bool InterpretSrsAxix
    {
        get { return _interpretSrsAxis; }
        set { _interpretSrsAxis = value; }
    }

    internal string AuthUsername { get { return _authUser; } }
    internal string AuthPassword { get { return _authPassword; } }

    #endregion

    #region Helper
    internal string AppendToUrl(string url, string parameter)
    {
        if (url.Contains("?"))
        {
            if (url[url.Length - 1] == '?')
            {
                return url + parameter;
            }

            return url + "&" + parameter;
        }
        else
        {
            return url + "?" + parameter;
        }
    }
    #endregion
}
