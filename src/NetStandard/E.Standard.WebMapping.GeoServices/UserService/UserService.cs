using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.OGC.Schema;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.UserService;

public class UserService : IUserService, IPersistable
{
    private IMapService _service = null;
    private string _tocName = String.Empty;
    private readonly string _appConfigPath = String.Empty;
    private string[] _layerIds = null;

    public UserService(IMapService service, string tocName, string appConfigPath)
    {
        _service = service;
        _tocName = tocName;
        _appConfigPath = appConfigPath;
        ShowInToc = true;
    }

    public string TocName
    {
        get { return _tocName; }
        set { _tocName = value; }
    }

    public IMapService MapService
    {
        get { return _service; }
    }

    public string[] LayerIds
    {
        get { return _layerIds; }
        set { _layerIds = value; }
    }

    #region IMapService Member

    public string Name
    {
        get
        {
            if (_service == null)
            {
                return String.Empty;
            }

            return _service.Name;
        }
        set
        {
            if (_service != null)
            {
                _service.Name = value;
            }
        }
    }

    public string Url
    {
        get
        {
            if (_service != null)
            {
                return _service.Url;
            }

            return String.Empty;
        }
        set
        {
            if (_service != null)
            {
                _service.Url = value;
            }
        }
    }
    public string Server
    {
        get
        {
            if (_service == null)
            {
                return String.Empty;
            }

            return _service.Server;
        }
    }

    public string Service
    {
        get
        {
            if (_service == null)
            {
                return String.Empty;
            }

            return _service.Service;
        }
    }

    public string ServiceShortname { get { return this.Service; } }

    public string ID
    {
        get
        {
            if (_service == null)
            {
                return String.Empty;
            }

            return _service.ID;
        }
    }

    public float Opacity
    {
        get
        {
            if (_service == null)
            {
                return 0f;
            }

            return _service.Opacity;
        }
        set
        {
            if (_service != null)
            {
                _service.Opacity = value;
            }
        }
    }
    public float OpacityFactor { get; set; } = 1f;

    public bool ShowInToc { get; set; }

    public bool CanBuffer
    {
        get
        {
            if (_service == null)
            {
                return false;
            }

            return _service.CanBuffer;
        }
    }

    public bool UseToc
    {
        get
        {
            if (_service == null)
            {
                return false;
            }

            return _service.UseToc;
        }
        set
        {
            if (_service != null)
            {
                _service.UseToc = value;
            }
        }
    }

    public LayerCollection Layers
    {
        get
        {
            if (_service == null)
            {
                return null;
            }

            return _service.Layers;
        }
    }

    public Envelope InitialExtent
    {
        get
        {
            if (_service == null)
            {
                return null;
            }

            return _service.InitialExtent;
        }
    }

    public ServiceResponseType ResponseType
    {
        get
        {
            if (_service == null)
            {
                return ServiceResponseType.Image;
            }

            return _service.ResponseType;
        }
    }

    public ServiceDiagnostic Diagnostics
    {
        get
        {
            if (_service == null)
            {
                return null;
            }

            return _service.Diagnostics;
        }
    }

    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel
    {
        get
        {
            if (_service == null)
            {
                return ServiceDiagnosticsWarningLevel.Never;
            }

            return _service.DiagnosticsWaringLevel;
        }
        set
        {
            if (_service != null)
            {
                _service.DiagnosticsWaringLevel = value;
            }
        }
    }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        if (_service != null)
        {
            return _service.PreInit(serviceID, server, url, authUser, authPwd, token, appConfigPath, serviceThemes);
        }

