using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using E.Standard.WebMapping.GeoServices.Graphics.Renderer;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Graphics;

public class GraphicsService : IGraphicsService
{
    private IMap _map;
    private string _name = "Graphics";
    private float _opacity = 1.0f;
    private bool _isDirty = true;
    private int _timeout = 20;

    public GraphicsService()
    {
        this.ShowInToc = true;
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
        get { return String.Empty; }
    }

    public string Service
    {
        get { return "webgis-int-graphics"; }
    }

    public string ServiceShortname { get { return this.Service; } }

    public string ID
    {
        get { return "graphics"; }
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
        get { return true; }
        set { }
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
        get { return ServiceResponseType.Image; }
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

        IBitmap bm = null;
        try
        {
            // Seit 4337
            var displayExtent = _map is Display ? Display.TransformedBounds((Display)_map) : _map.Extent;

            if (displayExtent != null && displayExtent.Width > 0 && displayExtent.Height > 0)
            {
                if (_map.GraphicsContainer.Count > 0)
                {
                    if (bm == null)
                    {
                        bm = Current.Engine.CreateBitmap(_map.ImageWidth, _map.ImageHeight, PixelFormat.Rgba32);
                    }

                    using (var gr = bm.CreateCanvas())
                    {
                        gr.SmoothingMode = SmoothingMode.AntiAlias;
                        gr.TextRenderingHint = TextRenderingHint.AntiAlias;

                        var grElements = _map.GraphicsContainer.Where(e => e != null && !(e is LabelElement));
                        var labels = _map.GraphicsContainer.Where(e => (e is LabelElement));

                        foreach (IGraphicElement grElement in grElements)
                        {
                            grElement.Draw(gr, _map);
                        }
                        foreach (IGraphicElement label in labels)
                        {
                            label.Draw(gr, _map);
                        }
                    }
                }
                foreach (Selection selection in _map.Selection)
                {
                    if (selection != null &&
                        selection.DrawSpatialFilter == true &&
                        selection.Filter is SpatialFilter &&
                        ((SpatialFilter)selection.Filter).QueryShape != null)
                    {
                        if (bm == null)
                        {
                            bm = Current.Engine.CreateBitmap(_map.ImageWidth, _map.ImageHeight, PixelFormat.Rgba32);
                        }

                        GeometryRenderer renderer = new GeometryRenderer(_map);
                        renderer.Bitmap = bm;
                        renderer.Dpi = _map.Dpi;
                        //renderer.setImageRect(_map.Extent);
                        renderer.Geometry.Add(((SpatialFilter)selection.Filter).QueryShape);
                        using (var brush = Current.Engine.CreateHatchBrush(HatchStyle.BackwardDiagonal, ArgbColor.Gray, ArgbColor.Transparent))
                        using (var pen = Current.Engine.CreatePen(ArgbColor.Cyan, 1f))
                        {
                            renderer.Brush = brush;
                            renderer.Pen = pen;
                            renderer.Renderer();
                        }
                    }
                }
            }

            if (bm != null)
            {
                string outputPath = (string)_map.Environment.UserValue(webgisConst.OutputPath, String.Empty);
                string outputUrl = (string)_map.Environment.UserValue(webgisConst.OutputUrl, String.Empty);
                string title = "gr_" + Guid.NewGuid().ToString("N") + ".png";

                try
                {
                    bm.SaveOrUpload($"{outputPath}/{title}", ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    return Task.FromResult<ServiceResponse>(new ExceptionResponse(_map.Services.IndexOf(this), this.ID, ex));
                }

                return Task.FromResult<ServiceResponse>((new ImageLocation(_map.Services.IndexOf(this), this.ID,
                    $"{outputPath}/{title}",
                    $"{outputUrl}/{title}")));
            }
            else
            {
                return Task.FromResult<ServiceResponse>(new EmptyImage(_map.Services.IndexOf(this), this.ID));
            }
        }
        finally
        {
            if (bm != null)
            {
                bm.Dispose();
                bm = null;
            }
        }
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(
            new ErrorResponse(_map.Services.IndexOf(this), this.ID, "GetSelection is not Available for Graphics", "GetSelection is not Available for Graphics"));
    }

    public int Timeout
    {
        get { return _timeout; }
        set { _timeout = value; }
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
        if (parent is null)
        {
            return null;
        }

        GraphicsService clone = new GraphicsService();
        clone._map = parent;
        clone._name = _name;
        clone._opacity = _opacity;
        clone.OpacityFactor = this.OpacityFactor;
        clone.ShowInToc = this.ShowInToc;
        clone._isDirty = _isDirty;
        clone._supportedCrs = _supportedCrs;
        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        return clone;
    }

    #endregion
}
