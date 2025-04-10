using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Tiling.Models;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Tiling;

public abstract class TileService : IMapService, IPrintableService, IServiceLegend, IServiceSupportedCrs, IExportableOgcService, IServiceCopyrightInfo, IServiceInitialException
{
    private IMap _map;
    private string _name = String.Empty, _logName = String.Empty;
    private string _id = String.Empty;
    private string _server = String.Empty;
    private string _service = String.Empty;
    private LayerCollection _layers;
    private float _opacity = 1.0f;
    private int _timeout = 20;
    private bool _useToc = true;
    private bool _isDirty = false;
    private TileGrid _grid = null;
    private int _rendering = -1;
    private string _tilePath = String.Empty;
    private int[] _projIds = null;
    protected ErrorResponse _initErrorResponse = null, _diagnosticsErrorResponse = null;

    public TileService(bool hideBeyondMaxLevel)
    {
        _layers = new LayerCollection(this);
        ShowInToc = true;

        HideBeyondMaxLevel = hideBeyondMaxLevel;
    }

    public TileGrid TileGrid
    {
        get { return _grid; }
        set { _grid = value; }
    }

    public TileGridRendering GridRendering
    {
        get
        {
            if (_grid != null)
            {
                return _grid.GridRendering;
            }

            return _rendering == -1 ? TileGridRendering.Quality : (TileGridRendering)_rendering;
        }
        set
        {
            if (_grid != null)
            {
                _grid.GridRendering = value;
                _rendering = -1;
            }
            else
            {
                _rendering = (int)value;
            }
        }
    }

    public string TileUrl(IRequestContext requestContext, int level, int row, int col, ServiceRestirctions serviceRestrictions = null)
    {
        if (serviceRestrictions != null)
        {
            var resolution = _grid.GetLevelResolution(level);
            Point tilePoint = _grid.TileUpperLeft(row, col, resolution);
            Envelope tileEnvelope = new Envelope(tilePoint.X, tilePoint.Y,
                tilePoint.X + _grid.TileWidth(resolution), tilePoint.Y + _grid.TileHeight(resolution) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));

            if (!serviceRestrictions.EnvelopeInBounds(tileEnvelope))
            {
                return String.Empty;
            }
        }

        string tileUrl = this.ImageUrl(requestContext, this.Map);

