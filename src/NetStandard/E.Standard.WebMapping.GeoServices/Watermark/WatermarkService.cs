using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using gView.GraphicsEngine;
using System;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Watermark;

public class WatermarkService : IMapService, IPrintableMapService
{
    private bool _isDirty = true;
    private IMap _map = null;
    private readonly string _watermarkUrl = String.Empty, _watermarkPath = String.Empty;
    private int _prevImageWidth = -1, _prevImageHeight = -1;
    private float _opacity = 0.15f;

    public WatermarkService(string watermarkUrl, string watermarkPath)
    {
        _watermarkUrl = watermarkUrl;
        _watermarkPath = watermarkPath;
        ShowInToc = true;
    }

    #region IService Member

    public string Name
    {
        get
        {
            return "WatermarkService";
        }
        set
        {

        }
    }

    public string Url
    {
        get { return String.Empty; }
        set { }
    }

    public string Server
    {
        get { return String.Empty; }
    }

    public string Service
    {
        get { return "webtis-int-watermark"; }
    }

    public string ServiceShortname { get { return this.Service; } }

    public string ID
    {
        get { return "_watermark"; }
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
        get { return null; }
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Html; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
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
        if (_map == null)
        {
            return Task.FromResult<ServiceResponse>(new ExceptionResponse(-1, this.ID, new Exception("Map==NULL")));
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            bool setHtml = false;
            if (_prevImageWidth != _map.ImageWidth ||
                _prevImageHeight != _map.ImageHeight)
            {
                _prevImageWidth = _map.ImageWidth;
                _prevImageHeight = _map.ImageHeight;
                setHtml = true;

                int stepsX = this.Map.ImageWidth / 512;
                int stepsY = this.Map.ImageHeight / 256;

                int tileSizeX = Math.Min(512, this.Map.ImageWidth);
                int tileSizeY = Math.Min(256, this.Map.ImageHeight);

                Random r = new Random();
                for (int y = 0; y < _map.ImageHeight; y += (stepsY > 0 ? _map.ImageHeight / stepsY : _map.ImageHeight))
                {
                    for (int x = 0; x < _map.ImageWidth; x += (stepsX > 0 ? _map.ImageWidth / stepsX : _map.ImageWidth))
                    {
                        int left = x + tileSizeX / 2 - tileSizeX / 4 + r.Next(tileSizeX / 2),
                            top = y + tileSizeY / 2 - tileSizeY / 4 + r.Next(tileSizeY / 2);

                        sb.Append($"<img src=\"{_watermarkUrl}\" style=\"position:absolute;left:{left}px;top:{top}px\" />");
                    }
                }
            }

            HtmlResponse resp = new HtmlResponse(_map.Services.IndexOf(this), this.ID, sb.ToString());
            resp.SetHtml = setHtml;
            return Task.FromResult<ServiceResponse>(resp);
        }
        catch (Exception ex)
        {
            return Task.FromResult<ServiceResponse>(new ExceptionResponse(_map.Services.IndexOf(this), this.ID, ex));
        }
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(null);
    }

    public int Timeout
    {
        get
        {
            return 20;
        }
        set
        {

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
            return 0.0;
        }
        set
        {

        }
    }

    public double MaxScale
    {
        get
        {
            return 0.0;
        }
        set
        {

        }
    }

    public string CollectionId
    {
        get
        {
            return String.Empty;
        }
        set
        {
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

    public int[] SupportedCrs
    {
        get { return null; }
        set { }
    }
    #endregion

    #region IClone Member

    public IMapService Clone(IMap parent)
    {
        if (parent is null)
        {
            return null;
        }

        WatermarkService clone = new WatermarkService(_watermarkUrl, _watermarkPath);

        clone._map = parent;
        clone._isDirty = _isDirty;
        clone._opacity = _opacity;
        clone.ShowInToc = this.ShowInToc;

        clone._isBasemap = _isBasemap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        return clone;
    }

    #endregion

    #region IPrintableService Member

    async public Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext)
    {
        if (_map == null)
        {
            return new ExceptionResponse(-1, this.ID, new Exception("Map==NULL"));
        }

        var httpService = requestContext.Http;
        string extraMessage = String.Empty;

        try
        {
            using (var watermark = await _watermarkPath.ImageFromUri(httpService))
            using (var bitmap = Current.Engine.CreateBitmap(_map.ImageWidth, _map.ImageHeight))
            using (var canvas = bitmap.CreateCanvas())
            {
                Random r = new Random(DateTime.Now.Millisecond);
                for (int y = 0; y < bitmap.Height; y += bitmap.Height / 3)
                {
                    for (int x = 0; x < bitmap.Width; x += bitmap.Width / 3)
                    {
                        canvas.DrawBitmap(watermark,
                            new CanvasRectangle(x + r.Next(100) - 50, y + r.Next(100) - 50, watermark.Width, watermark.Height),
                            new CanvasRectangle(0, 0, watermark.Width, watermark.Height));
                    }
                }

                string filename = "wm_" + Guid.NewGuid().ToString("N") + ".png";
                string outputPath = (string)_map.Environment.UserValue(webgisConst.OutputPath, String.Empty);
                string outputUrl = (string)_map.Environment.UserValue(webgisConst.OutputUrl, String.Empty);

                bitmap.Save(extraMessage = $"{outputPath}/{filename}", ImageFormat.Png);
                return new ImageLocation(
                    _map.Services.IndexOf(this), this.ID,
                    $"{outputPath}/{filename}",
                    $"{outputUrl}/{filename}");
            }
        }
        catch (Exception ex)
        {
            return new ExceptionResponse(_map.Services.IndexOf(this), this.ID, new Exception(ex.Message + " - " + extraMessage));
        }
    }

    #endregion
}
