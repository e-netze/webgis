using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ExceptionHandling;

public class ExceptionService : IMapService
{
    private string _name = String.Empty;
    private string _server = String.Empty, _service = String.Empty, _id = String.Empty;
    private bool _isDirty = true;
    private readonly System.Exception _exception = null;
    private IMap _map = null;
    private ServiceTheme[] _serviceThemes = null;

    public ExceptionService(System.Exception ex)
    {
        _exception = ex;
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
        get { return String.Empty; }
        set { }
    }

    public string Server
    {
        get { return _server; }
    }

    public string Service
    {
        get { return _service; }
    }

    public string ServiceShortname { get { return this.Service; } }

    public string ID
    {
        get { return _id; }
    }

    public float Opacity
    {
        get
        {
            return 1.0f;
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
        get { return new LayerCollection(this); }
    }

    public Envelope InitialExtent
    {
        get { return new Envelope(-100, -100, 100, 100); }
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
        _service = url;
        _serviceThemes = serviceThemes;

        return true;
    }

    public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        _map = map;
        return Task.FromResult(true);
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

    public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(new ExceptionResponse(_map.Services.IndexOf(this), ID, _exception));
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(new ExceptionResponse(_map.Services.IndexOf(this), ID, _exception));
    }

    public int Timeout
    {
        get
        {
            return 10;
        }
        set
        {

        }
    }

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

    public bool CheckSpatialConstraints
    {
        get { return false; }
        set { }
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

    #region IClone Member

    public IMapService Clone(IMap parent)
    {
        ExceptionService service = new ExceptionService(_exception);
        service.PreInit(ID, this.Server, this.Service, String.Empty, String.Empty, String.Empty, String.Empty, _serviceThemes);

        service._isBasemap = _isBasemap;
        service._basemapType = _basemapType;
        service.BasemapPreviewImage = this.BasemapPreviewImage;

        service._supportedCrs = _supportedCrs;
        service.Diagnostics = this.Diagnostics;
        service.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        return service;
    }

    #endregion
}
