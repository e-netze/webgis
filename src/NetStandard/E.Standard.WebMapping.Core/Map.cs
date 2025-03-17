using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.ServiceResponses;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core;

public class Map : Display, IMap
{
    private string _name = String.Empty;
    private readonly ServiceCollection _services;
    private Envelope _initialExtent;
    private double _refScale, _mapScale;
    private SelectionCollection _selection;
    private UserData _environment;
    private SpatialReference _sRef = null;
    //private double _R = 6378137.0;
    private string _requestId = String.Empty;
    private readonly GraphicsContainer _graphicsContainer;

    public Map()
    {
        _services = new ServiceCollection();
        _extent = new Envelope();
        _initialExtent = new Envelope();

        this.Dpi = 96.0;

        _iWidth = 500;
        _iHeight = 400;
        _refScale = 0;

        _selection = new SelectionCollection(this);

        _environment = new UserData();
        _requestId = Guid.NewGuid().ToString("N").ToLower();
        _graphicsContainer = new GraphicsContainer();
    }

    public Map(string name)
        : this()
    {
        _name = name;
    }

    //public Map(string name, string sessionID, string sessionUser, string appConfigPath)
    //    : this(name)
    //{
    //    string connectorConfig =
    //            String.IsNullOrEmpty(appConfigPath) ?
    //            String.Empty :
    //            appConfigPath + @"\ims\dotNETConnector.xml";
    //}

    #region IMap Member

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

    public ServiceCollection Services
    {
        get { return _services; }
    }

    public MapRestrictions MapRestrictions { get; set; }

    public ServiceRestirctions GetServivceRestrictions(IMapService service)
    {
        if (this.MapRestrictions == null || service == null || !this.MapRestrictions.ContainsKey(service.ID))
        {
            return null;
        }

        return this.MapRestrictions[service.ID];
    }

    public double MapScale
    {
        get
        {
            return _mapScale;
        }
        set
        {
            SetScale(value, _iWidth, _iHeight);
        }
    }

    // Experimental Feature
    // Use this eg for VisInScale to solve problems in WebMercator!?
    public double ServiceMapScale { get; private set; }

    public double RefScale
    {
        get { return _refScale; }
        set { _refScale = value; }
    }

    public double Resolution
    {
        get
        {
            return _mapScale / (_dpi / 0.0254);
        }
    }

    //public double MinScale
    //{
    //    get
    //    {
    //        if (_useMinScale && _fixScales != null && _fixScales.Count > 0)
    //            return Math.Max(1.0, (double)_fixScales[0]);
    //        else
    //            return 1.0;
    //    }
    //}

    public Envelope InitialExtent
    {
        get { return _initialExtent; }
        set
        {
            if (value != null)
            {
                _initialExtent = value;
            }
        }
    }