        string tUrl = tileUrl.Replace("[LEVEL]", level.ToString()).Replace("[ROW]", row.ToString()).Replace("[COL]", col.ToString());
        tUrl = tUrl.Replace("[LEVEL_PAD2]", level.ToString().PadLeft(2, '0'));
        tUrl = tUrl.Replace("[ROW_DIV3_PAD3]", (row / 1000000).ToString().PadLeft(3, '0') + "/" +
                                              ((row / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                              ((row % 1000).ToString().PadLeft(3, '0')));
        tUrl = tUrl.Replace("[COL_DIV3_PAD3]", (col / 1000000).ToString().PadLeft(3, '0') + "/" +
                                              ((col / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                              ((col % 1000).ToString().PadLeft(3, '0')));
        tUrl = tUrl.Replace("[ROW_HEX_PAD8]", row.ToString("x").PadLeft(8, '0'));
        tUrl = tUrl.Replace("[COL_HEX_PAD8]", col.ToString("x").PadLeft(8, '0'));

        return tUrl;
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
            _logName = _name.Replace(" ", "_");
        }
    }

    string _url = String.Empty;
    public string Url
    {
        get { return _url; }
        set { _url = value; }
    }

    public string Server
    {
        get { return _server; }
        set { _server = value; }
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
        get { return null; }
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Html; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    virtual public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        _id = serviceID;
        _server = server;
        _service = url;

        return true;
    }

    abstract public Task<bool> InitAsync(IMap map, IRequestContext requestContext);

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
        throw new Exception("The method or operation is not implemented.");
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        throw new Exception("The method or operation is not implemented.");
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
        protected set { _map = value; }
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

    public bool HideBeyondMaxLevel { get; set; }

    #endregion

    #region IClone Member

    abstract public IMapService Clone(IMap parent);

    protected virtual void Clone(TileService clone, IMap parent)
    {
        clone._map = parent ?? _map;

        clone._name = _name;
        clone._logName = _logName;
        clone._id = _id;
        clone._server = _server;
        clone._service = _service;

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
        clone.ShowInToc = this.ShowInToc;

        clone._minScale = _minScale;
        clone._maxScale = _maxScale;

        clone._grid = _grid;

        clone._checkSpatialConstraints = _checkSpatialConstraints;
        clone._collectionId = _collectionId;
        clone._url = _url;

        clone._rendering = _rendering;
        clone._tilePath = _tilePath;

        clone._legendVisible = _legendVisible;
        clone._showServiceLegendInMap = _showServiceLegendInMap;
        clone._fixLegendUrl = _fixLegendUrl;

        clone._isBasemap = _isBasemap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone._projIds = _projIds;

        clone._exportWms = _exportWms;
        clone._ogcEnvelope = _ogcEnvelope != null ? new Envelope(_ogcEnvelope) : null;

        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;
        clone.CopyrightInfoId = this.CopyrightInfoId;

        clone._initErrorResponse = _initErrorResponse;
        clone._diagnosticsErrorResponse = _diagnosticsErrorResponse;
    }

    #endregion

    // private double _lastMapResolution = double.MaxValue;
    //public void ForceRefresh()
    //{
    //    _lastMapResolution = 0.0;
    //}

    abstract public string ImageUrl(IRequestContext requestContext, IMap map);

    abstract public string[] ImageUrls(IRequestContext requestContext, IMap map);

    #region ImageUrlPro

    abstract public (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map);

    protected (string tileUrl, string[] domains) CreateImageUrlTemplate(string[] imageUrls)
    {
        if (imageUrls.Length == 1)
        {
            return (imageUrls[0], new string[0]);
        }

        string uniqueTileUrl = null; ;
        List<string> domains = new List<string>();
        foreach (var imageUrl in imageUrls)
        {
            var uri = new Uri(imageUrl);
            var host = uri.Host;

            if (domains.Contains(host))
            {
                continue;
            }
            domains.Add(host);

            var scheme = (imageUrl.StartsWith("//")) ? "//" : $"{uri.Scheme}://";

            string tileUrl = $"{scheme}{{s}}{uri.PathAndQuery}";
            if (uniqueTileUrl == null)
            {
                uniqueTileUrl = tileUrl;
            }
            else
            {
                if (uniqueTileUrl != tileUrl)
                {
                    return (imageUrls[0], new string[0]);
                }
            }
        }

        return (uniqueTileUrl, domains.ToArray());
    }

    #endregion

    public string TilePath
    {
        get { return _tilePath; }
        set { _tilePath = value; }
    }

    #region IPrintableService Member

    async public Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext)
    {
        try
        {
            var httpService = requestContext.Http;
            bool visible = true;

            if (!ServiceHelper.VisibleInScale(this, _map))
            {
                visible = false;
            }
            else if (_layers.Count == 1 && _layers[0] != null)
            {
                ILayer layer = _layers[0];
                visible = layer.Visible;
            }
            if (visible == false)
            {
                return new EmptyImage(_map.Services.IndexOf(this), this.ID);
            }

            string tileUrl = this.ImageUrl(requestContext, this.Map);

            int level = _grid.GetBestLevel(SphericHelper.CalcTileResolution(Map), Map.Dpi);
            double iW, iH, res;
            int col0, row0;
            Point tilePoint, iTilePoint;

            GeometricTransformerPro geoTransform = null;
            try
            {
                //Console.WriteLine($"TileService.GetPrintMapAsync: SupportedCrs - { (this.SupportedCrs != null && this.SupportedCrs.Length > 0 ? this.SupportedCrs[0].ToString() : "null") }");
                if (this.SupportedCrs != null && this.SupportedCrs.Length > 0)
                {
                    if (_map.SpatialReference != null && _map.SpatialReference.Id != this.SupportedCrs[0])
                    {
                        var from = _map.SpatialReference;
                        var to = CoreApiGlobals.SRefStore.SpatialReferences.ById(this.SupportedCrs[0]);

                        var transformation = _map.Environment.UserString(webgisConst.Transformation + "-" + this.Url + "-" + _map.SpatialReference.Id);
                        if (String.IsNullOrEmpty(transformation) && this.Url.Contains("@"))
                        {
                            transformation = _map.Environment.UserString(webgisConst.Transformation + "-" + this.Url.Split('@')[0] + "-" + _map.SpatialReference.Id);
                        }

                        if (!String.IsNullOrWhiteSpace(transformation))
                        {
                            to.ReplaceOrInsertProj4TransformationParameters(transformation.Split(' '));
                        }

                        geoTransform = new GeometricTransformerPro(from, to);

                        //Console.WriteLine($"TileService.GetPrintMapAsync: Tramsform from { from.Id } to { to.Id }");
                    }
                }

                Display display = new Display();
                display.Extent.Set(_map.Extent.MinX, _map.Extent.MinY, _map.Extent.MaxX, _map.Extent.MaxY);
                display.ImageWidth = _map.ImageWidth;
                display.ImageHeight = _map.ImageHeight;
                display.Dpi = _map.Dpi;
                Envelope extent = Display.TransformedBounds(_map as Display);

                Point extentUpperLeft = new Point(extent.MinX, extent.MaxY);
                Point extentLowerRight = new Point(extent.MaxX, extent.MinY);

                if (geoTransform != null)
                {
                    geoTransform.Transform(extentUpperLeft);
                    geoTransform.Transform(extentLowerRight);
                }

                while (true)
                {
                    res = _grid.GetLevelResolution(level);
                    col0 = _grid.TileColumn(extentUpperLeft.X, res);
                    row0 = _grid.TileRow(extentUpperLeft.Y, res);

                    tilePoint = _grid.TileUpperLeft(row0, col0, res);
                    iTilePoint = display.WorldToImage(new Point(tilePoint));
                    Point iTilePoint_w = display.WorldToImage(new Point(tilePoint.X + _grid.TileWidth(res), tilePoint.Y));
                    Point iTilePoint_h = display.WorldToImage(new Point(tilePoint.X, tilePoint.Y + _grid.TileHeight(res)));
                    iW = iTilePoint.Distance(iTilePoint_w);
                    iH = iTilePoint.Distance(iTilePoint_h);

                    if ((iW > 100 && iH > 100))
                    {
                        break;
                    }

                    level = _grid.GetNextLowerLevel(level);
                    if (level <= 0)
                    {
                        level = 0;
                        break;
                    }
                }

                int col1 = _grid.TileColumn(extentLowerRight.X, res);
                int row1 = _grid.TileRow(extentLowerRight.Y, res);

                int col_s = Math.Min(col0, col1), col_e = Math.Max(col0, col1);
                int row_s = Math.Min(row0, row1), row_e = Math.Max(row0, row1);

                var imageFormat = ImageFormat.Png;

                if (tileUrl.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                    tileUrl.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase))
                {
                    imageFormat = ImageFormat.Jpeg;
                }

                string filetitle = "tile_" + System.Guid.NewGuid().ToString("N").ToLower() + (imageFormat == ImageFormat.Jpeg ? ".jpg" : ".png");
                string filename = (string)_map.Environment.UserValue(webgisConst.OutputPath, String.Empty) + @"/" + filetitle;
                string fileurl = (string)_map.Environment.UserValue(webgisConst.OutputUrl, String.Empty) + "/" + filetitle;

                double displayRot = _map.DisplayRotation;

                var serviceRestrictions = _map.GetServivceRestrictions(this);

                var tileDatas = new List<TileData>();

                using (var bitmap = Current.Engine.CreateBitmap(_map.ImageWidth, _map.ImageHeight, PixelFormat.Rgba32))
                using (var canvas = bitmap.CreateCanvas())
                using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
                {
                    if (imageFormat == ImageFormat.Jpeg)
                    {
                        canvas.FillRectangle(whiteBrush, new CanvasRectangle(0, 0, bitmap.Width, bitmap.Height));
                    }

                    canvas.InterpolationMode = InterpolationMode.Bicubic;
                    if (String.IsNullOrEmpty(_tilePath))
                    {
                        for (int row = row_s; row <= row_e; row++)
                        {
                            for (int col = col_s; col <= col_e; col++)
                            {
                                try
                                {
                                    if (serviceRestrictions != null)
                                    {
                                        var tilePoint2 = _grid.TileUpperLeft(row, col, res);
                                        Envelope tileEnvelope = new Envelope(tilePoint2.X, tilePoint2.Y,
                                            tilePoint2.X + _grid.TileWidth(res), tilePoint2.Y + _grid.TileHeight(res) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));

                                        if (geoTransform != null)
                                        {
                                            geoTransform.InvTransform(tileEnvelope);
                                        }

                                        if (!serviceRestrictions.EnvelopeInBounds(tileEnvelope))
                                        {
                                            continue;
                                        }
                                    }


                                    string tUrl = tileUrl.Replace("[LEVEL]", level.ToString()).Replace("[ROW]", row.ToString()).Replace("[COL]", col.ToString());
                                    tUrl = tUrl.Replace("[LEVEL_PAD2]", level.ToString().PadLeft(2, '0'));
                                    tUrl = tUrl.Replace("[ROW_DIV3_PAD3]", (row / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                                          ((row / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                                          ((row % 1000).ToString().PadLeft(3, '0')));
                                    tUrl = tUrl.Replace("[COL_DIV3_PAD3]", (col / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                                          ((col / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                                          ((col % 1000).ToString().PadLeft(3, '0')));
                                    tUrl = tUrl.Replace("[ROW_HEX_PAD8]", row.ToString("x").PadLeft(8, '0'));
                                    tUrl = tUrl.Replace("[COL_HEX_PAD8]", col.ToString("x").PadLeft(8, '0'));

                                    tileDatas.Add(new TileData()
                                    {
                                        Url = tUrl,
                                        Row = row,
                                        Col = col
                                    });
                                }
                                catch { }
                            }
                        }

                        #region Tasks

                        int MAX_PARALLEL_TASKS = 10;
                        List<Task<TileData>> tasks = new List<Task<TileData>>();
                        for (int td = 0; td < Math.Min(MAX_PARALLEL_TASKS, tileDatas.Count); td++)
                        {
                            tasks.Add(DownloadTile(requestContext, tileDatas[td]));
                        }

                        int taskIndex = 0, tileDataPos = tasks.Count;
                        while (taskIndex < tileDatas.Count)
                        {
                            try
                            {
                                var task = tasks[Task.WaitAny(tasks.ToArray())];
                                tasks.Remove(task);

                                var tileData = task.Result;
                                DrawTile(canvas, geoTransform, col0, row0, iTilePoint, tilePoint, iW, iH, res, displayRot, tileData);

                                if (tileDataPos < tileDatas.Count)
                                {
                                    tasks.Add(DownloadTile(requestContext, tileDatas[tileDataPos++]));
                                }
                            }
                            finally
                            {
                                taskIndex++;
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        //using (IDisposable iContext = _map.MapSession.MapApplication.Impersonator?.ImpersonateContext(true))
                        {
                            for (int row = row_s; row <= row_e; row++)
                            {
                                for (int col = col_s; col <= col_e; col++)
                                {
                                    try
                                    {
                                        string tPath = _tilePath.Replace("[LEVEL]", level.ToString()).Replace("[ROW]", row.ToString()).Replace("[COL]", col.ToString());
                                        tPath = tPath.Replace("[LEVEL_PAD2]", level.ToString().PadLeft(2, '0'));
                                        tPath = tPath.Replace("[ROW_DIV3_PAD3]", (row / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                                              ((row / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                                              ((row % 1000).ToString().PadLeft(3, '0')));
                                        tPath = tPath.Replace("[COL_DIV3_PAD3]", (col / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                                              ((col / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                                              ((col % 1000).ToString().PadLeft(3, '0')));
                                        tPath = tPath.Replace("[ROW_HEX_PAD8]", row.ToString("x").PadLeft(8, '0'));
                                        tPath = tPath.Replace("[COL_HEX_PAD8]", col.ToString("x").PadLeft(8, '0'));

                                        using (var tile = Current.Engine.CreateBitmap(tPath))
                                        {
                                            if (displayRot != 0.0 || geoTransform != null)
                                            {
                                                #region neue Methode (mit drehen)

                                                Point p1 = new Point(tilePoint.X + (col - col0) * _grid.TileWidth(res),
                                                                     tilePoint.Y + (row - row0 - (_grid.Orientation == TileGridOrientation.UpperLeft ? 0 : 1)) *
                                                                          _grid.TileHeight(res) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));
                                                Point p3 = new Point(p1.X, p1.Y + _grid.TileHeight(res) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));
                                                if (_grid.Orientation == TileGridOrientation.LowerLeft)
                                                {
                                                    Point hp = new Point(p1);
                                                    p1 = new Point(p3);
                                                    p3 = new Point(hp);
                                                }
                                                Point p2 = new Point(p1.X + _grid.TileWidth(res), Math.Max(p1.Y, p3.Y));

                                                if (geoTransform != null)
                                                {
                                                    geoTransform.InvTransform(p1);
                                                    geoTransform.InvTransform(p2);
                                                    geoTransform.InvTransform(p3);
                                                }

                                                p1 = _map.WorldToImage(p1);
                                                p2 = _map.WorldToImage(p2);
                                                p3 = _map.WorldToImage(p3);

                                                CanvasPointF[] points = new CanvasPointF[]{
                                                    new CanvasPointF((float)p1.X,(float)p1.Y),
                                                    new CanvasPointF((float)p2.X,(float)p2.Y),
                                                    new CanvasPointF((float)p3.X,(float)p3.Y),
                                                };

                                                canvas.DrawBitmap(tile, points,
                                                    new CanvasRectangleF(0f, 0f, tile.Width, tile.Height));

                                                #endregion
                                            }
                                            else
                                            {
                                                #region alte Methode

                                                double x0 = iTilePoint.X + (col - col0) * iW;
                                                double y0 = iTilePoint.Y + (row - row0) * iH * (_grid.Orientation == TileGridOrientation.UpperLeft ? 1.0 : -1.0);

                                                canvas.DrawBitmap(tile,
                                                    new CanvasRectangleF((float)x0, (float)y0, (float)(iW + 0.5), (float)(iH + 0.5)),
                                                    new CanvasRectangleF(0f, 0f, tile.Width, tile.Height));

                                                #endregion
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    await bitmap.SaveOrUpload(filename, imageFormat);
                }

                return new ImageLocation(_map.Services.IndexOf(this), this.ID, filename, fileurl);
            }
            finally
            {
                if (geoTransform != null)
                {
                    geoTransform.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            if (_map == null)
            {
                return new ExceptionResponse(0, this.ID, ex);
            }

            return new ExceptionResponse(_map.Services.IndexOf(this), this.ID, ex);
        }
    }

    virtual internal async Task<TileData> DownloadTile(IRequestContext requestContext, TileData tileData)
    {
        try
        {
            tileData.Data = await requestContext.Http.GetDataAsync(tileData.Url);
        }
        catch (Exception)
        {
        }

        return tileData;
    }

    private void DrawTile(ICanvas canvas,
                          GeometricTransformerPro geoTransform,
                          int col0, int row0,
                          Point iTilePoint, Point tilePoint,
                          double iW, double iH, double res, double displayRot,
                          TileData tileData)
    {
        try
        {
            lock (canvas)
            {
                if (tileData?.Data != null)
                {
                    if (displayRot != 0.0 || geoTransform != null)
                    {
                        #region neue Methode (mit drehen)

                        Point p1 = new Point(tilePoint.X + (tileData.Col - col0) * _grid.TileWidth(res),
                                             tilePoint.Y + (tileData.Row - row0 - (_grid.Orientation == TileGridOrientation.UpperLeft ? 0 : 1)) *
                                                  _grid.TileHeight(res) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));
                        Point p3 = new Point(p1.X, p1.Y + _grid.TileHeight(res) * (_grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));

                        if (_grid.Orientation == TileGridOrientation.LowerLeft)
                        {
                            Point hp = new Point(p1);
                            p1 = new Point(p3);
                            p3 = new Point(hp);
                        }
                        Point p2 = new Point(p1.X + _grid.TileWidth(res), Math.Max(p1.Y, p3.Y));

                        if (geoTransform != null)
                        {
                            geoTransform.InvTransform(p1);
                            geoTransform.InvTransform(p2);
                            geoTransform.InvTransform(p3);
                        }
                        p1 = _map.WorldToImage(p1);
                        p2 = _map.WorldToImage(p2);
                        p3 = _map.WorldToImage(p3);

                        p1.X -= .5f; p1.Y -= .5f;
                        p2.X += .5f; p2.Y -= .5f;
                        p3.X -= .5f; p3.Y += .5f;

                        CanvasPointF[] points = new CanvasPointF[]{
                                                new CanvasPointF((float)p1.X,(float)p1.Y),
                                                new CanvasPointF((float)p2.X,(float)p2.Y),
                                                new CanvasPointF((float)p3.X,(float)p3.Y),
                                           };

                        using (var ms = new MemoryStream(tileData.Data))
                        using (var tileImage = Current.Engine.CreateBitmap(ms))
                        {
                            canvas.DrawBitmap(tileImage,
                                         points,
                                         new CanvasRectangleF(0f, 0f, tileImage.Width, tileImage.Height));
                        }

                        #endregion
                    }
                    else
                    {
                        using (var ms = new MemoryStream(tileData.Data))
                        using (var tileBitmap = Current.Engine.CreateBitmap(ms))
                        {
                            CanvasRectangleF rect;
                            switch (canvas.InterpolationMode)
                            {
                                case InterpolationMode.Bilinear:
                                case InterpolationMode.Bicubic:
                                    rect = new CanvasRectangleF(0f, 0f, tileBitmap.Width - 1f, tileBitmap.Height - 1f);
                                    break;
                                case InterpolationMode.NearestNeighbor:
                                    rect = new CanvasRectangleF(-0.5f, -0.5f, tileBitmap.Width, tileBitmap.Height);
                                    break;
                                default:
                                    rect = new CanvasRectangleF(0f, 0f, tileBitmap.Width, tileBitmap.Height);
                                    break;
                            }

                            #region alte Methode

                            double x0 = iTilePoint.X + (tileData.Col - col0) * iW;
                            double y0 = iTilePoint.Y + (tileData.Row - row0) * iH * (_grid.Orientation == TileGridOrientation.UpperLeft ? 1.0 : -1.0);

                            canvas.DrawBitmap(tileBitmap,
                                new CanvasRectangleF((float)x0, (float)y0, (float)(iW + 0.5), (float)(iH + 0.5)),
                                rect);
                        }

                        #endregion
                    }
                }
            }
        }
        catch { }
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

    public Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext)
    {
        if (!_legendVisible)
        {
            return Task.FromResult<ServiceResponse>(new ServiceResponse(_map.Services.IndexOf(this), this.ID));
        }

        if (!String.IsNullOrEmpty(FixLegendUrl))
        {
            return Task.FromResult<ServiceResponse>(new ImageLocation(_map.Services.IndexOf(this),
                this.ID, String.Empty, FixLegendUrl));
        }

        return Task.FromResult<ServiceResponse>(new EmptyImage(_map.Services.IndexOf(this), this.ID));
    }

    private bool _showServiceLegendInMap = false;
    public bool ShowServiceLegendInMap
    {
        get
        {
            return _showServiceLegendInMap;
        }
        set
        {
            // will set in _fixLegendUrl
            //_showServiceLegendInMap = value;  
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
            _showServiceLegendInMap = !String.IsNullOrEmpty(_fixLegendUrl);
        }
    }

    #endregion

    #region IServiceCopyrightInfo 

    public string CopyrightInfoId { get; set; }

    #endregion

    #region IServiceSupportedCrs Member

    public int[] SupportedCrs
    {
        get { return _projIds; }
        set { _projIds = value; }
    }

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

    #region IServiceInitialException

    public ErrorResponse InitialException => _initErrorResponse;

    #endregion
}