        return false;
    }

    async public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        if (_service != null)
        {
            return await _service.InitAsync(map, requestContext);
        }

        return false;
    }

    public bool IsDirty
    {
        get
        {
            if (_service == null)
            {
                return false;
            }

            return _service.IsDirty;
        }
        set
        {
            if (_service != null)
            {
                _service.IsDirty = value;
            }
        }
    }

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        if (_service == null)
        {
            return new ExceptionResponse(0, this.ID, new Exception("Service nicht vorhanden!"));
        }

        return await _service.GetMapAsync(requestContext);
    }

    async public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        if (_service == null)
        {
            return new ExceptionResponse(0, this.ID, new Exception("Service nicht vorhanden!"));
        }

        return await _service.GetSelectionAsync(collection, requestContext);
    }

    public int Timeout
    {
        get
        {
            if (_service == null)
            {
                return 0;
            }

            return _service.Timeout;
        }
        set
        {
            if (_service != null)
            {
                _service.Timeout = value;
            }
        }
    }

    public IMap Map
    {
        get
        {
            if (_service == null)
            {
                return null;
            }

            return _service.Map;
        }
    }

    public double MinScale
    {
        get
        {
            if (_service == null)
            {
                return 0.0;
            }

            return _service.MinScale;
        }
        set
        {
            if (_service != null)
            {
                _service.MinScale = value;
            }
        }
    }

    public double MaxScale
    {
        get
        {
            if (_service == null)
            {
                return 0.0;
            }

            return _service.MaxScale;
        }
        set
        {
            if (_service != null)
            {
                _service.MaxScale = value;
            }
        }
    }

    public string CollectionId
    {
        get
        {
            if (_service != null)
            {
                return _service.CollectionId;
            }

            return String.Empty;
        }
        set
        {
            if (_service != null)
            {
                _service.CollectionId = value;
            }
        }
    }

    public bool CheckSpatialConstraints
    {
        get
        {
            if (_service != null)
            {
                return _service.CheckSpatialConstraints;
            }

            return false;
        }
        set
        {
            if (_service != null)
            {
                _service.CheckSpatialConstraints = value;
            }
        }
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
        IMapService serviceClone = (_service != null)
            ? _service.Clone(parent)
            : null;

        UserService clone = new UserService(serviceClone, _tocName, _appConfigPath);

        if (_layerIds != null)
        {
            clone.LayerIds = new List<string>(_layerIds).ToArray();
        }

        clone._isBasemap = _isBasemap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone._supportedCrs = _supportedCrs;
        clone.ShowInToc = this.ShowInToc;

        return clone;
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _tocName = (string)stream.Load("tocname", "Dienst 1");
        _layerIds = WebGIS.CMS.Globals.StringToList((string)stream.Load("layerids", String.Empty));

        switch ((string)stream.Load("type", String.Empty))
        {
            case "E.Standard.WebMapping.Service.AXL.Service":
                _service = new AXL.AxlService();
                break;
            case "E.Standard.WebMapping.Service.ArcServer.Rest.Service":
                _service = new ArcServer.Rest.MapService();
                break;
            case "E.Standard.WebMapping.Service.OGC.WMS.Service":
                _service = new OGC.WMS.WmsService(
                    (WMS_Version)stream.Load("wms_version", (int)WMS_Version.version_1_1_1),
                    (string)stream.Load("wms_imageformat", "image/png"),
                    (string)stream.Load("wms_getfeatureformat", "text/html"),
                    (WMS_LayerOrder)stream.Load("layerorder", (int)WMS_LayerOrder.Up),
                    (WMS_Vendor)stream.Load("vendor", (int)WMS_Vendor.Unknown));
                break;
        }
        if (_service == null)
        {
            return;
        }

        _service.PreInit((string)stream.Load("id", String.Empty),
                         (string)stream.Load("server", String.Empty),
                         (string)stream.Load("service", String.Empty),
                         String.Empty, String.Empty, String.Empty,
                         _appConfigPath, null);

    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("tocname", _tocName);
        stream.Save("type", _service.GetType().ToString());


        stream.Save("id", this.ID);
        stream.Save("server", this.Server);
        stream.Save("service", this.Service);
        stream.Save("timeout", this.Timeout);

        stream.Save("layerids", WebGIS.CMS.Globals.ListToString(_layerIds));

        if (_service is OGC.WMS.WmsService)
        {
            stream.Save("wms_version", (int)((OGC.WMS.WmsService)_service).WMSVersion);
            stream.Save("wms_imageformat", ((OGC.WMS.WmsService)_service).GetMapFormat);
            stream.Save("wms_getfeatureformat", ((OGC.WMS.WmsService)_service).GetFeatureInfoFormat);
        }
    }

    #endregion
}