    public bool IsDirty
    {
        get
        {
            foreach (IMapService service in _services)
            {
                if (service.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }
        set
        {
            foreach (IMapService service in _services)
            {
                service.IsDirty = value;
            }
        }
    }

    public void SetScale(double scale, int iWidth, int iHeight)
    {
        if (Extent == null)
        {
            return;
        }

        Point cPoint = Extent.CenterPoint;

        SetScale(scale, iWidth, iHeight, cPoint.X, cPoint.Y);
    }

    public void SetScale(double scale, int iWidth, int iHeight, double cx, double cy)
    {
        if (Extent == null)
        {
            return;
        }

        scale = Math.Max(scale, 5.0);

        Point eC = Extent.CenterPoint;
        if (_mapScale.EqualDoubleValue(scale) &&
            eC.X.EqualDoubleValue(cx) &&
            eC.Y.EqualDoubleValue(cy) &&
           _iWidth == iWidth &&
           _iHeight == iHeight)
        {
            // all the same
            return;
        }

        this.IsDirty = true;
        this.Selection.IsDirty = true;

        ImageWidth = iWidth;
        ImageHeight = iHeight;

        _mapScale = scale;

        //double w = (iWidth / _dpm) * scale;
        //double h = (iHeight / _dpm) * scale;

        //if (!IsProjective)
        //{
        //    double phi = cy * Math.PI / 180.0;
        //    w = (w / (_R * Math.Cos(phi))) * 180.0 / Math.PI;
        //    h = (h / _R) * 180.0 / Math.PI;
        //}

        Envelope ex = SphericHelper.CalcExtent(this, new Geometry.Point(cx, cy), _mapScale);

        //Extent.Set(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2);
        Extent.Set(ex.MinX, ex.MinY, ex.MaxX, ex.MaxY);

        if (this.SpatialReference.IsWebMercator())
        {
            var centerPoint = SphericHelper.WebMercator_2_WGS84(this.Extent.CenterPoint);
            ServiceMapScale = _mapScale / Math.Cos(centerPoint.Y / 180.0 * Math.PI);
        }
        else
        {
            ServiceMapScale = _mapScale;
        }

        //if (OnExtentChanged != null)
        //    OnExtentChanged(this, _session);
    }

    public void ZoomTo(Envelope extent)
    {
        if (extent.Width < double.Epsilon ||
            extent.Height < double.Epsilon)
        {
            SetScale(100.0, this.ImageWidth, this.ImageHeight,
                     extent.CenterPoint.X, extent.CenterPoint.Y);
        }
        else
        {
            //double W = IsProjective ? extent.Width : extent.SphericWidth(_R);
            //double H = IsProjective ? extent.Height : extent.SphericHeight(_R);

            //double mapScale = Math.Max(
            //           W / _iWidth * _dpm,
            //           H / _iHeight * _dpm);
            double mapScale = SphericHelper.CalcScale(this, extent);

            Point cPoint = extent.CenterPoint;
            SetScale(mapScale, _iWidth, _iHeight, cPoint.X, cPoint.Y);
        }
    }

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext,
                                                   bool makeTransparent = false,
                                                   ArgbColor? transparentColor = null,
                                                   string format = null,
                                                   bool throwException = false)
    {
        var httpService = requestContext.Http;
        var serviceResponses = new ServiceResponses();

        //int requestCount = 0;
        foreach (IMapService service in _services)
        {
            if (!String.IsNullOrEmpty(service.CollectionId))
            {
                continue;
            }

            if (service.ResponseType == ServiceResponseType.Image ||
                (service is IPrintableService))
            {
                GetMapThread st = new GetMapThread(service, serviceResponses);
                st.ThreadFinisched += new EventHandler(st_ThreadFinisched);

                ServiceResponse preResp = null;
                if (service is IMapService2)
                {
                    preResp = ((IMapService2)service).PreGetMap();
                    if (preResp != null)
                    {
                        serviceResponses.Add(service, preResp);
                        //requestCount++;
                    }
                }

                if (preResp == null)
                {
                    //Thread thread = new Thread(new ThreadStart(st.Run));
                    //thread.Start();
                    //requestCount++;
                    await st.RunAsync(requestContext);
                }
            }
        }

        #region Selection abschicken (optimiert: wenn mehrer Selection in einem Service, dann nur einmal aufrufen...)

        Dictionary<SelectionCollection, IMapService> collections = new Dictionary<SelectionCollection, IMapService>();
        SelectionCollection collection = null;
        foreach (Selection selection in this.Selection)
        {
            if (selection == null)
            {
                continue;
            }

            foreach (IMapService service in _services)
            {
                // zB. mit AGS kann immer nur eine Selektion gezeichnet werden...
                bool canMultipleSelection = service is IServiceSelectionProperties ?
                            ((IServiceSelectionProperties)service).CanMultipleSelections : true;

                if (service.Layers.Contains(selection.Layer))
                {
                    if (collection != null &&
                        collections[collection] == service &&
                        canMultipleSelection)
                    {
                        collection.Add(selection);
                    }
                    else
                    {
                        collection = new SelectionCollection();
                        collection.Add(selection);
                        collections.Add(collection, service);
                    }
                }
            }
        }
        foreach (SelectionCollection coll in collections.Keys)
        {
            GetSelectionThrad st = new GetSelectionThrad(collections[coll], coll, serviceResponses);
            st.ThreadFinisched += new EventHandler(st_ThreadFinisched);

            //Thread thread = new Thread(new ThreadStart(st.Run));
            //thread.Start();
            //requestCount++;
            await st.RunAsync(requestContext);

        }

        #region Collect Errors

        ErrorResponseCollection errorResponses = new ErrorResponseCollection(_services);
        foreach (var serviceRespone in serviceResponses)
        {
            if (serviceRespone.Value is ErrorResponse)
            {
                errorResponses.Add((ErrorResponse)serviceRespone.Value);
            }
            else if (serviceRespone.Value?.InnerErrorResponse != null)
            {
                errorResponses.Add(serviceRespone.Value.InnerErrorResponse);
            }
        }

        #endregion

        #endregion

        using (ImageMerger merger = new ImageMerger(this, requestContext))
        {
            merger.outputPath = this.OutputPath;
            merger.outputUrl = this.OutputUrl;
            merger.MakeTransparent = makeTransparent;
            merger.TransparentColor = transparentColor;

            //
            // Sollte nicht mehr notwendig sein, weil die Services jetzt einzelen (mit await) abgerufen werden
            // Dauert zwar vielleicht ein bisserl länger, dafür resourcen schonender
            // Grundsätzlich sollten die Diente sowieso schnell antworten und ob User 3 oder 5 Sekunden auf ein PDF wartet sollte egal sein
            //
            //while (serviceResponses.Keys.Count < requestCount)
            //{
            //    Console.WriteLine("Map.GetMap: sleep 100");
            //    Thread.Sleep(100);
            //}

            int responseCount = 0;
            StringBuilder errors = new StringBuilder();

            foreach (IMapService service in _services)
            {
                if (!serviceResponses.ContainsKey(service))
                {
                    continue;
                }

                ServiceResponse response = serviceResponses[service];
                if (response == null)
                {
                    continue;
                }

                if (response is ImageLocation imageLocationResponse)
                {
                    if (imageLocationResponse.IsEmptyImage)
                    {
                        continue;
                    }

                    if (imageLocationResponse.ImageUrl.ToLower().StartsWith("fileredirect.aspx/"))
                    {
                        try
                        {
                            string uriString = (string)Environment.UserValue("WebAppUrl", String.Empty);
                            uriString = uriString.Substring(0, uriString.LastIndexOf("/"));
                            Uri uri = new Uri(uriString);
                            uriString = uriString.Replace(uri.Host, "localhost");

                            imageLocationResponse.ImageUrl = uriString + "/" + imageLocationResponse.ImageUrl;
                        }
                        catch { }
                    }
                    //string imagePath = await _connector.GetImage2Async(il.ImagePath, il.ImageUrl, merger.outputPath);
                    string imagePath = await httpService.GetImagePathAsync(imageLocationResponse.ImagePath, imageLocationResponse.ImageUrl, merger.outputPath);

                    if (String.IsNullOrEmpty(imagePath))
                    {
                        continue;
                    }

                    merger.Add(imagePath,
                               service is IGraphicsService ? int.MaxValue : _services.IndexOf(service),   // Graphics immer ganz oben, noch über Selektion zeichnen!!
                               service.Opacity);
                    responseCount++;
                }

                if (response is ErrorResponse || response.InnerErrorResponse != null)
                {
                    var errorResponse = response.InnerErrorResponse ?? response as ErrorResponse;
                    if (errors.Length > 0)
                    {
                        errors.Append("\r\n\r\n");
                    }

                    errors.Append("Service: " + service.Name + ":\r\n");
                    errors.Append(errorResponse.ErrorMessage);

                    try
                    {
                        requestContext.GetRequiredService<IWarningsLogger>()
                            .LogString(CMS.Core.CmsDocument.UserIdentification.Anonymous,
                                       String.Empty, service.ServiceShortname,
                                       "GetMap/PrintMap", errorResponse.ErrorMessage + "|" + errorResponse.ErrorMessage2);
                    }
                    catch { }
                }
            }
            int selCounter = 1;

            foreach (SelectionCollection coll in collections.Keys)
            {
                ServiceResponse response = serviceResponses[coll];
                if (response == null)
                {
                    continue;
                }

                if (response is ImageLocation)
                {
                    ImageLocation il = (ImageLocation)response;
                    if (il.IsEmptyImage)
                    {
                        continue;
                    }

                    string imagePath = await httpService.GetImagePathAsync(il.ImagePath, il.ImageUrl, merger.outputPath);

                    if (String.IsNullOrEmpty(imagePath))
                    {
                        continue;
                    }

                    merger.Add(imagePath, _services.Count + selCounter, 1f);
                    selCounter++;
                }
                if (response is ErrorResponse || response.InnerErrorResponse != null)
                {
                    var errorResponse = response.InnerErrorResponse ?? response as ErrorResponse;
                    if (errors.Length > 0)
                    {
                        errors.Append("\r\n\r\n");
                    }

                    errors.Append("Selection:\n");
                    errors.Append(errorResponse.ErrorMessage);
                }
            }

            if (errors.Length > 0 && ((bool)this.Environment.UserValue("show_warnings_in_print_output", true) == true /*|| throwException==true*/))
            {
                string errorMessage = "In einigen Diensten sind Fehler aufgetreten. Dies kann dazu führen,dass Daten unvollstängig oder falsch dargestellt werden!\r\n\r\n" + errors.ToString();
                if (throwException)
                {
                    throw new DrawMapException(errorMessage);
                }

                using (var errpic = Current.Engine.CreateBitmap(this._iWidth, this._iHeight, PixelFormat.Rgba32))
                using (var canvas = errpic.CreateCanvas())
                using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10))
                using (var redBrush = Current.Engine.CreateSolidBrush(ArgbColor.Red))
                using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
                using (var blackPen = Current.Engine.CreatePen(ArgbColor.Red, 1))
                {
                    errpic.MakeTransparent();
                    var size = canvas.MeasureText(errorMessage, font);
                    size = new CanvasSizeF((float)Math.Min(size.Width, this._iWidth * 0.8), (float)Math.Min(size.Height, this._iHeight * 0.8));
                    canvas.TranslateTransform(new CanvasPointF(this._iWidth / 2f - size.Width / 2f, this._iHeight / 2f - size.Height / 2f));
                    canvas.FillRectangle(whiteBrush, new CanvasRectangle(-10, -10, (int)size.Width + 10, (int)size.Height + 10));
                    canvas.DrawRectangle(blackPen, new CanvasRectangle(-10, -10, (int)size.Width + 10, (int)size.Height + 10));
                    canvas.DrawText(errorMessage, font, redBrush, new CanvasRectangleF(0f, 0f, size.Width, size.Height));

                    string errpic_filename = $"{this.OutputPath}/err_{Guid.NewGuid().ToString("N").ToLower()}.png";
                    await errpic.SaveOrUpload(errpic_filename, ImageFormat.Png);
                    merger.Add(errpic_filename, _services.Count + 100, 1f);
                }
            }

            merger.ImageFormat = format?.ToLowerInvariant() switch
            {
                "jpg" => ImageFormat.Jpeg,
                "jpeg" => ImageFormat.Jpeg,
                _ => ImageFormat.Png  // default should be png, most apps (buffer etc) need transparency
            };

            var imageLocation = await merger.Merge(ImageWidth, ImageHeight);

            if (imageLocation.expections != null && imageLocation.expections.Count() > 0)
            {
                string errorMessage = String.Join("\n", imageLocation.expections.Select(ex => ex.Message));

                errorResponses.Add(new ErrorResponse(0, string.Empty, errorMessage, String.Empty));
            }

            return new ImageLocation(0, "0", imageLocation.imagePath, imageLocation.imageUrl)
            {
                InnerErrorResponse = errorResponses.HasErrors ? errorResponses : null
            };
        }
    }

    async public Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext)
    {
        var httpService = requestContext.Http;
        var serviceResponses = new ServiceResponses();

        //int requestCount = 0;
        foreach (IMapService service in ListOps<IMapService>.Reverse(_services))
        {
            if (!(service is IServiceLegend) ||
                ((IServiceLegend)service).ShowServiceLegendInMap == false)
            {
                continue;
            }

            #region Sichtbarkeit

            bool visible = true;

            if (!visible)
            {
                continue;
            }

            #endregion

            GetLegendThread lt = new GetLegendThread(service, serviceResponses);
            lt.ThreadFinisched += new EventHandler(st_ThreadFinisched);

            await lt.RunAsync(requestContext);
        }

        #region Join Thread
        // Nicht mehr notwendig (siehe GetMapAsync)
        //while (serviceResponses.Keys.Count < requestCount)
        //{
        //    Console.WriteLine("Map.GetLegend: sleep 100");
        //    Thread.Sleep(100);
        //}
        #endregion

        using (LegendMerger merger = new LegendMerger(this, requestContext))
        {
            merger.outputPath = this.OutputPath;
            merger.outputUrl = this.OutputUrl;

            int responseCount = 0;
            foreach (IMapService service in ListOps<IMapService>.Reverse(_services))
            {
                if (!serviceResponses.ContainsKey(service))
                {
                    continue;
                }

                ServiceResponse response = serviceResponses[service];
                if (response == null)
                {
                    continue;
                }

                if (response is ImageLocation)
                {
                    ImageLocation il = (ImageLocation)response;
                    if (il.IsEmptyImage)
                    {
                        continue;
                    }
                    string iUrl = il.ImageUrl, iPath = il.ImagePath;
                    if (String.IsNullOrEmpty(iPath) && !iUrl.Contains("://")) // relative Url (bei fixer Legende)!
                    {
                        iPath = this.Environment.UserString("WebAppPath") + @"\" + iUrl.Replace("/", @"\");
                    }

                    var image = await httpService.GetImageAsync(iPath, iUrl);

                    if (image == null)
                    {
                        continue;
                    }

                    string title = service.Name;
                    merger.Add(image, title, responseCount++);
                }
            }

            var imageLocation = await merger.Merge();

            return new ImageLocation(0, "0", imageLocation.imagePath, imageLocation.imageUrl);
        }
    }

    public SelectionCollection Selection
    {
        get { return _selection; }
    }

    public ILayer LayerById(string layerId)
    {
        foreach (IMapService service in _services)
        {
            if (service == null)
            {
                continue;
            }

            ILayer layer = service?.Layers?.FindById(layerId);
            if (layer != null)
            {
                return layer;
            }
        }
        return null;
    }
    public ILayer LayerByName(string layerName)
    {
        foreach (IMapService service in _services)
        {
            if (service == null)
            {
                continue;
            }

            ILayer layer = service.Layers.FindByName(layerName);
            if (layer != null)
            {
                return layer;
            }
        }
        return null;
    }
    public IMapService ServiceByLayer(ILayer layer)
    {
        foreach (IMapService service in _services)
        {
            if (service == null)
            {
                continue;
            }

            if (service.Layers.Contains(layer))
            {
                return service;
            }
        }
        return null;
    }

    public IMapService FirstServiceByType(Type type)
    {
        foreach (IMapService service in _services)
        {
            if (service.GetType().Equals(type))
            {
                return service;
            }
        }

        return null;
    }

    public IUserData Environment
    {
        get { return _environment; }
    }

    public IGraphicsContainer GraphicsContainer
    {
        get { return _graphicsContainer; }
    }

    public SpatialReference SpatialReference
    {
        get
        {
            return _sRef;
        }
        set
        {
            _sRef = value;
        }
    }

    #endregion

    #region ServerThreading
    void st_ThreadFinisched(object sender, EventArgs e)
    {
        if (sender is GetMapThread)
        {
            GetMapThread st = (GetMapThread)sender;
        }
        // Responses werden schon im ServiceThread eingetragen
    }

    private class ServiceResponses : Dictionary<object, ServiceResponse>
    {
        private readonly object locker = new object();

        new public void Add(object key, ServiceResponse value)
        {
            lock (locker)
            {
                base.Add(key, value);
            }
        }
    }
    private class GetMapThread
    {
        protected IMapService _service;
        protected ServiceResponse _response = null;
        protected ServiceResponses _serviceResponses;

        public event EventHandler ThreadFinisched = null;

        public GetMapThread(IMapService service, ServiceResponses serviceResponses)
        {
            _service = service;
            _serviceResponses = serviceResponses;
        }

        //virtual public void Run()
        //{
        //    try
        //    {
        //        try
        //        {
        //            if (_service != null)
        //                if (_service.ResponseType == ServiceResponseType.Image)
        //                    _response = _service.GetMap();
        //                else if (_service is IPrintableService)
        //                    _response = ((IPrintableService)_service).GetPrintMap();

        //            if (_serviceResponses != null)
        //            {
        //                lock (_serviceResponses)
        //                    _serviceResponses.Add(_service, _response);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            try
        //            {
        //                if (_serviceResponses != null)
        //                {
        //                    lock (_serviceResponses)
        //                    {
        //                        if (_service != null)
        //                            _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
        //                        else
        //                            _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
        //                    }
        //                }
        //            }
        //            catch (Exception ex2)
        //            {
        //                if (_service != null && _service.Map != null && _service.Map.ExceptionLogger is IExceptionLogger)
        //                    ((IExceptionLogger)_service.Map.ExceptionLogger).LogException(_service.Server, _service.ServiceName, "GetMap", ex2);
        //            }
        //        }
        //        Callback();
        //    }
        //    catch { }
        //}

        async virtual public Task RunAsync(IRequestContext requestContext)
        {
            try
            {
                try
                {
                    if (_service != null)
                    {
                        if (_service.ResponseType == ServiceResponseType.Image)
                        {
                            _response = await _service.GetMapAsync(requestContext);
                        }
                        else if (_service is IPrintableService)
                        {
                            _response = await ((IPrintableService)_service).GetPrintMapAsync(requestContext);
                        }
                    }

                    if (_serviceResponses != null)
                    {
                        lock (_serviceResponses)
                        {
                            _serviceResponses.Add(_service, _response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (_serviceResponses != null)
                        {
                            lock (_serviceResponses)
                            {
                                if (_service != null)
                                {
                                    _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
                                }
                                else
                                {
                                    _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
                                }
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        requestContext.GetRequiredService<IExceptionLogger>()
                            .LogException(_service?.Map, _service.Server, _service.Service, "GetMap", ex2);
                    }
                }
                Callback();
            }
            catch { }
        }

        public ServiceResponse ServiceRespone
        {
            get { return _response; }
        }
        public IMapService Service
        {
            get { return _service; }
        }

        protected void Callback()
        {
            if (ThreadFinisched != null)
            {
                ThreadFinisched(this, new EventArgs());
            }
        }
    }

    private class GetLegendThread : GetMapThread
    {
        public GetLegendThread(IMapService service, ServiceResponses serviceResponses)
            : base(service, serviceResponses)
        {
        }

        //public override void Run()
        //{
        //    try
        //    {
        //        try
        //        {
        //            if (_service is IServiceLegend)
        //                _response = ((IServiceLegend)_service).GetLegend();

        //            if (_serviceResponses != null)
        //            {
        //                lock (_serviceResponses)
        //                    _serviceResponses.Add(_service, _response);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            if (_serviceResponses != null)
        //            {
        //                lock (_serviceResponses)
        //                {
        //                    if (_service != null)
        //                        _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
        //                    else
        //                        _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
        //                }
        //            }
        //        }
        //        Callback();
        //    }
        //    catch { }
        //}

        async public override Task RunAsync(IRequestContext requestContext)
        {
            try
            {
                try
                {
                    if (_service is IServiceLegend)
                    {
                        _response = await ((IServiceLegend)_service).GetLegendAsync(requestContext);
                    }

                    if (_serviceResponses != null)
                    {
                        lock (_serviceResponses)
                        {
                            _serviceResponses.Add(_service, _response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_serviceResponses != null)
                    {
                        lock (_serviceResponses)
                        {
                            if (_service != null)
                            {
                                _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
                            }
                            else
                            {
                                _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
                            }
                        }
                    }
                }
                Callback();
            }
            catch { }
        }
    }

    private class GetSelectionThrad
    {
        protected IMapService _service;
        protected SelectionCollection _selections;
        protected ServiceResponse _response = null;
        protected ServiceResponses _serviceResponses;

        public event EventHandler ThreadFinisched = null;

        public GetSelectionThrad(IMapService service, SelectionCollection selections, ServiceResponses serviceResponses)
        {
            _service = service;
            _selections = selections;
            _serviceResponses = serviceResponses;
        }

        //virtual public void Run()
        //{
        //    try
        //    {
        //        try
        //        {
        //            if (_service != null && _selections != null)
        //            {
        //                _response = _service.GetSelection(_selections);
        //            }

        //            if (_serviceResponses != null)
        //            {
        //                lock (_serviceResponses)
        //                    _serviceResponses.Add(_selections, _response);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            try
        //            {
        //                if (_serviceResponses != null)
        //                {
        //                    lock (_serviceResponses)
        //                    {
        //                        if (_service != null)
        //                            _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
        //                        else
        //                            _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
        //                    }
        //                }
        //            }
        //            catch (Exception ex2)
        //            {
        //                if (_service != null && _service.Map != null && _service.Map.ExceptionLogger is IExceptionLogger)
        //                    ((IExceptionLogger)_service.Map.ExceptionLogger).LogException(_service.Server, _service.ServiceName, "GetSelection", ex2);
        //            }
        //        }
        //        Callback();
        //    }
        //    catch { }
        //}

        async public Task RunAsync(IRequestContext requestContext)
        {
            try
            {
                try
                {
                    if (_service != null && _selections != null)
                    {
                        _response = await _service.GetSelectionAsync(_selections, requestContext);
                    }

                    if (_serviceResponses != null)
                    {
                        lock (_serviceResponses)
                        {
                            _serviceResponses.Add(_selections, _response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (_serviceResponses != null)
                        {
                            lock (_serviceResponses)
                            {
                                if (_service != null)
                                {
                                    _serviceResponses.Add(_service, new ExceptionResponse(-1, _service.ID, ex));
                                }
                                else
                                {
                                    _serviceResponses.Add(_service, new ExceptionResponse(-1, String.Empty, ex));
                                }
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        requestContext.GetRequiredService<IExceptionLogger>()
                            .LogException(_service?.Map, _service.Server, _service.Service, "GetSelection", ex2);
                    }
                }
                Callback();
            }
            catch { }
        }

        public ServiceResponse ServiceRespone
        {
            get { return _response; }
        }
        public IMapService Service
        {
            get { return _service; }
        }

        protected void Callback()
        {
            if (ThreadFinisched != null)
            {
                ThreadFinisched(this, new EventArgs());
            }
        }
    }

    private class InitThread : GetMapThread
    {
        private readonly IMap _map = null;

        public InitThread(IMap map, IMapService service, ServiceResponses serviceResponses)
            : base(service, serviceResponses)
        {
            _map = map;
        }
        //public override void Run()
        //{
        //    try
        //    {
        //        if (_service != null && _map != null)
        //        {
        //            try
        //            {
        //                _response = new SuccessResponse(_map.Services.IndexOf(_service), _service.ID, _service.Init(_map));

        //                #region Cache Service

        //                if ((_service.Diagnostics == null || _service.Diagnostics.State == ServiceDiagnosticState.Ok) &&
        //                    ((SuccessResponse)_response).Succeeded && _service.Layers.Count > 0 && !(_service is IUserService))
        //                {
        //                    if (_map.MapSession.MapApplication.Cache[Map.ServiceCacheId(_map, _service)] == null)
        //                        _map.MapSession.MapApplication.Cache.Add(Map.ServiceCacheId(_map, _service), _service.Clone(_map) as IClone, CacheObjectType.Service);
        //                }
        //                #endregion
        //            }
        //            catch (Exception ex)
        //            {
        //                if (_map.ExceptionLogger is IExceptionLogger)
        //                    ((IExceptionLogger)_map.ExceptionLogger).LogException(_service.Server, _service.ServiceName, "Init", ex);
        //                _response = new ExceptionResponse(_map.Services.IndexOf(_service), _service.ID, ex);
        //            }
        //        }

        //        if (_serviceResponses != null)
        //        {
        //            lock (_serviceResponses)
        //                _serviceResponses.Add(_service, _response);
        //        }

        //        Callback();
        //    }
        //    catch (Exception ex)
        //    {
        //        try
        //        {
        //            if (_map != null && _map.ExceptionLogger is IExceptionLogger && _service != null)
        //            {
        //                ((IExceptionLogger)_map.ExceptionLogger).LogException(
        //                    _service.Server, _service.ServiceName, "Init",
        //                    new Exception("Error@Service: " + _service.Name + " (" + _service.ID + ")", ex));
        //            }
        //        }
        //        catch { }
        //    }
        //}

        async override public Task RunAsync(IRequestContext requestContext)
        {
            try
            {
                if (_service != null && _map != null)
                {
                    try
                    {
                        _response = new SuccessResponse(_map.Services.IndexOf(_service), _service.ID, await _service.InitAsync(_map, requestContext));
                    }
                    catch (Exception ex)
                    {
                        requestContext.GetRequiredService<IExceptionLogger>()
                            .LogException(_map, _service.Server, _service.Service, "Init", ex);

                        _response = new ExceptionResponse(_map.Services.IndexOf(_service), _service.ID, ex);
                    }
                }

                if (_serviceResponses != null)
                {
                    lock (_serviceResponses)
                    {
                        _serviceResponses.Add(_service, _response);
                    }
                }

                Callback();
            }
            catch (Exception ex)
            {
                try
                {
                    requestContext.GetRequiredService<IExceptionLogger>()
                        .LogException(_map, _service.Server, _service.Name, "GetMap", ex);
                }
                catch { }
            }
        }
    }

    #endregion

    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    #region IClone Member

    public IMap Clone(object parent)
    {
        Map clone = new Map(_name);
        // UserData
        clone._environment?.CopyData(clone._environment);

        foreach (IMapService service in _services)
        {
            if (service == null)
            {
                continue;
            }

            clone._services.Add(service.Clone(clone));
        }

        clone._extent = new Envelope(_extent);
        clone._initialExtent = new Envelope(_initialExtent);
        clone._dpi = _dpi;
        clone._dpm = _dpm;
        clone._refScale = _refScale;
        clone._mapScale = _mapScale;
        clone.ServiceMapScale = this.ServiceMapScale;
        clone._iWidth = _iWidth;
        clone._iHeight = _iHeight;
        if (_selection != null)
        {
            clone._selection = _selection.Clone(clone);
        }

        clone._sRef = (_sRef != null) ? _sRef.Clone() : null;

        clone.DisplayRotation = _displayRotation;

        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        return clone;
    }

    #endregion

    #region IMap Member

    //public event OnExtentChangedEventHandler OnExtentChanged;

    public string RequestId
    {
        get { return _requestId; }
        set { _requestId = value; }
    }
    #endregion

    #region Helper

    private string OutputPath => this.Environment?.UserString("OutputPath")/*.OrTake(_connector?.outputPath)*/;

    private string OutputUrl => this.Environment?.UserString("OutputUrl")/*.OrTake(_connector?.outputUrl)*/;


    #endregion
}
