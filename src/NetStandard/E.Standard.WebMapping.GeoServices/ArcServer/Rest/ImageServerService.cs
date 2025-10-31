using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class ImageServerService : IMapService2,
                                  IMapServiceSupportedCrs,
                                  IImageServiceType,
                                  IMapServiceLegend,
                                  IDynamicService,
                                  IMapServiceDescription,
                                  IMapServiceInitialException,
                                  IMapServiceAuthentication
{
    private IMap _map;

    private string _name = String.Empty;
    private string _id = String.Empty;
    private string _url = String.Empty;
    private string _server = String.Empty;
    private string _service = String.Empty;
    private float _opacity = 1f;
    private bool _useToc = true;
    private bool _isDirty = false;
    private readonly LayerCollection _layers;
    private string _errMsg = "";
    private ErrorResponse _initErrorResponse = null;
    public string _imageServiceName = String.Empty;

    private bool _tokenRequired = false;
    private string _tokenParam = String.Empty;

    public ImageServerService()
    {
        _layers = new LayerCollection(this);
        this.TokenExpiration = 60;
        this.ShowInToc = true;
    }

    #region ArcIS Properties

    private ArcIS_ImageFormat _imageFormat = ArcIS_ImageFormat.jpgpng;
    private ArcIS_PixelType _pixelType = ArcIS_PixelType.UNKNOWN;
    private string _nodata = String.Empty;
    private ArcIS_NoDataInterpretation _nodataInterpretation = ArcIS_NoDataInterpretation.esriNoDataMatchAny;
    private ArcIS_Interpolation _interpolation = ArcIS_Interpolation.RSP_BilinearInterpolation;
    private string _compressQaulity = String.Empty;
    private string _bandIDs = String.Empty;

    private string _serviceUrl = String.Empty;
    public string ServiceUrl
    {
        get { return _serviceUrl; }
        set { _serviceUrl = value; }
    }

    public ArcIS_ImageFormat ImageFormat
    {
        get { return _imageFormat; }
        set { _imageFormat = value; }
    }
    public ArcIS_PixelType PixelType
    {
        get { return _pixelType; }
        set { _pixelType = value; }
    }
    public string NoData
    {
        get { return _nodata; }
        set { _nodata = value; }
    }
    public ArcIS_NoDataInterpretation NoDataInterpretation
    {
        get { return _nodataInterpretation; }
        set { _nodataInterpretation = value; }
    }
    public ArcIS_Interpolation Interpolation
    {
        get { return _interpolation; }
        set { _interpolation = value; }
    }
    public string CompressionQuality
    {
        get { return _compressQaulity; }
        set { _compressQaulity = value; }
    }
    public string BandIDs
    {
        get { return _bandIDs; }
        set { _bandIDs = value; }
    }
    public string MosaicRule { get; set; }
    public string RenderingRule { get; set; }
    public string RenderingRuleIdentify { get; set; }

    public string PixelAliasname { get; set; }

    #region IMapServiceAuthentication

    public string Username { get; private set; }
    public string Password { get; private set; }
    public string StaticToken { get; private set; }


    public int TokenExpiration
    {
        get;
        set;
    }

    public ICredentials HttpCredentials { get; set; }

    #endregion

    public int SpatialReferenceId { get; set; }

    #endregion

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
        get { return _service; }
    }

    public string ServiceShortname => _imageServiceName;

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

    public bool CanBuffer
    {
        get { return false; }
    }

    public bool UseToc
    {
        get { return _useToc; }
        set { _useToc = value; }
    }

    public LayerCollection Layers
    {
        get { return _layers; }
    }

    public Core.Geometry.Envelope InitialExtent
    {
        get { return null; }
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Image; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string staticToken, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        _id = serviceID;
        _server = server;
        _service = url;

        this.Username = authUser;
        this.Password = authPwd;
        this.StaticToken = staticToken;

        _imageServiceName = ImageServiceName(this.Service);

        return true;
    }

    async public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        _map = map;
        _initErrorResponse = null;

        var httpService = requestContext.Http;

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, Service, "Init", "Init " + this.Server + " " + this.Service.Replace(" ", "_")))
        {
            try
            {
                //
                // Add Layer before get responseString!
                // otherwise _initErrorResponse never shown in Viewer
                // becouse a service must be visible to get an GetMapAsync Request and returning the _initErrorResponse
                // (without a layer the service is never "visible" => no GetMapAsync is called
                //
                string layerName = this.Service.Replace("/", "-");
                ImageServerLayer layer = new ImageServerLayer(layerName, "0", this, queryable: true);
                this.Layers.SetItems(new ILayer[] { layer });

                string url = $"{_serviceUrl}?f=json";
                var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

                string responseString = await authHandler.TryGetAsync(this, url);
                JsonImageService jsonImageService = JSerializer.Deserialize<JsonImageService>(responseString);

                this.ServiceDescription = jsonImageService.ServiceDescription.OrTake(jsonImageService.Description);
                this.CopyrightText = jsonImageService.CopyrightText;
                this.SpatialReferenceId = jsonImageService.SpatialReference?.EpsgCode ?? 0;

                if (String.IsNullOrEmpty(this.Name))
                {
                    this.Name = jsonImageService.Name.OrTake(this.ServiceShortname);
                }

                if (jsonImageService.TimeInfo is not null)
                {
                    layer.TimeInfo = new LayerTimeInfo(
                        jsonImageService.TimeInfo.TimeExtent,
                        Math.Max(1, jsonImageService.TimeInfo.TimeInterval.OrTake(1)),
                        jsonImageService.TimeInfo.TimeIntervalUnits.ToTimePeriod(TimePeriod.Years));
                }

                return true;
            }
            catch (Exception ex)
            {
                _initErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex, Const.InitServiceExceptionPreMessage);
                _errMsg = ex.Message + "\n" + ex.StackTrace;
                return false;
            }
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
        ServiceResponse ret = this.PreGetMap();

        if (ret != null)
        {
            return ret;
        }

        var httpService = requestContext.Http;

        using (var pLog = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this._service, "GetMap", "GetMap " + _server))
        {
            try
            {
                IDisplay display = _map.DisplayRotation != 0
                    ? Display.TransformedDisplay(_map)
                    : _map;
                
                var time = _map.GetTimeEpoch(this.Url)?.ToJavascriptEpochArray();

                string path = String.Format("exportImage?bbox={0},{1},{2},{3}&bboxSR={4}&size={5},{6}&imageSR={4}&format={7}&pixelType={8}&noData={9}&noDataInterpretation={10}&interpolation={11}&compressionQuality={12}&bandIds={13}&mosaicRule={14}&renderingRule={15}&time={16}&f=json",
                    display.Extent?.MinX.ToPlatformNumberString(),
                    display.Extent?.MinY.ToPlatformNumberString(),
                    display.Extent?.MaxX.ToPlatformNumberString(),
                    display.Extent?.MaxY.ToPlatformNumberString(),
                    _map.SpatialReference?.Id,
                    display.ImageWidth, display.ImageHeight,
                    _imageFormat.ToString(),
                    _pixelType.ToString(),
                    _nodata,
                    _nodataInterpretation.ToString(),
                    _interpolation.ToString(),
                    _compressQaulity,
                    _bandIDs,
                    HttpUtility.UrlEncode(this.MosaicRule),
                    HttpUtility.UrlEncode(this.RenderingRule),
                    time?.Any() == true ? string.Join(",", time) : String.Empty
                    );

                string url = $"{_serviceUrl}/{path}";
                var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

                string responseString = await authHandler.TryGetAsync(this, url);
                var jsonResult = JSerializer.Deserialize<JsonExportResponse>(responseString);

                if (!String.IsNullOrEmpty(jsonResult.Href))
                {
                    if (_map.DisplayRotation != 0)
                    {
                        using var imageData = new MemoryStream(await requestContext.Http.GetDataAsync(jsonResult.Href));
                        using var sourceImgage = Current.Engine.CreateBitmap(imageData);
                        using var sourceBitmap = Display.TransformImage(sourceImgage, display, _map);

                        string filename = $"rot_{Guid.NewGuid():N}.{_imageFormat}";
                        string filePath = _map.AsOutputFilename(filename);
                        string fileUrl = _map.AsOutputUrl(filename);
                        var imageFormat = _imageFormat.ToString().StartsWith("png", StringComparison.OrdinalIgnoreCase)
                                            ? gView.GraphicsEngine.ImageFormat.Png
                                            : gView.GraphicsEngine.ImageFormat.Jpeg;

                        await sourceBitmap.SaveOrUpload(filePath, imageFormat);

                        return new ImageLocation(_map.Services.IndexOf(this), this.ID, String.Empty, fileUrl);
                    }

                    return new ImageLocation(_map.Services.IndexOf(this), this.ID, String.Empty, jsonResult.Href);
                }

                return new ErrorResponse(_map.Services.IndexOf(this), this.ID, "Unknown Error", String.Empty);
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
        return Task.FromResult<ServiceResponse>(null);
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

    public bool ShowInToc { get; set; }

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

    #region IServiceDescription
    public string ServiceDescription { get; set; }
    public string CopyrightText { get; set; }
    #endregion

    #region IImageServiceType 

    public ImageServiceType ImageServiceType { get; set; }

    #endregion

    #region IClone Member

    public IMapService Clone(IMap parent)
    {
        ImageServerService clone = new ImageServerService();

        clone._map = parent ?? _map;

        clone._id = _id;
        clone._server = _server;
        clone._name = _name;
        clone._service = _service;
        clone._serviceUrl = _serviceUrl;
        clone.Timeout = Timeout;

        foreach (ILayer layer in _layers)
        {
            if (layer == null)
            {
                continue;
            }

            clone._layers.Add(layer.Clone(clone));
        }
        clone._isDirty = _isDirty;
        clone._opacity = _opacity;
        clone.OpacityFactor = this.OpacityFactor;
        clone.Username = this.Username;
        clone.Password = this.Password;
        clone.StaticToken = this.StaticToken;
        clone._minScale = _minScale;
        clone._maxScale = _maxScale;
        clone.ShowInToc = this.ShowInToc;

        //clone._tokenParam = _tokenParam;
        clone._tokenRequired = _tokenRequired;

        clone._checkSpatialConstraints = _checkSpatialConstraints;
        clone._collectionId = _collectionId;
        clone._url = _url;

        clone.TokenExpiration = this.TokenExpiration;
        clone.HttpCredentials = this.HttpCredentials;

        clone._imageFormat = _imageFormat;
        clone._pixelType = _pixelType;
        clone._nodata = _nodata;
        clone._nodataInterpretation = _nodataInterpretation;
        clone._interpolation = _interpolation;
        clone._compressQaulity = _compressQaulity;
        clone._bandIDs = _bandIDs;
        clone.MosaicRule = this.MosaicRule;
        clone.RenderingRule = this.RenderingRule;
        clone.RenderingRuleIdentify = this.RenderingRuleIdentify;
        clone.PixelAliasname = this.PixelAliasname;

        clone._isBasemap = _isBasemap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone._initErrorResponse = _initErrorResponse;
        clone._errMsg = _errMsg;

        clone._supportedCrs = _supportedCrs;
        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        clone.LayerProperties = this.LayerProperties;
        clone.ImageServiceType = this.ImageServiceType;

        clone.ShowServiceLegendInMap = this.ShowServiceLegendInMap;
        clone.LegendVisible = this.LegendVisible;
        clone.LegendOptMethod = this.LegendOptMethod;
        clone.LegendOptSymbolScale = this.LegendOptSymbolScale;
        clone.FixLegendUrl = this.FixLegendUrl;
        clone.CreatePresentationsDynamic = this.CreatePresentationsDynamic;
        clone.CreateQueriesDynamic = this.CreateQueriesDynamic;
        clone.ServiceDescription = this.ServiceDescription;
        clone.CopyrightText = this.CopyrightText;
        clone.SpatialReferenceId = this.SpatialReferenceId;

        clone._imageServiceName = _imageServiceName;

        clone.ServiceThemes = this.ServiceThemes;

        return clone;
    }

    #endregion

    #region IService2 Member

    public ServiceResponse PreGetMap()
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map.Services.IndexOf(this), this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        if (!ServiceHelper.VisibleInScale(this, _map))
        {
            return new EmptyImage(_map.Services.IndexOf(this), this.ID);
        }

        bool hasVisibleLayers = false;
        foreach (Layer layer in _layers)
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

    #region IServiceLegend

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

    bool _legendVisible = true;
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
        try
        {
            if (!_legendVisible)
            {
                return new ServiceResponse(this.Map.Services.IndexOf(this), this.ID);
            }
            if (!String.IsNullOrEmpty(FixLegendUrl))
            {
                return new ImageLocation(this.Map.Services.IndexOf(this),
                    this.ID, String.Empty, FixLegendUrl);
            }
            using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, _service, "GetLegend", ""))
            {
                var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();
                string requestUrl = $"{this._serviceUrl}/legend?bandids={_bandIDs}&renderingRule={HttpUtility.UrlEncode(RenderingRule)}&f=pjson";

                string jsonAnswer = await authHandler.TryGetAsync(this, requestUrl);

                return await this.RenderRestLegendResponse(requestContext, jsonAnswer, optimize: true);
            }
        }
        catch (Exception ex)
        {
            return new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex);
        }
    }

    public LegendOptimization LegendOptMethod
    {
        get { return LegendOptimization.None; }
        set { }
    }

    public double LegendOptSymbolScale
    {
        get { return 0D; }
        set { }
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

    #region IDymamicService

    public ServiceDynamicPresentations CreatePresentationsDynamic { get; set; }

    public ServiceDynamicQueries CreateQueriesDynamic { get; set; }

    #endregion

    #region IServiceInitialException

    public ErrorResponse InitialException => _initErrorResponse;

    #endregion

    #region Helper

    public string ImageServiceName(string url)
    {
        var p = url.Split('/');
        if (p.Length > 1 && p[p.Length - 1].ToLower() == "imageserver")
        {
            return p[p.Length - 2];
        }

        return p[p.Length - 1];
    }

    #endregion
}
