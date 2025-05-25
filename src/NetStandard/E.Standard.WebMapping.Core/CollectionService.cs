using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core;

public class CollectionService : IMapService, IEnumerable<IMapService>, IPrintableMapService, IMapServiceSupportedCrs
{
    private string _name = "CollectionService";
    private string _id = String.Empty;
    private float _opacity = 1.0f;
    private bool _useToc = true;
    private bool _isBaseMap = false;
    private BasemapType _basemapType = BasemapType.Normal;

    protected LayerCollection _layers = null;
    protected ServiceCollection _services = new ServiceCollection();
    private IMap _map = null;

    public CollectionService()
    {
        _layers = new LayerCollection(this);
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

    string _url = String.Empty;
    public string Url
    {
        get { return _url; }
        set { _url = value; }
    }

    public string Server
    {
        get { return String.Empty; }
    }

    public string Service
    {
        get { return String.Empty; }
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
        get
        {
            //LayerCollection layers = new LayerCollection();
            //foreach (IService service in _services)
            //{
            //    LayerCollection serviceLayers = service.Layers;
            //    if (serviceLayers != null)
            //    {
            //        foreach (ILayer layer in serviceLayers)
            //        {
            //            layers.Add(layer);
            //        }
            //    }
            //}
            //return layers;
            return _layers;
        }
    }

    public Envelope InitialExtent
    {
        get
        {
            Envelope env = new Envelope();
            foreach (IMapService service in _services)
            {
                Envelope ext = service.InitialExtent;
                if (ext == null || ext.IsNull)
                {
                    continue;
                }

                if (env == null || env.IsNull)
                {
                    env = new Envelope(ext);
                }
                else
                {
                    env.Union(ext);
                }
            }
            return env;
        }
    }

    virtual public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Javascript; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        _id = serviceID;

        return true;
    }

    virtual public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        _map = map;

        #region Init Services
        _services.Clear();

        // Passiert alles in Map.Init(), weil die Dienste ja auch den Karten Services zugewiesen sind und dort intialiserit werden
        // Hinzugefügt zur Collection werden sie dann auch erst dort!!!

        //ServiceResponses serviceResponses = new ServiceResponses();

        //foreach (IService service in _services)
        //{
        //    InitThread st = new InitThread(_map, service, serviceResponses);
        //    //st.ThreadFinisched += new EventHandler(st_ThreadFinisched);

        //    Thread thread = new Thread(new ThreadStart(st.Run));
        //    thread.Start();
        //}

        //while (serviceResponses.Keys.Count < _services.Count)
        //{
        //    Thread.Sleep(100);
        //}
        #endregion

