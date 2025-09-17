using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wms;
using E.Standard.OGC.Schema.wms_1_1_1;
using E.Standard.OGC.Schema.wms_1_3_0;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.Proxy;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using E.Standard.WebMapping.GeoServices.OGC.Extensions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.OGC.WMS;

public class WmsService : IMapService2,
                          IMapServiceLegend, /*IServiceLegend2,*/
                          IMapServiceSupportedCrs,
                          IExportableOgcService,
                          IMapServiceMetadataInfo,
                          IMapServiceDescription,
                          IMapServiceInitialException,
                          IDynamicService,
                          IImageServiceType,
                          IMapServiceCapabilities
{
    internal IMap _map;
    private string _name = String.Empty;
    private string _id = String.Empty;
    private string _server = String.Empty;
    internal readonly WMS_Version _version = WMS_Version.version_1_1_1;
    internal readonly WMS_Vendor _vendor = WMS_Vendor.Unknown;
    internal readonly SLD_Version _sldVersion = SLD_Version.unused;
    private bool _isDirty = false;
    private float _opacity = 1.0f;
    private bool _useToc = true;
    private LayerCollection _layers;
    private Envelope _initialExtent = new Envelope();
    private int _dpi = 96, _featureCount = 30;
    private string _authUser = String.Empty, _authPassword = String.Empty;
    private string _onlineResourceGetMap = String.Empty;
    private string _onlineResourceGetFeatureInfo = String.Empty;
    private string _onlineResourceGetLegendGraphic = String.Empty;
    private string _getMapFormat = "image/png", _imgExtension = "png";
    private string _getFeatureInfoFormat = String.Empty;
    private string _ticketServer = String.Empty;
    private TicketHttpService _ticketHttpService = null;
    private ExceptionResponse _initErrorResponse = null, _diagnosticsErrorResponse = null;
    private System.Security.Cryptography.X509Certificates.X509Certificate _x509certificate = null;
    private ServiceTheme[] _serviceThemes = null;
    private RequestAuthorization _requestAuthorization = null;
    private readonly WMS_LayerOrder _layerOrder;

    public WmsService(WMS_Version version, string getMapFormat, string getFeatureInfoFormat, WMS_LayerOrder layerOrder, WMS_Vendor vendor, SLD_Version sldVersion)
    {
        _version = version;
        if (!String.IsNullOrEmpty(getMapFormat))
        {
            _getMapFormat = getMapFormat;
            if (_getMapFormat.Contains("/"))
            {
                int pos = _getMapFormat.LastIndexOf("/");
                _imgExtension = _getMapFormat.Substring(pos + 1, _getMapFormat.Length - pos - 1).ToFileExtenstion();
            }
        }
        _getFeatureInfoFormat = getFeatureInfoFormat;
        _layers = new LayerCollection(this);
        _layerOrder = layerOrder;
        _vendor = vendor;

        ShowInToc = true;
        _sldVersion = sldVersion;
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

    private string _url = String.Empty;
    public string Url
    {
        get { return _url; }
        set { _url = value; }
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
            return _opacity;
        }
        set
        {
            _opacity = value;
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
            return _useToc;
        }
        set
        {
            _useToc = value;
        }
    }

    public LayerCollection Layers
    {
        get { return _layers; }
    }

    public Envelope InitialExtent
    {
        get { return _initialExtent; }
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Image; }
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

        _serviceThemes = serviceThemes;

        if ((!String.IsNullOrEmpty(authUser) && !String.IsNullOrEmpty(authPwd)) || !String.IsNullOrEmpty(token) || _x509certificate != null)
        {
            _requestAuthorization = new RequestAuthorization()
            {
                Username = authUser,
                Password = authPwd,
                UrlToken = token,
                ClientCerticate = _x509certificate
            };
        }

        //if (!String.IsNullOrEmpty(_ticketServer))
        //{
        //    _requestWrapper = new TicketWebRequest(_ticketServer, _authUser, _authPassword);
        //    ((TicketWebRequest)_requestWrapper).WebProxy = _proxy;
        //    _conn.WebRequestWrapper = _requestWrapper;
        //}

        _ticketHttpService = new TicketHttpService(_ticketServer, _authUser, _authPassword);

        return true;
    }

    async public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        if (map == null)
        {
            return false;
        }

        _initErrorResponse = null;
        var httpService = requestContext.Http;

        try
        {
            using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "Init", ""))
            {
                _map = map;
                _layers = new LayerCollection(this);

                CapabilitiesHelper capsHelper = null;
                try
                {

                    string url = _server.Replace("[webgis-username]", Map.Environment.UserString(webgisConst.UserName));
                    if (_version == WMS_Version.version_1_1_1)
                    {
                        Serializer<WMT_MS_Capabilities> ser = new Serializer<WMT_MS_Capabilities>();
                        url = AppendToUrl(url, "VERSION=1.1.1&SERVICE=WMS&REQUEST=GetCapabilities", false);
                        WMT_MS_Capabilities caps = await ser.FromUrlAsync(_ticketHttpService.ModifyUrl(httpService, url), httpService, _requestAuthorization);
                        capsHelper = new CapabilitiesHelper(caps, _vendor);
                    }
                    else if (_version == WMS_Version.version_1_3_0)
                    {
                        Serializer<WMS_Capabilities> ser = new Serializer<WMS_Capabilities>();

                        ser.AddReplaceNamespace("https://www.opengis.net/wms", "http://www.opengis.net/wms");
                        ser.AddReplaceNamespace("https://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink");
                        ser.AddReplaceNamespace("https://www.w3.org/2001/XMLSchema-instance", "http://www.w3.org/2001/XMLSchema-instance");

                        url = AppendToUrl(url, "VERSION=1.3.0&SERVICE=WMS&REQUEST=GetCapabilities", false);
                        WMS_Capabilities caps = await ser.FromUrlAsync(_ticketHttpService.ModifyUrl(httpService, url), httpService, _requestAuthorization);
                        capsHelper = new CapabilitiesHelper(caps, _vendor);
                    }
                }
                catch (System.Exception ex)
                {
                    requestContext.GetRequiredService<IExceptionLogger>()
                        .LogException(_map, this.Server, this.Service, "Init", ex);

                    _initErrorResponse = new ExceptionResponse(_map.Services.IndexOf(this), this.ID, ex);

                    return false;
                }

                this.ServiceDescription = capsHelper.GetServiceDescription();

                _onlineResourceGetMap = capsHelper.GetMapOnlineResouce.OrTake(_server);
                _onlineResourceGetFeatureInfo = capsHelper.GetFeatureInfoOnlineResouce.OrTake(_server);
                _onlineResourceGetLegendGraphic = capsHelper.GetLegendGraphicOnlineResource.OrTake(_server);

                #region Layers

                // Temporäre Liste => Falls Init mehrfach/gleichzeitg aufgerufen wird
                // Am schluss dann an LayerCollection übergeben
                List<Core.Layer> layers = new List<Core.Layer>();

                foreach (CapabilitiesHelper.WMSLayer wmslayer in capsHelper.LayersWithStyle)
                {
                    OgcWmsLayer layer = new OgcWmsLayer(
                        wmslayer.Title,
                        wmslayer.Name,
                        LayerType.image,
                        this,
                        queryable: wmslayer.Queryable);

                    layer.MinScale = wmslayer.MinScale;
                    layer.MaxScale = wmslayer.MaxScale;
                    layer.Visible = false;
                    ServiceHelper.SetLayerScale(this, layer);
                    layers.Add(layer);
                }

                _layers.SetItems(layers);

                #endregion

                pLogger.Success = true;

                this.Diagnostics = ServiceTheme.CheckServiceLayers(this, _serviceThemes);

                if (this.Diagnostics != null && this.Diagnostics.State != ServiceDiagnosticState.Ok)
                {
                    _diagnosticsErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, new System.Exception(this.Diagnostics.Message));
                }

                return true;
            }
        }
        catch (System.Exception ex)
        {
            _initErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex, Const.InitServiceExceptionPreMessage);
            return false;
        }
    }

    public bool IsDirty
    {
        get
        {
            return _isDirty;
        }
        set
        {
            _isDirty = value;
        }
    }

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map?.Services != null ? this.Map.Services.IndexOf(this) : 0, this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        var httpService = requestContext.Http;

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "GetMap", ""))
        {
            if (!ServiceHelper.VisibleInScale(this, _map))
            {
                pLogger.Success = pLogger.SuppressLogging = true;
                return new EmptyImage(_map.Services.IndexOf(this), this.ID);
            }

            IDisplay display = _map;
            if (_map.DisplayRotation != 0.0)
            {
                display = Display.TransformedDisplay(_map);
            }

            StringBuilder reqArgs = new StringBuilder();

            #region Version

            switch (_version)
            {
                case WMS_Version.version_1_1_1:
                    reqArgs.Append("VERSION=1.1.1");
                    break;
                case WMS_Version.version_1_3_0:
                    reqArgs.Append("VERSION=1.3.0");
                    break;
            }

            #endregion

            #region Request/Service

            if (_onlineResourceGetMap.IndexOf("?service=wms", StringComparison.InvariantCultureIgnoreCase) < 0 &&
               _onlineResourceGetMap.IndexOf("&service=wms", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                reqArgs.Append("&SERVICE=WMS");
            }

            reqArgs.Append("&REQUEST=GetMap");

            #endregion

            #region Layers & Styles

            StringBuilder styles = new StringBuilder();
            reqArgs.Append("&LAYERS=");

            bool firstLayer = true, hasStyles = false;
            bool hasVisibleLayers = false;

            var layers = new List<ILayer>();

            foreach (var layer in _layerOrder == WMS_LayerOrder.Down ? _layers.ToArray().Reverse() : _layers)
            {
                bool visible = layer.Visible;

                if (visible || this.SealedLayers)  // sealedLayers => show awalys all layers
                {
                    hasVisibleLayers = true;
                    if (!firstLayer)
                    {
                        reqArgs.Append(",");
                        styles.Append(",");
                    }
                    string layerId = layer.ID;
                    string layerStyle = String.Empty;
                    if (layer.ID.Contains("|"))
                    {
                        int pos = layer.ID.LastIndexOf("|");
                        layerId = layer.ID.Substring(0, pos);
                        layerStyle = layer.ID.Substring(pos + 1, layer.ID.Length - pos - 1);
                        hasStyles = true;
                    }

                    reqArgs.Append(layerId);
                    styles.Append(layerStyle);

                    firstLayer = false;
                }
            }
            if (!hasVisibleLayers)
            {
                pLogger.Success = pLogger.SuppressLogging = true;
                return new EmptyImage(_map.Services.IndexOf(this), this.ID);
            }

            #region Styles

            reqArgs.Append("&STYLES=");
            if (hasStyles)
            {
                reqArgs.Append(styles.ToString());
            }

            #endregion

            #endregion

            #region SRS

            if (_map.SpatialReference != null)
            {
                if (_version == WMS_Version.version_1_3_0)
                {
                    reqArgs.Append($"&CRS=EPSG:{_map.SpatialReference.Id}");
                }
                else
                {
                    reqArgs.Append($"&SRS=EPSG:{_map.SpatialReference.Id}");
                }
            }

            #endregion

            #region BBOX

            reqArgs.Append("&BBOX=");
            if (_version == WMS_Version.version_1_3_0 && _map.SpatialReference != null)
            {
                switch (_map.SpatialReference.AxisX)
                {
                    case AxisDirection.North:
                    case AxisDirection.South:
                        reqArgs.Append(display.Extent.MinY.ToPlatformNumberString()).Append(",");
                        break;
                    case AxisDirection.East:
                    case AxisDirection.West:
                        reqArgs.Append(display.Extent.MinX.ToPlatformNumberString()).Append(",");
                        break;
                }
                switch (_map.SpatialReference.AxisY)
                {
                    case AxisDirection.North:
                    case AxisDirection.South:
                        reqArgs.Append(display.Extent.MinY.ToPlatformNumberString()).Append(",");
                        break;
                    case AxisDirection.East:
                    case AxisDirection.West:
                        reqArgs.Append(display.Extent.MinX.ToPlatformNumberString()).Append(",");
                        break;
                }
                switch (_map.SpatialReference.AxisX)
                {
                    case AxisDirection.North:
                    case AxisDirection.South:
                        reqArgs.Append(display.Extent.MaxY.ToPlatformNumberString()).Append(",");
                        break;
                    case AxisDirection.East:
                    case AxisDirection.West:
                        reqArgs.Append(display.Extent.MaxX.ToPlatformNumberString()).Append(",");
                        break;
                }
                switch (_map.SpatialReference.AxisY)
                {
                    case AxisDirection.North:
                    case AxisDirection.South:
                        reqArgs.Append(display.Extent.MaxY.ToPlatformNumberString());
                        break;
                    case AxisDirection.East:
                    case AxisDirection.West:
                        reqArgs.Append(display.Extent.MaxX.ToPlatformNumberString());
                        break;
                }
            }
            else
            {
                reqArgs
                    .Append(display.Extent.MinX.ToPlatformNumberString()).Append(",")
                    .Append(display.Extent.MinY.ToPlatformNumberString()).Append(",")
                    .Append(display.Extent.MaxX.ToPlatformNumberString()).Append(",")
                    .Append(display.Extent.MaxY.ToPlatformNumberString());
            }

            #endregion

            #region ImageSize

            reqArgs
                .Append("&WIDTH=").Append(display.ImageWidth)
                .Append("&HEIGHT=").Append(display.ImageHeight);

            #endregion

            #region ImageFormat

            reqArgs.Append($"&FORMAT={_getMapFormat}");

            #endregion

            #region Background

            if (_getMapFormat.ToLower().Contains("/jpeg") || _getMapFormat.ToLower().Contains("/jpg"))
            {
                reqArgs.Append("&BGCOLOR=0xFFFFFF");
            }
            else
            {
                reqArgs.Append("&BGCOLOR=0xFFFFFF&TRANSPARENT=TRUE");
            }

            #endregion

            #region Vendor Parameters

            #region DPI

            var dpi = _map.Dpi;
            switch (_vendor)
            {
                case WMS_Vendor.GeoServer2_x:
                    //if (_map.SpatialReference.Id == 3857)
                    //{
                    //    using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SpatialReferences, 3857, 4326))
                    //    {
                    //        var centerPoint = new Point(display.Extent.CenterPoint);
                    //        transformer.Transform(centerPoint);
                    //        dpi *= Math.Cos(centerPoint.Y / 180.0 * Math.PI);
                    //    }
                    //}
                    reqArgs.Append($"&format_options=dpi:{(int)dpi}");
                    break;
                case WMS_Vendor.MapServer:
                    reqArgs.Append($"&map_resolution={dpi.ToPlatformNumberString()}");
                    break;
                default:
                    reqArgs.Append($"&DPI={dpi.ToPlatformNumberString()}");
                    break;
            }

            #endregion

            #region Other Parameters

            reqArgs.AppendSldVersion(_sldVersion);

            #endregion

            #endregion

            string url = AppendToUrl(_onlineResourceGetMap,
                                     reqArgs.ToString()).Replace("[webgis-username]",
                                     Map.Environment.UserString(webgisConst.UserName));
            try
            {
                if (requestContext.Trace)
                {
                    requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                        .LogString(this.Server, this.Service, "GetMap", "WMS Request: " + url);
                }

                string filename = $"wms{Guid.NewGuid().ToString("N").ToLower()}.{_imgExtension}";
                string filePath = _map.AsOutputFilename(filename);
                string fileUrl = _map.AsOutputUrl(filename);

                var imageData = await httpService.GetDataAsync(_ticketHttpService.ModifyUrl(httpService, url),
                                                               _requestAuthorization,
                                                               timeOutSeconds: this.Timeout.ToTimeoutSecondOrDefault());

                if (imageData.Length > 0 && imageData[0] == '<')
                {
                    var errorMessage = Encoding.UTF8.GetString(imageData);

                    Console.WriteLine($"Ivalid WMS Request: {url}");

                    return new ErrorResponse(_map.Services.IndexOf(this), _id, errorMessage, url);
                }

                var fileBytes = new MemoryStream(imageData);

                if (_map.DisplayRotation != 0.0)
                {
                    using (var sourceImg = Current.Engine.CreateBitmap(fileBytes))
                    using (var bm = Display.TransformImage(sourceImg, display, _map))
                    {
                        filename = $"rot_{Guid.NewGuid():N}.{_imgExtension}";
                        filePath = _map.AsOutputFilename(filename);
                        fileUrl = _map.AsOutputUrl(filename);
                        var imageFormat = _imgExtension == "png"
                                            ? ImageFormat.Png
                                            : ImageFormat.Jpeg;

                        await bm.SaveOrUpload(filePath, imageFormat);
                    }
                }
                else
                {
                    await fileBytes.SaveOrUpload(filePath);
                }

                ErrorResponse innerErrorResponse = new ErrorResponse(this);
                if (this.Diagnostics != null && this.Diagnostics.ThrowExeption(this))
                {
                    if (_diagnosticsErrorResponse != null)
                    {
                        innerErrorResponse.AppendMessage(_diagnosticsErrorResponse);
                    }
                }

                pLogger.Success = true;
                return new ImageLocation(_map.Services.IndexOf(this), _id,
                    filePath, fileUrl)
                {
                    InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null,
                    Scale = _map.MapScale,
                    Extent = new Envelope(_map.Extent)
                };
            }
            catch (System.Exception ex)
            {
                requestContext.GetRequiredService<IExceptionLogger>()
                    .LogException(_map, this.Server, this.Service, "GetMap", ex);

                return new ExceptionResponse(_map.Services.IndexOf(this), _id, ex);
            }
        }
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(new ExceptionResponse(
            _map.Services.IndexOf(this),
            _id,
            new System.Exception("The method or operation is not implemented.")));
    }

    public int Timeout { get; set; } = 20;

    public IMap Map
    {
        get { return _map; }
    }

    private double _minScale = 0.0;
    private double _maxScale = 0.0;
    public double MinScale
    {
        get { return _minScale; }
        set { _minScale = value; }
    }
    public double MaxScale
    {
        get { return _maxScale; }
        set { _maxScale = value; }
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

    private bool _isBasemap = false;
    public bool IsBaseMap
    {
        get
        {
            return _isBasemap;
        }
        set
        {
            _isBasemap = value;
        }
    }

    private BasemapType _basemapType = BasemapType.Normal;
    public BasemapType BasemapType
    {
        get
        {
            return _basemapType;
        }
        set
        {
            _basemapType = value;
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

    #region IDymamicService

    public ServiceDynamicPresentations CreatePresentationsDynamic { get; set; }
    public ServiceDynamicQueries CreateQueriesDynamic { get; set; }

    #endregion

    #region IImageServiceType 

    public ImageServiceType ImageServiceType { get; set; }

    #endregion

    #region IClone Member

    public IMapService Clone(IMap parent)
    {
        WmsService clone = new WmsService(_version, _getMapFormat, _getFeatureInfoFormat, _layerOrder, _vendor, _sldVersion);

        clone._map = parent ?? _map;

        clone._id = _id;
        clone._server = _server;
        clone._name = _name;

        foreach (ILayer layer in _layers)
        {
            if (layer == null)
            {
                continue;
            }

            clone._layers.Add(layer.Clone(clone));
        }
        clone._initialExtent = new Envelope(_initialExtent);
        clone._isDirty = _isDirty;
        clone._opacity = _opacity;
        clone.OpacityFactor = this.OpacityFactor;
        clone._dpi = _dpi;
        clone._authUser = _authUser;
        clone._authPassword = _authPassword;
        clone._minScale = _minScale;
        clone._maxScale = _maxScale;
        clone.ShowInToc = this.ShowInToc;

        clone.Timeout = this.Timeout;

        clone._onlineResourceGetFeatureInfo = _onlineResourceGetFeatureInfo;
        clone._onlineResourceGetMap = _onlineResourceGetMap;
        clone._onlineResourceGetLegendGraphic = _onlineResourceGetLegendGraphic;
        clone._getMapFormat = _getMapFormat;
        clone._imgExtension = _imgExtension;
        clone._getFeatureInfoFormat = _getFeatureInfoFormat;
        clone._featureCount = _featureCount;

        clone._checkSpatialConstraints = _checkSpatialConstraints;
        clone._collectionId = _collectionId;
        clone._url = _url;
        clone._x509certificate = _x509certificate;

        clone._legendVisible = _legendVisible;
        clone._showServiceLegendInMap = _showServiceLegendInMap;
        clone._legendOptMethod = _legendOptMethod;
        clone._legendOptSymbolScale = _legendOptSymbolScale;
        clone._fixLegendUrl = _fixLegendUrl;

        clone._initErrorResponse = _initErrorResponse;
        clone._diagnosticsErrorResponse = _diagnosticsErrorResponse;
        clone._serviceThemes = _serviceThemes;

        clone._isBasemap = _isBasemap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone._supportedCrs = _supportedCrs;
        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;
        clone.CopyrightInfoId = this.CopyrightInfoId;
        clone.MetadataLink = this.MetadataLink;

        clone._exportWms = _exportWms;
        clone._ogcEnvelope = _ogcEnvelope != null ? new Envelope(_ogcEnvelope) : null;

        clone.CopyrightText = this.CopyrightText;
        clone.ServiceDescription = this.ServiceDescription;

        clone.LayerProperties = this.LayerProperties;

        clone._ticketServer = _ticketServer;
        clone._ticketHttpService = this._ticketHttpService;

        clone._requestAuthorization = this._requestAuthorization;

        clone.CreatePresentationsDynamic = this.CreatePresentationsDynamic;
        clone.CreateQueriesDynamic = this.CreateQueriesDynamic;
        clone.ImageServiceType = this.ImageServiceType;

        clone.ServiceThemes = this.ServiceThemes;
        clone.SealedLayers = this.SealedLayers;

        return clone;
    }

    #endregion

    #region Helper

    internal string AppendToUrl(string url, string parameters, bool replaceKeys = true)
    {
        //
        // Wird beim WMS für Gemeinden verwendet. Hier wird der Username für den Filter als URL Parameter übergeben
        // zB &ogc_username=[role-parameter:GEM]
        // Die Ersetzung sollte nicht beim GetCapabilites erfolgen, weil sonst schon der ersetzte String als Onlineparamter zurückkommt... Sollte aber nicht sein,
        // weil Service (Clone) ja öferter verwendet wird...
        //
        if (replaceKeys && url.Contains("[") && url.Contains("]"))
        {
            url = CmsHlp.ReplaceFilterKeys(this.Map, null,
                this.Map.Environment.UserValue(webgisConst.UserIdentification, null) as CmsDocument.UserIdentification,
                url);
        }

        return url.AppendUrlParameters(parameters);
    }

    #endregion

    #region Properties

    internal WMS_Version WMSVersion
    {
        get { return _version; }
    }
    internal string GetFeatureInfoFormat
    {
        get
        {
            if (_getFeatureInfoFormat.Contains("->"))
            {
                int pos = _getFeatureInfoFormat.IndexOf("->");
                return _getFeatureInfoFormat.Substring(0, pos);
            }
            return _getFeatureInfoFormat;
        }
    }
    internal string GetFeatureInfoTarget
    {
        get
        {
            if (_getFeatureInfoFormat.Contains("->"))
            {
                int pos = _getFeatureInfoFormat.IndexOf("->");
                return _getFeatureInfoFormat.Substring(pos + 2, _getFeatureInfoFormat.Length - pos - 2);
            }
            return String.Empty;
        }
    }
    internal string GetMapFormat
    {
        get { return _getMapFormat; }
    }
    internal string GetFeatureInfoOnlineResouce
    {
        get { return _onlineResourceGetFeatureInfo; }
    }

    public bool SealedLayers { get; set; } = false;

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

    public string TicketServer
    {
        get
        {
            return _ticketServer;
        }
        set { _ticketServer = value; }
    }

    internal string AuthUsername { get { return _authUser; } }
    internal string AuthPassword { get { return _authPassword; } }

    #endregion

    #region IService2 Member

    public ServiceResponse PreGetMap()
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(_map.Services.IndexOf(this), this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        if (_diagnosticsErrorResponse != null && this.Diagnostics != null && this.Diagnostics.ThrowExeption(this.DiagnosticsWaringLevel))
        {
            return new ErrorResponse(_map.Services.IndexOf(this), this.ID, _diagnosticsErrorResponse.ErrorMessage, _diagnosticsErrorResponse.ErrorMessage2);
        }

        if (!ServiceHelper.VisibleInScale(this, _map))
        {
            return new EmptyImage(_map.Services.IndexOf(this), this.ID);
        }

        bool hasVisibleLayers = false;
        foreach (var layer in _layers)
        {
            bool visible = layer.Visible;

            if (visible)
            {
                hasVisibleLayers = true;
                break;
            }
        }
        if (!hasVisibleLayers)
        {
            return new EmptyImage(_map.Services.IndexOf(this), this.ID);
        }

        return null;
    }

    public IEnumerable<ILayerProperties> LayerProperties { get; set; }

    private ServiceTheme[] _themes = null;
    public IEnumerable<ServiceTheme> ServiceThemes
    {
        get { return _themes; }
        set { _themes = value?.ToArray(); }
    }

    #endregion

    #region IServiceLegend Member

    bool _legendVisible = false;
    public bool LegendVisible
    {
        get
        {
            return _legendVisible;
        }
        set
        {
            _legendVisible = value;
        }
    }

    async public Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext)
    {
        var httpService = requestContext.Http;

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "GetLegend", ""))
        {
            if (_map == null)
            {
                return new ServiceResponse(_map.Services.IndexOf(this), this.ID);
            }

            if (!_legendVisible)
            {
                return new ServiceResponse(_map.Services.IndexOf(this), this.ID);
            }

            if (!String.IsNullOrEmpty(FixLegendUrl))
            {
                return new ImageLocation(_map.Services.IndexOf(this),
                    this.ID, String.Empty, FixLegendUrl);
            }

            int legendHeight = 0, legendWidth = 0;
            Dictionary<string, byte[]> imageData = new Dictionary<string, byte[]>();
            Dictionary<string, string> layerLabels = new Dictionary<string, string>();

            using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10))
            using (var bitmap = Current.Engine.CreateBitmap(1, 1))
            using (var canvas = bitmap.CreateCanvas())
            {
                foreach (var layer in _layers)
                {
                    string layerLabel = this.LayerProperties.LegendAliasname(layer.ID)
                                            .OrTake(this.LayerProperties.Aliasname(layer.ID))
                                            .OrTake(layer.Name);

                    bool visible = layer.Visible && this.LayerProperties.ShowInLegend(layer.ID);

                    if (visible)
                    {
                        visible = Globals.VisibleInServiceMapScale(this.Map, layer);
                    }

                    if (!visible)
                    {
                        continue;
                    }

                    string layerId = layer.ID;
                    string layerStyle = String.Empty;
                    if (layer.ID.Contains("|"))
                    {
                        int pos = layer.ID.LastIndexOf("|");
                        layerId = layer.ID.Substring(0, pos);
                        layerStyle = layer.ID.Substring(pos + 1, layer.ID.Length - pos - 1);
                    }

                    StringBuilder reqArgs = new StringBuilder();
                    switch (_version)
                    {
                        case WMS_Version.version_1_1_1:
                            reqArgs.Append("VERSION=1.1.1");
                            break;
                        case WMS_Version.version_1_3_0:
                            reqArgs.Append("VERSION=1.3.0");
                            break;
                    }
                    if (!_onlineResourceGetMap.ToLower().Contains("service=wms"))
                    {
                        reqArgs.Append("&SERVICE=WMS");
                    }

                    reqArgs
                        .Append("&REQUEST=GetLegendGraphic")
                        .Append($"&LAYER={layerId}&STYLE={layerStyle}")
                        .Append($"&FORMAT={_getMapFormat}")
                        .AppendSldVersion(_sldVersion);

                    string url = AppendToUrl(
                        _onlineResourceGetLegendGraphic, 
                        reqArgs.ToString());
                    try
                    {
                        var fileBytes = new MemoryStream(await httpService.GetDataAsync(_ticketHttpService.ModifyUrl(httpService, url),
                                                                                        _requestAuthorization,
                                                                                        timeOutSeconds: this.Timeout.ToTimeoutSecondOrDefault()));
                        try
                        {
                            using (var lImage = Current.Engine.CreateBitmap(fileBytes))
                            {
                                if (lImage.Height > 25)
                                {
                                    legendHeight += (int)Math.Max(20f, lImage.Height + 20f + 4f);
                                    legendWidth = Math.Max(legendWidth, lImage.Width + 20);

                                    legendWidth = Math.Max(legendWidth, (int)canvas.MeasureText($"{layerLabel}:", font).Width);
                                }
                                else
                                {
                                    legendHeight += (int)Math.Max(20f, lImage.Height);
                                    legendWidth = Math.Max(legendWidth, lImage.Width);

                                    legendWidth = Math.Max(legendWidth, (int)(canvas.MeasureText(layerLabel, font).Width + 45f));
                                }

                                imageData.Add(layer.Name, fileBytes.ToArray());
                                layerLabels.Add(layer.Name, layerLabel);
                            }
                        } 
                        catch
                        {
                            requestContext
                                .GetRequiredService<IExceptionLogger>()
                                .LogException(_map, this.Server, this.Service, "GetLegend",
                                    new System.Exception($"Can't load legend image: {Encoding.UTF8.GetString(fileBytes.ToArray())}"));
                        }
                    }
                    catch (System.Exception ex)
                    {
                        requestContext
                            .GetRequiredService<IExceptionLogger>()
                            .LogException(_map, this.Server, this.Service, "GetLegend", ex);

                        return new ExceptionResponse(_map.Services.IndexOf(this), _id, ex);
                    }
                }
            }

            if (legendWidth == 0 || legendHeight == 0)
            {
                return new ServiceResponse(_map.Services.IndexOf(this), this.ID);
            }

            var stringFormat = Current.Engine.CreateDrawTextFormat();
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;

            using (var bitmap = Current.Engine.CreateBitmap(legendWidth, legendHeight))
            using (var canvas = bitmap.CreateCanvas())
            using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                canvas.Clear(ArgbColor.White);
                canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

                float y = 0f;
                foreach (string layerName in imageData.Keys)
                {
                    using (MemoryStream ms = new MemoryStream(imageData[layerName]))
                    using (var lImage = Current.Engine.CreateBitmap(ms))
                    {
                        string label = layerLabels.ContainsKey(layerName) && !String.IsNullOrEmpty(layerName) ? layerLabels[layerName] : layerName;

                        if (lImage.Height > 25)
                        {
                            canvas.DrawText($"{label}:", font, blackBrush, new CanvasPointF(3f, y + 1f), stringFormat);
                            y += 20f;
                            canvas.DrawBitmap(lImage, new CanvasPointF(20f, y + Math.Max(0f, (20f - lImage.Height) / 2f)));

                            y += lImage.Height + 4f;
                        }
                        else
                        {
                            if ( //
                                 // Hier wurden einige Chaos Regeln geführt, weil jeder WMS Legenden anders erzeugt... 
                                 // und jeder glaubt, er macht es RICHTIG!??
                                 // WMS sucks sometimes...
                                 //
                                (lImage.Width <= lImage.Height * 2) ||  // Problem bei Kunden: hier wird der Text immer schon in der Legendgraphic angedruckt -> wenn Bild eine gewisse Breite hat, kein Text schreiben 
                                (lImage.Width <=40)  // Problem bei anderem Kunden: Images (für Raster) sind oft nur 5 Pixel hoch -> trotzdem beschreiften, auch wenn erstes Kriterium greift
                               )  
                            {
                                canvas.DrawText(label, font, blackBrush, new CanvasPointF(40f, y + 3f), stringFormat);
                            }

                            canvas.DrawBitmap(lImage, new CanvasPointF(3f, y + Math.Max(0f, (20f - lImage.Height) / 2f)));
                            y += Math.Max(20f, lImage.Height);
                        }
                    }
                }

                string fileTitle = $"legend_{System.Guid.NewGuid():N}.png";
                string filePath = _map.AsOutputFilename(fileTitle);
                string fileUrl = _map.AsOutputUrl(fileTitle);

                await bitmap.SaveOrUpload(filePath, ImageFormat.Png);

                pLogger.Success = true;
                return new ImageLocation(_map.Services.IndexOf(this), this.ID, filePath, fileUrl);
            }
        }
    }

    private bool _showServiceLegendInMap = true;
    public bool ShowServiceLegendInMap
    {
        get
        {
            return _showServiceLegendInMap;
        }
        set
        {
            _showServiceLegendInMap = value;
        }
    }

    private LegendOptimization _legendOptMethod = LegendOptimization.None;
    public LegendOptimization LegendOptMethod
    {
        get
        {
            return _legendOptMethod;
        }
        set
        {
            _legendOptMethod = value;
        }
    }

    private double _legendOptSymbolScale = 1000.0;
    public double LegendOptSymbolScale
    {
        get
        {
            return _legendOptSymbolScale;
        }
        set
        {
            _legendOptSymbolScale = value;
        }
    }

    private string _fixLegendUrl = String.Empty;
    public string FixLegendUrl
    {
        get
        {
            return _fixLegendUrl;
        }
        set
        {
            _fixLegendUrl = value;
        }
    }

    #endregion

    #region IServiceLegend2

    async public Task<IEnumerable<LayerLegendItem>> GetLayerLegendItemsAsync(string layerId, IHttpService httpService)
    {
        var layer = _layers.Where(l => l.ID == layerId).FirstOrDefault();

        if (layer == null)
        {
            return new LayerLegendItem[0];
        }

        string layerStyle = String.Empty;
        if (layer.ID.Contains("|"))
        {
            int pos = layer.ID.LastIndexOf("|");
            layerId = layer.ID.Substring(0, pos);
            layerStyle = layer.ID.Substring(pos + 1, layer.ID.Length - pos - 1);
        }

        StringBuilder reqArgs = new StringBuilder();
        switch (_version)
        {
            case WMS_Version.version_1_1_1:
                reqArgs.Append("VERSION=1.1.1");
                break;
            case WMS_Version.version_1_3_0:
                reqArgs.Append("VERSION=1.3.0");
                break;
        }
        if (!_onlineResourceGetMap.ToLower().Contains("service=wms"))
        {
            reqArgs.Append("&SERVICE=WMS");
        }

        reqArgs
            .Append("&REQUEST=GetLegendGraphic")
            .Append($"&LAYER={layerId}&STYLE={layerStyle}")
            .Append($"&FORMAT={_getMapFormat}");

        string url = AppendToUrl(_onlineResourceGetMap, reqArgs.ToString());

        try
        {
            var fileBytes = new MemoryStream(await httpService.GetDataAsync(_ticketHttpService.ModifyUrl(httpService, url),
                                                                            _requestAuthorization,
                                                                            timeOutSeconds: this.Timeout.ToTimeoutSecondOrDefault()));
            using (var image = Current.Engine.CreateBitmap(fileBytes))
            {
                return new LayerLegendItem[]
                {
                    new LayerLegendItem()
                    {
                        Label=String.Empty,
                        ContentType="image/png",
                        Width = image.Width,
                        Height = image.Height,
                        Data=fileBytes.ToArray()
                    }
                };
            }
        }
        catch
        {
        }

        return new LayerLegendItem[0];
    }

    #endregion

    #region IServiceCopyrightInfo 

    public string CopyrightInfoId { get; set; }
    public string MetadataLink { get; set; }

    #endregion

    #region IServiceDescription

    public string ServiceDescription { get; set; }
    public string CopyrightText { get; set; }

    #endregion

    #region IServiceInitialException

    public ErrorResponse InitialException => _initErrorResponse;

    #endregion

    #region IExportableOgcService Member

    private bool _exportWms = false;
    public bool ExportWms
    {
        get { return _exportWms; }
        set { _exportWms = value; }
    }

    private Envelope _ogcEnvelope = null;
    public Envelope OgcEnvelope
    {
        get { return _ogcEnvelope; }
        set { _ogcEnvelope = value; }
    }

    #endregion

    #region IMapServiceCapabilities

    private static MapServiceCapability[] _capabilities =
       [MapServiceCapability.Map, MapServiceCapability.Identify, MapServiceCapability.Legend];

    public MapServiceCapability[] Capabilities => _capabilities;

    #endregion
}