        return Task.FromResult(true);
    }

    public bool IsDirty
    {
        get
        {
            if (_map != null)
            {
                foreach (IMapService service in _map.Services.ByCollectionId(this.ID))
                {
                    if (service.IsDirty)
                    {
                        return true;
                    }
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

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        try
        {
            var httpService = requestContext.Http;
            var serviceResponses = new ServiceResponses();

            //int requestCount = 0;
            foreach (IMapService service in _services)
            {
                if (service.ResponseType == ServiceResponseType.Image ||
                    (service is IPrintableMapService))
                {
                    GetMapThread st = new GetMapThread(service, serviceResponses);
                    st.ThreadFinisched += new EventHandler(st_ThreadFinisched);

                    //Thread thread = new Thread(new ThreadStart(st.Run));
                    //thread.Start();
                    //requestCount++;
                    await st.RunAsync(requestContext);

                }
            }

            #region Selection abschicken (optimiert: wenn mehrer Selection in einem Service, dann nur einmal aufrufen...)
            Dictionary<SelectionCollection, IMapService> collections = new Dictionary<SelectionCollection, IMapService>();
            SelectionCollection collection = null;
            foreach (Selection selection in _map.Selection)
            {
                if (selection == null)
                {
                    continue;
                }

                foreach (IMapService service in _services)
                {
                    if (service.Layers.Contains(selection.Layer))
                    {
                        if (collection != null &&
                            collections[collection] == service)
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
                GetSelectionThread st = new GetSelectionThread(collections[coll], coll, serviceResponses);
                st.ThreadFinisched += new EventHandler(st_ThreadFinisched);

                //Thread thread = new Thread(new ThreadStart(st.Run));
                //thread.Start();
                //requestCount++;
                await st.RunAsync(requestContext);
            }
            #endregion

            ImageMerger merger = new ImageMerger(_map, requestContext);
            merger.outputPath = this.OutputPath;
            merger.outputUrl = this.OutputUrl;

            //while (serviceResponses.Keys.Count < requestCount)
            //{
            //    Console.WriteLine("ServiceCollection.GetMap: sleep 100");
            //    Thread.Sleep(100);
            //}

            int responseCount = 0;
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

                if (response is ImageLocation)
                {
                    ImageLocation il = (ImageLocation)response;
                    if (il.IsEmptyImage)
                    {
                        continue;
                    }

                    //string imagePath = await _connector.GetImage2Async(il.ImagePath, il.ImageUrl);
                    string imagePath = await httpService.GetImagePathAsync(il.ImagePath, il.ImageUrl/*, merger.outputPath*/);

                    if (String.IsNullOrEmpty(imagePath))
                    {
                        continue;
                    }

                    merger.Add(imagePath, _services.IndexOf(service), service.InitialOpacity * service.OpacityFactor);
                    responseCount++;
                }
            }
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

                    //string image = await _connector.GetImage2Async(il.ImagePath, il.ImageUrl);
                    string imagePath = await httpService.GetImagePathAsync(il.ImagePath, il.ImageUrl);

                    if (String.IsNullOrEmpty(imagePath))
                    {
                        continue;
                    }

                    merger.Add(imagePath, responseCount++, 1f);
                    responseCount++;
                }
            }

            merger.MakeTransparent = true;
            var imageLocation = await merger.Merge(_map.ImageWidth, _map.ImageHeight);

            return new ImageLocation(_map.Services.IndexOf(this), _id, imageLocation.imagePath, imageLocation.imageUrl);
        }
        catch (Exception ex)
        {
            requestContext.GetRequiredService<IExceptionLogger>()
                .LogException(_map, this.Server, this.Service, "GetMap", ex);

            throw;
        }
        //return null;
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(null);
    }

    public int Timeout
    {
        get
        {
            int timeout = 0;
            foreach (IMapService service in _services)
            {
                timeout = Math.Max(service.Timeout, timeout);
            }

            return timeout;
        }
        set
        {
            foreach (IMapService service in _services)
            {
                service.Timeout = value;
            }
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

    public bool ShowInToc
    {
        get { return true; }
        set { }
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
            return _isBaseMap;
        }
        set
        {
            _isBaseMap = value;
        }
    }

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

    virtual protected CollectionService ClassClone()  // for inherited class with no default constructor: ApiCollectionService...
    {
        return new CollectionService();
    }

    virtual public IMapService Clone(IMap parent)
    {
        CollectionService clone = ClassClone();

        clone._name = _name;
        clone._id = _id;
        clone._opacity = _opacity;
        clone.OpacityFactor = OpacityFactor;
        clone._useToc = _useToc;
        clone._url = _url;

        clone._isBaseMap = _isBaseMap;
        clone._basemapType = _basemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone._supportedCrs = _supportedCrs;

        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        foreach (IMapService service in _services)
        {
            clone.AddService(service.Clone(parent));
        }

        return clone;
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
                        else if (_service is IPrintableMapService)
                        {
                            _response = await ((IPrintableMapService)_service).GetPrintMapAsync(requestContext);
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
        //        if (_service is IServiceLegend)
        //            _response = ((IServiceLegend)_service).GetLegend();

        //        if (_serviceResponses != null)
        //        {
        //            lock (_serviceResponses)
        //                _serviceResponses.Add(_service, _response);
        //        }

        //        Callback();
        //    }
        //    catch { }
        //}
    }

    private class GetSelectionThread
    {
        protected IMapService _service;
        protected SelectionCollection _selections;
        protected ServiceResponse _response = null;
        protected ServiceResponses _serviceResponses;

        public event EventHandler ThreadFinisched = null;

        public GetSelectionThread(IMapService service, SelectionCollection selections, ServiceResponses serviceResponses)
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
        //    if (_service != null && _map != null)
        //    {
        //        try
        //        {
        //               _response = new SuccessResponse(_map.Services.IndexOf(_service), _service.ID, _service.Init(_map));
        //        }
        //        catch (Exception ex)
        //        {
        //            if (_map.ExceptionLogger is IExceptionLogger)
        //                ((IExceptionLogger)_map.ExceptionLogger).LogException(this.Service.Server, this.Service.ServiceName, "Init", ex);
        //            _response = new ExceptionResponse(_map.Services.IndexOf(_service), _service.ID, ex);
        //        }
        //    }

        //    if (_serviceResponses != null)
        //    {
        //        lock (_serviceResponses)
        //            _serviceResponses.Add(_service, _response);
        //    }

        //    Callback();
        //}
    }

    #endregion

    public void AddService(IMapService service)
    {
        if (service == null || _services.Contains(service))
        {
            return;
        }

        _services.Add(service);
    }

    public ILayer FindLayerByName(string name)
    {
        foreach (IMapService service in this)
        {
            ILayer layer = service.Layers.FindByName(name);
            if (layer != null)
            {
                return layer;
            }
        }
        return null;
    }

    public ILayer[] FindLayersByName(string name)
    {
        List<ILayer> layers = new List<ILayer>();
        foreach (IMapService service in this)
        {
            ILayer layer = service.Layers.FindByName(name);
            if (layer != null)
            {
                layers.Add(layer);
            }
        }
        return layers.ToArray();
    }

    public ILayer FindLayerByLayerId(string id)
    {
        foreach (IMapService service in this)
        {
            ILayer layer = service.Layers.FindByLayerId(id);
            if (layer != null)
            {
                return layer;
            }
        }

        return null;
    }

    public LayerCollection AllServiceLayers
    {
        get
        {
            LayerCollection layers = new LayerCollection(this);
            foreach (IMapService service in _services)
            {
                LayerCollection serviceLayers = service.Layers;
                if (serviceLayers != null)
                {
                    foreach (ILayer layer in serviceLayers)
                    {
                        layers.Add(layer);
                    }
                }
            }
            return layers;
        }
    }

    #region IEnumerable<IService> Member

    public IEnumerator<IMapService> GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    #region IEnumerable Member

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    #region Helper Classes
    private class Enumerator : IEnumerator<IMapService>
    {
        private readonly CollectionService _collservice;
        private int _enumPos = 0;

        public Enumerator(CollectionService collservice)
        {
            _collservice = collservice;
        }

        #region IEnumerator<IService> Member

        public IMapService Current
        {
            get
            {
                if (_enumPos > _collservice._services.Count)
                {
                    return null;
                }

                return _collservice._services[_enumPos - 1];
            }
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {
            _enumPos = 0;
        }

        #endregion

        #region IEnumerator Member

        object System.Collections.IEnumerator.Current
        {
            get
            {
                if (_enumPos > _collservice._services.Count)
                {
                    return null;
                }

                return _collservice._services[_enumPos - 1];
            }
        }

        public bool MoveNext()
        {
            _enumPos++;

            return _enumPos <= _collservice._services.Count;
        }

        public void Reset()
        {
            _enumPos = 0;
        }

        #endregion
    }

    #endregion

    #region IPrintableService Member

    async public Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext)
    {
        return await GetMapAsync(requestContext);
    }

    #endregion

    #region Helper

    private string OutputPath => (this._map?.Environment?.UserString("OutputPath"))/*.OrTake(_connector?.outputPath)*/;

    private string OutputUrl => (this._map?.Environment?.UserString("OutputUrl"))/*.OrTake(_connector?.outputUrl)*/;

    #endregion
}
