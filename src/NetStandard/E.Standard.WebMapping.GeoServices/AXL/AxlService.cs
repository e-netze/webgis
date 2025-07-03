using E.Standard.ArcXml;
using E.Standard.ArcXml.Extensions;
using E.Standard.Extensions.Text;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

public class AxlService : IMapService2,
                          IMapServiceLegend,
                          IMapServiceProjection,
                          ICacheServicePrefix,
                          IMapServiceSupportedCrs,
                          IExportableOgcService,
                          IMapServiceMetadataInfo,
                          IMapServiceInitialException,
                          IDynamicService,
                          IImageServiceType,
                          IMapServiceCapabilities
{
    private IMap _map;
    private string _name = String.Empty;
    private string _id = String.Empty;
    private string _server = String.Empty;
    private string _service = String.Empty;
    private LayerCollection _layers;
    private Envelope _initialExtent = new Envelope();
    private XmlDocument _SelectionRenderers_Template = null;
    private bool _isDirty = false;
    private float _opacity = 1.0f;
    private Encoding _encoding = Encoding.Default;
    private bool _extraCheckUmlaut = false, _umlaute2wildcard = false;
    private int _dpi = 96, _timeout = 20;
    private string _errMsg;
    private bool _useToc = true, _rotatable = false, _cap_refscale = false;
    internal CultureInfo _culture;
    internal NumberFormatInfo _nfi;
    private ErrorResponse _initErrorResponse = null, _diagnosticsErrorResponse = null;
    private string _imageFormat = String.Empty;
    private bool _useFixRefScale = false;
    private double _fixRefScale = 0.0;
    private bool _arcmapserver = false;
    private MapUnits _mapUnits = MapUnits.meters;
    private string _overrideLocale = String.Empty;
    private ServiceTheme[] _serviceThemes = null;

    private ArcAxlConnectionProperties _connectionProperties = null;

    public AxlService()
    {
        //_id = System.Guid.NewGuid().ToString("N");
        _culture = new CultureInfo(System.Globalization.CultureInfo.CurrentCulture.TextInfo.CultureName);
        _nfi = _culture.NumberFormat;
        _layers = new LayerCollection(this);
        ShowInToc = true;
    }

    internal ArcAxlConnectionProperties ConnectionProperties => _connectionProperties;

    #region IService Member

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    string _url = String.Empty;
    public string Url
    {
        get { return _url; }
        set { _url = value; }
    }

    public string Service
    {
        get { return _service; }
    }

    public string ServiceShortname { get { return this.Service; } }

    public string Server
    {
        get { return _server; }
    }

    public string ID
    {
        get { return _id; }
    }

    public float InitialOpacity
    {
        get { return _opacity; }
        set
        {
            if (value < 0.0f)
            {
                _opacity = 0.0f;
            }
            else if (value > 1.0f)
            {
                _opacity = 1.0f;
            }
            else
            {
                _opacity = value;
            }
        }
    }

    public float OpacityFactor { get; set; } = 1f;

    public bool ShowInToc { get; set; }

    public bool CanBuffer
    {
        get { return true; }
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
    public Envelope InitialExtent
    {
        get
        {
            return _initialExtent;
        }
    }
    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Image; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool IsDirty
    {
        get { return _isDirty; }
        set { _isDirty = value; }
    }
    public bool PreInit(string serviceID, string server, string service, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        _id = serviceID;
        _server = server;
        _service = service;

        _connectionProperties = new ArcAxlConnectionProperties()
        {
            CheckUmlaut = _extraCheckUmlaut,
            AuthUsername = authUser,
            AuthPassword = authPwd,
            Token = token,
            Timeout = this.Timeout,

            //OutputPath = outputPath,
            //OutputUrl = outputUrl
        };

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

                RefreshSpatialReference();

                String resp,
                    axl = "<ARCXML version=\"1.1\"><REQUEST><GET_SERVICE_INFO fields=\"true\" envelope=\"false\" renderer=\"true\" extensions=\"false\" /></REQUEST></ARCXML>";

                //resp = await _connector.SendRequestAsync(axl, _server, _service);
                resp = await httpService.SendAxlRequestAsync(_connectionProperties, axl, _server, _service);

                if (hasErrorMessage(resp))
                {
                    _initErrorResponse = new ErrorResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp));
                    LogException(requestContext, "Init", _initErrorResponse);
                    return false;
                }
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(resp);

                #region Localize Service
                XmlNode locale = xmldoc.SelectSingleNode("//SERVICEINFO/ENVIRONMENT/LOCALE[@language and @country]");
                if (locale != null)
                {
                    string language = locale.Attributes["language"].Value; // "en";
                    string country = locale.Attributes["country"].Value; // "US";

                    try
                    {
                        if (String.IsNullOrEmpty(_overrideLocale))
                        {
                            _culture = new CultureInfo(language + "-" + country);
                        }
                        else
                        {
                            _culture = new CultureInfo(_overrideLocale);
                        }

                        _nfi = _culture.NumberFormat;
                    }
                    catch (Exception /*ex*/)
                    {

                    }
                    //foreach (CultureInfo cu in System.Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures))
                    //{
                    //    if (cu.TextInfo.CultureName == language + "-" + country)
                    //    {
                    //        _culture = cu;
                    //        break;
                    //    }
                    //}
                }
                #endregion

                #region Screen Resolution

                XmlNode screen = xmldoc.SelectSingleNode("//SERVICEINFO/ENVIRONMENT/SCREEN[@dpi]");
                if (screen != null)
                {
                    try
                    {
                        _dpi = Convert.ToInt32(screen.Attributes["dpi"].Value);
                    }
                    catch { }
                }
                double dpm = (_dpi / 0.0254);
                #endregion

                #region Initial Extent
                XmlNode initialExtent = xmldoc.SelectSingleNode("//SERVICEINFO/PROPERTIES/ENVELOPE[@name='Initial_Extent']");
                if (initialExtent != null)
                {
                    SetInitialExtent(initialExtent);
                }
                #endregion

                #region MapUnits
                XmlNode mapUnits = xmldoc.SelectSingleNode("//SERVICEINFO/PROPERTIES/MAPUNITS[@units]");
                if (mapUnits != null)
                {
                    switch (mapUnits.Attributes["units"].Value.ToLower())
                    {
                        case "meters":
                            _mapUnits = MapUnits.meters;
                            break;
                        case "feet":
                            _mapUnits = MapUnits.feet;
                            break;
                        case "decimal_degrees":
                            _mapUnits = MapUnits.decimal_degrees;
                            break;
                    }
                }

                #endregion

                #region Capabilites

                _arcmapserver = false;
                XmlNode capabilities = xmldoc.SelectSingleNode("//SERVICEINFO/ENVIRONMENT/CAPABILITIES");
                if (capabilities != null)
                {
                    if (capabilities.Attributes["servertype"] != null &&
                        capabilities.Attributes["servertype"].Value.ToLower() == "arcmapserver")
                    {
                        _arcmapserver = true;
                    }
                    if (capabilities.Attributes["displayrotation"] != null &&
                        capabilities.Attributes["displayrotation"].Value.ToLower() == "true")
                    {
                        _rotatable = true;
                    }
                    if (capabilities.Attributes["refscale"] != null &&
                        capabilities.Attributes["refscale"].Value.ToLower() == "true")
                    {
                        _cap_refscale = true;
                    }
                }

                #endregion

                #region Layers

                // Temporäre Liste => Falls Init mehrfach/gleichzeitg aufgerufen wird
                // Am schluss dann an LayerCollection übergeben
                List<Layer> layers = new List<Layer>();

                foreach (XmlNode layerNode in xmldoc.SelectNodes("//LAYERINFO[@name and @id and @type]"))
                {
                    XmlNode fclassNode = layerNode.SelectSingleNode("FCLASS");

                    #region LayerType

                    LayerType layerType = LayerType.unknown;
                    if (layerNode.Attributes["type"].Value == "featureclass" &&
                        fclassNode != null &&
                        fclassNode.Attributes["type"] != null)
                    {
                        switch (fclassNode.Attributes["type"].Value.ToLower())
                        {
                            case "polygon":
                                layerType = LayerType.polygon;
                                break;
                            case "line":
                                layerType = LayerType.line;
                                break;
                            case "point":
                                layerType = LayerType.point;
                                break;
                            case "network":
                                layerType = LayerType.network;
                                break;
                            case "unknown":
                                layerType = LayerType.unknown;
                                break;
                        }
                    }
                    else if (layerNode.Attributes["type"].Value == "image")
                    {
                        layerType = LayerType.image;
                    }

                    #endregion

                    Layer layer = null;
                    if (layerType == LayerType.point ||
                        layerType == LayerType.line ||
                        layerType == LayerType.polygon ||
                        layerType == LayerType.unknown)
                    {
                        FeatureLayer fLayer = new FeatureLayer(
                            layerNode.Attributes["name"].Value,
                            layerNode.Attributes["id"].Value,
                            layerType,
                            this,
                            queryable: true);

                        #region Renderers

                        string renderer = getRendererFromAXLFile(layerNode);

                        // Rastermarkersymbol nicht hier übernehmen bzw. Modifizieren, da die Pfade nicht mehr passen!!!
                        if (renderer.Contains("<RASTERMARKERSYMBOL"))
                        {
                            renderer = String.Empty;
                        }

                        fLayer.OriginalRenderer = fLayer.Renderer = renderer;
                        fLayer.LabelRenderer = null;
                        fLayer.UseLabelRenderer = false;
                        splitLabelRendererFromGrouprenderer(fLayer);

                        #endregion

                        layer = fLayer;
                    }
                    else if (layerType == LayerType.network)
                    {
                        layer = new NetworkLayer(
                            layerNode.Attributes["name"].Value,
                            layerNode.Attributes["id"].Value,
                            this);
                    }
                    else
                    {
                        layer = new RasterLayer(
                            layerNode.Attributes["name"].Value,
                            layerNode.Attributes["id"].Value,
                            layerType,
                            this);
                    }

                    #region Fields

                    if (fclassNode != null)
                    {
                        foreach (XmlNode fieldNode in fclassNode.SelectNodes("FIELD[@name and @type]"))
                        {
                            layer.Fields.Add(new Field(fieldNode.Attributes["name"].Value, GetFieldType(fieldNode)));
                        }
                    }

                    #endregion

                    #region Visible

                    if (layerNode.Attributes["visible"] != null)
                    {
                        layer.Visible = layerNode.Attributes["visible"].Value.ToLower() == "true";
                    }

                    #endregion

                    #region Scales

                    if (!(layerNode.Attributes["minscale"] == null))
                    {
                        //double mins = Convert.ToDouble(layerNode.Attributes["minscale"].Value.Replace(".", ",")) * dpm;
                        double mins = UnitConverter.ToMeters(Shape.Convert(layerNode.Attributes["minscale"].Value, _nfi), _mapUnits) * dpm;
                        layer.MinScale = Math.Round(mins, 0);
                    }

                    if (!(layerNode.Attributes["maxscale"] == null))
                    {
                        //double maxs = Convert.ToDouble(layerNode.Attributes["maxscale"].Value.Replace(".", ",")) * dpm;
                        double maxs = UnitConverter.ToMeters(Shape.Convert(layerNode.Attributes["maxscale"].Value, _nfi), _mapUnits) * dpm;
                        layer.MaxScale = Math.Round(maxs, 0);
                    }
                    ServiceHelper.SetLayerScale(this, layer);

                    #endregion

                    #region Views haben keine ID Spalte

                    if ((layer is FeatureLayer && (String.IsNullOrEmpty(layer.IdFieldName) || Globals.shortName(layer.IdFieldName).ToLower() == "fid")))
                    {
                        Layer.TrySetIdField(_map, this, layer);
                    }

                    #endregion

                    layers.Add(layer);
                }

                this.Layers.SetItems(layers);

                #endregion

                pLogger.Success = true;

                this.Diagnostics = ServiceTheme.CheckServiceLayers(this, _serviceThemes);

                if (this.Diagnostics != null && this.Diagnostics.State != ServiceDiagnosticState.Ok)
                {
                    _diagnosticsErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, new Exception(this.Diagnostics.Message));
                    _errMsg = this.Diagnostics.Message;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            _initErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex, Const.InitServiceExceptionPreMessage);
            _errMsg = ex.Message + "\n" + ex.StackTrace;
            return false;
        }
    }

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map?.Services != null ? this.Map.Services.IndexOf(this) : 0, this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        var httpService = requestContext.Http;

        using (var pLog = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "GetMap", ""))
        {
            //_connector.LogString("webgis4.log", "Start Map Request: " + _service);

            double mapScale = _map.MapScale;
            double refScale = this.RefScale;

            if (!ServiceHelper.VisibleInScale(this, _map))
            {
                pLog.Success = pLog.SuppressLogging = true;
                return new EmptyImage(_map.Services.IndexOf(this), this.ID);
            }

            double widthFactor = -1.0, fontsizeFactor = -1.0;
            if (refScale > 0.1)
            {
                widthFactor = fontsizeFactor = refScale / Math.Max(mapScale, 1.0);
            }

            // REQUEST erzeugen
            StringBuilder axl = new StringBuilder();
            //StringWriter sw=new StringWriter(axl);
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

            //xWriter.Formatting=Formatting.Indented;
            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("GET_IMAGE");
            xWriter.WriteStartElement("PROPERTIES");

            AxlHelper.AXLaddFeatureCoordsys(ref xWriter, this.FeatureCoordsys);
            AxlHelper.AXLaddFilterCoordsys(ref xWriter, this.FilterCoordsys);

            IDisplay display = _map;
            if (_map.DisplayRotation != 0.0 && _rotatable == false)
            {
                display = Display.TransformedDisplay(_map);
            }

            xWriter.WriteStartElement("ENVELOPE");
            xWriter.WriteAttributeString("maxx", display.Extent.MaxX.ToString(_nfi));
            xWriter.WriteAttributeString("minx", display.Extent.MinX.ToString(_nfi));
            xWriter.WriteAttributeString("maxy", display.Extent.MaxY.ToString(_nfi));
            xWriter.WriteAttributeString("miny", display.Extent.MinY.ToString(_nfi));
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("IMAGESIZE");
            xWriter.WriteAttributeString("width", display.ImageWidth.ToString());
            xWriter.WriteAttributeString("height", display.ImageHeight.ToString());
            xWriter.WriteAttributeString("dpi", display.Dpi.ToString());
            if (_dpi != _map.Dpi)
            {
                xWriter.WriteAttributeString("scalesymbols", "true");
            }
            xWriter.WriteEndElement();

            if (_map.DisplayRotation != 0.0 && _rotatable)
            {
                xWriter.WriteStartElement("DISPLAYTRANSFORMATION");
                xWriter.WriteAttributeString("rotation", _map.DisplayRotation.ToString(_nfi));
                xWriter.WriteEndElement();
            }
            if (_cap_refscale == true && refScale > 0.1)
            {
                xWriter.WriteStartElement("DISPLAY");
                xWriter.WriteAttributeString("refscale", refScale.ToString(_nfi));
                xWriter.WriteEndElement();
            }

            if (!String.IsNullOrEmpty(_imageFormat))
            {
                xWriter.WriteStartElement("OUTPUT");
                xWriter.WriteAttributeString("type", _imageFormat);
                xWriter.WriteEndElement();
            }

            xWriter.WriteStartElement("BACKGROUND");
            xWriter.WriteAttributeString("color", "255,255,255");
            xWriter.WriteAttributeString("transcolor", "255,255,255");
            xWriter.WriteEndElement();

            if (_GET_IMAGE_Template != null)
            {
                XmlNode properties = _GET_IMAGE_Template.SelectSingleNode("ARCXML/REQUEST/GET_IMAGE/PROPERTIES");
                if (properties != null)
                {
                    xWriter.WriteRaw(properties.InnerXml);
                }
            }

            xWriter.WriteStartElement("LAYERLIST");

            bool hasVisibleLayers = false;
            foreach (Layer layer in _layers)
            {
                xWriter.WriteStartElement("LAYERDEF");

                xWriter.WriteAttributeString("id", layer.ID);
                bool visible = layer.Visible;

                int mins = (int)layer.MinScale,
                    maxs = (int)layer.MaxScale;
                if ((mins > 0) && (mins > Math.Round(mapScale + 0.5, 0))) { visible = false; }
                if ((maxs > 0) && (maxs < Math.Round(mapScale - 0.5, 0))) { visible = false; }
                xWriter.WriteAttributeString("visible", visible.ToString());

                if (visible)
                {
                    hasVisibleLayers = true;

                    #region Filter Query
                    string filterQuery = layer.Filter;
                    if (filterQuery != "")
                    {
                        xWriter.WriteRaw("<QUERY where=\"" +
                            WebGIS.CMS.Globals.EncUmlaute(WebGIS.CMS.Globals.EncodeXmlString(filterQuery), this.Umlaute2Wildcard) + "\" />");
                    }
                    #endregion

                    #region Renderer
                    if (layer is FeatureLayer && _cap_refscale == false)
                    {
                        string renderer = AxlHelper.addLabels2Renderer((FeatureLayer)layer);
                        if ((fontsizeFactor > 0.0 || widthFactor > 0.0) && visible && refScale > 0.1)
                        {
                            renderer = ModifyRenderer(renderer, (FeatureLayer)layer, fontsizeFactor, widthFactor);
                        }

                        if (renderer != ((FeatureLayer)layer).OriginalRenderer)
                        {
                            xWriter.WriteRaw(Types.Umlaute2Esri(renderer));
                        }
                    }
                    #endregion
                }

                xWriter.WriteEndElement(); // LAYERDEF
            }
            xWriter.WriteEndElement(); // LAYERLIST

            xWriter.WriteEndElement(); // PROPERTIES

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();

            #region Request
            string req = axl.ToString().Replace("&amp;", "&");
            // REQUEST verschicken
            int try_counter = 0;
            string resp = "";

            if (!hasVisibleLayers)
            {
                pLog.Success = pLog.SuppressLogging = true;
                return new EmptyImage(_map.Services.IndexOf(this), this.ID);
            }

            while (true)
            {
                //resp = await _connector.SendRequestAsync(req, _server, _service);
                resp = await httpService.SendAxlRequestAsync(_connectionProperties, req, _server, _service);
                if (String.IsNullOrEmpty(resp))
                {
                    resp = String.Empty;
                }

                if (resp.IndexOf("<?xml ") == 0)
                {
                    break;
                }

                if (resp.IndexOf("<?xml ") > 0 && resp.IndexOf("data update is in progress.") != -1)
                {
                    try_counter++;
                    if (try_counter > 5)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            #endregion

            #region Parse Response
            string imagePath = String.Empty;
            string imageUrl = String.Empty;

            if (resp.IndexOf("<ERROR") != -1)
            {
                if (resp.ToLower().Contains("timeout"))
                {
                    return LogException(requestContext, "GetMap", new TimeoutResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp)));
                }

                return LogException(requestContext, "GetMap", new ErrorResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp)));
            }

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(resp);
            }
            catch (Exception ex)
            {
                return new ExceptionResponse(
                    _map.Services.IndexOf(this),
                    this.ID,
                    new Exception("Xml:" + resp + "|" + ex.Message + "|" + ex.StackTrace));
            }

            XmlNode node = xmldoc.SelectSingleNode("//OUTPUT");
            if (node.Attributes["file"] != null)
            {
                imagePath = node.Attributes["file"].Value;
            }

            if (node.Attributes["url"] != null)
            {
                imageUrl = httpService.ApplyUrlOutputRedirection(node.Attributes["url"].Value);
            }

            // Bild nicht downloaden!!
            // Original Url verwnden!!
            #endregion

            //_connector.LogString("webgis4.log", "Finished Map Request: " + _service);

            if (_map.DisplayRotation != 0.0 && _rotatable == false)
            {
                //using (Bitmap sourceImg = (Bitmap)await _connector.GetImageAsync(node))
                using (var sourceImg = await httpService.GetAxlServiceImageAsync(_connectionProperties, node, this.OutputPath))
                using (var bm = Display.TransformImage(sourceImg, display, _map))
                {
                    string filename = "rot_" + Guid.NewGuid().ToString("N") + ".png";
                    imagePath = _map.AsOutputFilename(filename);
                    imageUrl = _map.AsOutputUrl(filename);
                    await bm.SaveOrUpload(imagePath, ImageFormat.Png);
                }
            }

            ErrorResponse innerErrorResponse = new ErrorResponse(this);
            if (this.Diagnostics != null && this.Diagnostics.ThrowExeption(this))
            {
                if (_diagnosticsErrorResponse != null)
                {
                    innerErrorResponse.AppendMessage(_diagnosticsErrorResponse);
                }
            }

            pLog.Success = true;
            return new ImageLocation(_map.Services.IndexOf(this), this.ID, imagePath, imageUrl)
            {
                InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null,
                Extent = new Envelope(_map.Extent),
                Scale = _map.MapScale
            };
        }
    }

    async public Task<ServiceResponse> GetSelectionAsync(SelectionCollection selections, IRequestContext requestContext)
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map?.Services != null ? this.Map.Services.IndexOf(this) : 0, this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        var httpService = requestContext.Http;

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "GetSelection", ""))
        {
            double mapScale = _map.MapScale;
            double refScale = this.RefScale;

            double widthFactor = -1.0, fontsizeFactor = -1.0;
            if (refScale > 0.1)
            {
                widthFactor = fontsizeFactor = refScale / Math.Max(mapScale, 1.0);
            }

            // REQUEST erzeugen
            StringBuilder axl = new StringBuilder();
            //StringWriter sw=new StringWriter(axl);
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

            //xWriter.Formatting=Formatting.Indented;
            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("GET_IMAGE");
            xWriter.WriteStartElement("PROPERTIES");

            AxlHelper.AXLaddFeatureCoordsys(ref xWriter, this.FeatureCoordsys);
            AxlHelper.AXLaddFilterCoordsys(ref xWriter, this.FilterCoordsys);

            IDisplay display = _map;
            if (_map.DisplayRotation != 0.0 && _rotatable == false)
            {
                display = Display.TransformedDisplay(_map);
            }

            xWriter.WriteStartElement("ENVELOPE");
            xWriter.WriteAttributeString("maxx", display.Extent.MaxX.ToString(_nfi));
            xWriter.WriteAttributeString("minx", display.Extent.MinX.ToString(_nfi));
            xWriter.WriteAttributeString("maxy", display.Extent.MaxY.ToString(_nfi));
            xWriter.WriteAttributeString("miny", display.Extent.MinY.ToString(_nfi));
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("IMAGESIZE");
            xWriter.WriteAttributeString("width", display.ImageWidth.ToString());
            xWriter.WriteAttributeString("height", display.ImageHeight.ToString());
            xWriter.WriteAttributeString("dpi", display.Dpi.ToString());

            if (_map.DisplayRotation != 0.0 && _rotatable)
            {
                xWriter.WriteStartElement("DISPLAYTRANSFORMATION");
                xWriter.WriteAttributeString("rotation", _map.DisplayRotation.ToString(_nfi));
                xWriter.WriteEndElement();
            }

            xWriter.WriteEndElement();

            xWriter.WriteStartElement("BACKGROUND");
            xWriter.WriteAttributeString("color", "255,255,255");
            xWriter.WriteAttributeString("transcolor", "255,255,255");
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("OUTPUT");
            xWriter.WriteAttributeString("type", "png");
            xWriter.WriteEndElement();

            if (_GET_IMAGE_Template != null)
            {
                XmlNode properties = _GET_IMAGE_Template.SelectSingleNode("ARCXML/REQUEST/GET_IMAGE/PROPERTIES");
                if (properties != null)
                {
                    xWriter.WriteRaw(properties.InnerXml);
                }
            }

            xWriter.WriteStartElement("LAYERLIST");

            foreach (Layer layer in _layers)
            {
                xWriter.WriteStartElement("LAYERDEF");

                xWriter.WriteAttributeString("id", layer.ID);
                bool visible = false;

                #region Beim ArcMapServer is alles anders
                if (_arcmapserver == true)
                {
                    // Beim ArcMap Server müssen die Layer die Selektiert werden Visible=true sein!!!!
                    foreach (Selection selection in selections)
                    {
                        if (selection.Layer == layer)
                        {
                            visible = true;
                            //visible = layer.Visible;
                            //if (_useToc && _map.Toc != null)
                            //{
                            //    ITocElement theme = _map.Toc.TocElements.FindById(_id + ":" + layer.ID);
                            //    if (theme != null)
                            //        visible = theme.Visible;
                            //    else
                            //        visible = false;
                            //}
                        }
                    }
                }
                #endregion

                xWriter.WriteAttributeString("visible", visible.ToString());
                xWriter.WriteEndElement(); // LAYERDEF
            }
            xWriter.WriteEndElement(); // LAYERLIST
            xWriter.WriteEndElement(); // PROPERTIES

            foreach (Selection selection in selections)
            {
                if (selection == null ||
                    selection.Filter == null ||
                    selection.Layer == null ||
                    !_layers.Contains(selection.Layer)
                    )
                {
                    continue;
                }

                string guid = System.Guid.NewGuid().ToString("N");
                if (selection is BufferSelection)
                {
                    if (selection.Filter.Buffer != null)
                    {
                        #region Buffer
                        // für Pufferdarstellung im AXL den Targetlayer nicht angeben
                        QueryFilter bQuery = selection.Filter.Clone();
                        bQuery.Where = WebGIS.CMS.Globals.EncUmlaute(bQuery.Where, _umlaute2wildcard);
                        bQuery.Buffer.TargetLayer = null;

                        xWriter.WriteStartElement("LAYER");
                        xWriter.WriteAttributeString("type", "featureclass");
                        xWriter.WriteAttributeString("name", "Buffer" + guid);
                        xWriter.WriteAttributeString("visible", "true");
                        xWriter.WriteAttributeString("id", "theBuffer" + guid);

                        xWriter.WriteStartElement("DATASET");
                        xWriter.WriteAttributeString("fromlayer", selection.Layer.ID);
                        xWriter.WriteEndElement();	// DATASET

                        //xWriter.WriteRaw(bQuery.ArcXML(_nfi));

                        string where = bQuery.Where.Replace("\"", "'");  // Abfragen aus GDB kommen mit " statt mit ' daher !!!
                        string filterExpression = selection.Layer.Filter.Trim();

                        if (!String.IsNullOrEmpty(filterExpression))
                        {
                            where = ((!String.IsNullOrEmpty(where)) ? "(" + where + ") AND " : "") + filterExpression;
                        }

                        bQuery.Where = WebGIS.CMS.Globals.EncUmlaute(where, this.Umlaute2Wildcard);
                        xWriter.WriteRaw(bQuery.ArcXML(_nfi));

                        AXLaddHighlightSymbol(ref xWriter, LayerType.polygon, AxlHelper.ColorToString((ArgbColor)_map.Environment.UserValue(webgisConst.BufferColor, ArgbColor.Gray)), "fdiagonal", 0.6);

                        //xWriter.WriteStartElement("SIMPLERENDERER");
                        //xWriter.WriteStartElement("SIMPLEPOLYGONSYMBOL");
                        //xWriter.WriteAttributeString("transparency", "0,6");
                        //xWriter.WriteAttributeString("fillcolor", "100,100,100");
                        //xWriter.WriteEndElement(); // SIMPLEPOLYGONSYMBOL
                        //xWriter.WriteEndElement(); // SIMPLERENDERER
                        xWriter.WriteEndElement(); // LAYER
                        #endregion

                        #region Buffer Selection
                        if (selection.Filter.Buffer.TargetLayer != null &&
                            _layers.Contains(selection.Filter.Buffer.TargetLayer))
                        {
                            QueryFilter filter = selection.Filter.Clone();
                            filter.Where = WebGIS.CMS.Globals.EncUmlaute(filter.Where, _umlaute2wildcard);

                            xWriter.WriteStartElement("LAYER");
                            xWriter.WriteAttributeString("type", "featureclass");
                            xWriter.WriteAttributeString("name", "BufferSel " + guid);
                            xWriter.WriteAttributeString("visible", "true");
                            xWriter.WriteAttributeString("id", "theBufferSel" + guid);

                            xWriter.WriteStartElement("DATASET");
                            xWriter.WriteAttributeString("fromlayer", selection.Layer.ID);
                            xWriter.WriteEndElement();	// DATASET

                            //xWriter.WriteRaw(filter.ArcXML(_nfi));

                            where = filter.Where.Replace("\"", "'");  // Abfragen aus GDB kommen mit " statt mit ' daher !!!
                            filterExpression = selection.Layer.Filter.Trim();

                            if (!String.IsNullOrEmpty(filterExpression))
                            {
                                where = ((!String.IsNullOrEmpty(where)) ? "(" + where + ") AND " : "") + filterExpression;
                            }

                            filter.Where = WebGIS.CMS.Globals.EncUmlaute(where, this.Umlaute2Wildcard);
                            xWriter.WriteRaw(filter.ArcXML(_nfi));

                            AXLaddHighlightSymbol(ref xWriter, selection.Filter.Buffer.TargetLayer.Type, AxlHelper.ColorToString(selection.Color), "bdiagonal", 1.0);

                            xWriter.WriteEndElement(); // LAYER
                        }
                        #endregion
                    }
                }
                else // einfache Selection/Hightlight
                {
                    #region Selection
                    QueryFilter filter = selection.Filter.Clone();
                    filter.Where = WebGIS.CMS.Globals.EncUmlaute(filter.Where, _umlaute2wildcard);

                    xWriter.WriteStartElement("LAYER");
                    xWriter.WriteAttributeString("type", "featureclass");
                    xWriter.WriteAttributeString("name", "theSelection");
                    xWriter.WriteAttributeString("visible", "true");
                    xWriter.WriteAttributeString("id", "theSelection");

                    xWriter.WriteStartElement("DATASET");
                    xWriter.WriteAttributeString("fromlayer", selection.Layer.ID);
                    xWriter.WriteEndElement();	// DATASET

                    //xWriter.WriteRaw(filter.ArcXML(_nfi));

                    string where = filter.Where.Replace("\"", "'");  // Abfragen aus GDB kommen mit " statt mit ' daher !!!
                    string filterExpression = selection.Layer.Filter.Trim();

                    if (!String.IsNullOrEmpty(filterExpression))
                    {
                        where = ((!String.IsNullOrEmpty(where)) ? "(" + where + ") AND " : "") + filterExpression;
                    }

                    filter.Where = WebGIS.CMS.Globals.EncUmlaute(where, this.Umlaute2Wildcard);

                    if (this.Is_gView)
                    {
                        if (filter is SpatialFilter && ((SpatialFilter)filter).QueryShape != null &&
                            ((SpatialFilter)filter).QueryShape.Buffer != null && ((SpatialFilter)filter).QueryShape.Buffer.BufferDistance != 0.0)
                        {
                            using (var cts = new CancellationTokenSource())
                            {
                                ((SpatialFilter)filter).QueryShape = ((SpatialFilter)filter).QueryShape.CalcBuffer(
                                                                        ((SpatialFilter)filter).QueryShape.Buffer.BufferDistance,
                                                                        cts);
                            }
                        }
                    }

                    xWriter.WriteRaw(filter.ArcXML(_nfi));

                    var layerType = selection.Layer.Type;
                    // Problem: bei MSSQL-Spatial hat die Geometriespalte keinen fixen Geometrietyp (anders als bei bspw. Postgres)
                    // Workaround: falls Geometrietyp == unknown => Feature selbst abfragen und aus dessen Geometrie bestimmen
                    // bei uns sollte immer der gleiche Geometrietyp je Spalte sein.
                    if (layerType == LayerType.unknown)
                    {
                        FeatureCollection features = new FeatureCollection();
                        var tempFilter = selection.Filter.Clone();
                        tempFilter.QueryGeometry = true;
                        tempFilter.SubFields = selection.Layer.ShapeFieldName;
                        await selection.Layer.GetFeaturesAsync(tempFilter, features, requestContext);
                        if (features.Count > 0)
                        {
                            if (features[0].Shape is Core.Geometry.Point)
                            {
                                layerType = LayerType.point;
                            }

                            if (features[0].Shape is Core.Geometry.Polyline)
                            {
                                layerType = LayerType.line;
                            }

                            if (features[0].Shape is Core.Geometry.Polygon)
                            {
                                layerType = LayerType.polygon;
                            }
                        }
                    }

                    AXLaddHighlightSymbol(ref xWriter, layerType, AxlHelper.ColorToString(selection.Color), "bdiagonal", 1.0);
                    xWriter.WriteEndElement(); // LAYER
                    #endregion
                }
            }

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();

            #region Request
            string req = axl.ToString().Replace("&amp;", "&");
            // REQUEST verschicken
            int try_counter = 0;
            string resp = "";

            while (true)
            {
                //resp = await _connector.SendRequestAsync(req, _server, _service);
                resp = await httpService.SendAxlRequestAsync(_connectionProperties, req, _server, _service);
                if (resp.IndexOf("<?xml ") == 0)
                {
                    break;
                }

                if (resp.IndexOf("<?xml ") > 0 && resp.IndexOf("data update is in progress.") != -1)
                {
                    try_counter++;
                    if (try_counter > 5)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            #endregion

            #region Parse Response
            string imagePath = String.Empty;
            string imageUrl = String.Empty;

            if (resp.IndexOf("<ERROR") != -1)
            {
                if (resp.ToLower().Contains("timeout"))
                {
                    return LogException(requestContext, "GetSelection", new TimeoutResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp)));
                }

                return LogException(requestContext, "GetSelection", new ErrorResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp)));
            }

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(resp);
            }
            catch (Exception ex)
            {
                return new ExceptionResponse(
                    _map.Services.IndexOf(this),
                    this.ID,
                    new Exception("Xml:" + resp + "|" + ex.Message + "|" + ex.StackTrace));
            }

            XmlNode node = xmldoc.SelectSingleNode("//OUTPUT");
            if (node.Attributes["file"] != null)
            {
                imagePath = node.Attributes["file"].Value;
            }

            if (node.Attributes["url"] != null)
            {
                imageUrl = httpService.ApplyUrlOutputRedirection(node.Attributes["url"].Value);
            }



            // Bild nicht downloaden!!
            // Original Url verwnden!!
            // ausser für ArcMapServer!!!
            if (_arcmapserver == true)
            {
                //using (Bitmap bm = dotNETConnector.DownloadImage(imageUrl, _connector.GetProxy(imageUrl)) /* ASHelper.DownloadImage(mapImg.ImageURL)*/)
                using (var bm = await httpService.GetImageAsync(imageUrl))
                {
                    if (bm != null)
                    {
                        ArcServer.ASHelper.CleanSelectionBitmap(bm, ArgbColor.Cyan);
                        string title = "sel_" + Guid.NewGuid().ToString("N") + ".png";
                        await bm.SaveOrUpload(_map.AsOutputFilename(title), ImageFormat.Png);

                        pLogger.Success = true;
                        return new ImageLocation(
                                            _map.Services.IndexOf(this),
                                            this.ID,
                                            _map.AsOutputFilename(title),
                                            _map.AsOutputUrl(title))
                        {
                            Extent = new Envelope(_map.Extent),
                            Scale = _map.MapScale
                        };
                    }
                }

            }
            #endregion

            if (_map.DisplayRotation != 0.0 && _rotatable == false)
            {
                //using (Bitmap sourceImg = (Bitmap)await _connector.GetImageAsync(node))
                using (var sourceImg = await httpService.GetAxlServiceImageAsync(_connectionProperties, node, this.OutputPath))
                using (var bm = Display.TransformImage(sourceImg, display, _map))
                {
                    string filename = "rot_" + Guid.NewGuid().ToString("N") + ".png";
                    imagePath = _map.AsOutputFilename(filename);
                    imageUrl = _map.AsOutputUrl(filename);
                    await bm.SaveOrUpload(imagePath, ImageFormat.Png);
                }
            }

            //_connector.LogString("webgis4.log", "Finished Map Request: " + _service);
            pLogger.Success = true;
            return new ImageLocation(_map.Services.IndexOf(this), this.ID, imagePath, imageUrl)
            {
                Extent = new Envelope(_map.Extent),
                Scale = _map.MapScale
            };
        }
    }

    public int Timeout
    {
        get { return _timeout; }
        set
        {
            _timeout = value;
            if (_connectionProperties != null)
            {
                _connectionProperties.Timeout = value;
            }
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

    public string BasemapPreviewImage { get; set; }

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

    private int[] _supportedCrs = null;
    public int[] SupportedCrs
    {
        get { return _supportedCrs; }
        set { _supportedCrs = value; }
    }

    #endregion

    #region Properties
    private string _featureCoordsys = String.Empty;
    public string FeatureCoordsys
    {
        get { return _featureCoordsys; }
        set { _featureCoordsys = value; }
    }

    private string _filterCoordsys = String.Empty;
    public string FilterCoordsys
    {
        get { return _filterCoordsys; }
        set { _filterCoordsys = value; }
    }

    private readonly XmlNode _GET_IMAGE_Template = null;

    public Encoding Encoding
    {
        get { return _encoding; }
        set { _encoding = value; }
    }
    public bool ExtraCheckUmlaute
    {
        get { return _extraCheckUmlaut; }
        set
        {
            _extraCheckUmlaut = value;
            if (_connectionProperties != null)
            {
                _connectionProperties.CheckUmlaut = _extraCheckUmlaut;
            }
        }
    }
    public bool Umlaute2Wildcard
    {
        get { return _umlaute2wildcard; }
        set { _umlaute2wildcard = value; }
    }

    double _selectionFillTransparency = 0.5;
    public double selectionFillTransparency
    {
        get { return _selectionFillTransparency; }
        set
        {
            _selectionFillTransparency = value;
        }
    }
    #endregion

    #region Helpers
    private bool hasErrorMessage(string response)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);

            XmlNodeList error = doc.GetElementsByTagName("ERROR");
            if (error.Count == 0)
            {
                return false;
            }

            _errMsg = error[0].InnerText;
        }
        catch (Exception e)
        {
            _errMsg = e.Source + "\n" + e.Message;
        }
        return true;
    }
    private string getRendererFromAXLFile(XmlNode layerNode)
    {
        try
        {
            if (layerNode.SelectNodes("SIMPLERENDERER").Count > 0)
            {
                return layerNode.SelectNodes("SIMPLERENDERER")[0].OuterXml;
            }
            else if (layerNode.SelectNodes("GROUPRENDERER").Count > 0)
            {
                return layerNode.SelectNodes("GROUPRENDERER")[0].OuterXml;
            }
            else if (layerNode.SelectNodes("VALUEMAPRENDERER").Count > 0)
            {
                return layerNode.SelectNodes("VALUEMAPRENDERER")[0].OuterXml;
            }
            else if (layerNode.SelectNodes("VALUEMAPLABELRENDERER").Count > 0)
            {
                return layerNode.SelectNodes("VALUEMAPLABELRENDERER")[0].OuterXml;
            }
            else if (layerNode.SelectNodes("SCALEDEPENDENTRENDERER").Count > 0)
            {
                return layerNode.SelectNodes("SCALEDEPENDENTRENDERER")[0].OuterXml;
            }
        }
        catch (Exception e)
        {
            string msg = e.Message;
        }
        return "";
    }
    protected XmlNode getParentXmlNode(XmlNode node, int maxChildNodes)
    {
        if (node.ParentNode == null)
        {
            return node;
        }

        if (node.ParentNode.ChildNodes == null)
        {
            return node;
        }

        if (node.ParentNode.ChildNodes.Count > maxChildNodes)
        {
            return node;
        }

        return getParentXmlNode(node.ParentNode, maxChildNodes);
    }
    private void splitLabelRendererFromGrouprenderer(FeatureLayer layer)
    {
        try
        {
            XmlTextReader render = new XmlTextReader(layer.Renderer, XmlNodeType.Element, null);
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(render);

            XmlNodeList label = xmldoc.SelectNodes("//SIMPLELABELRENDERER");
            if (label.Count != 1)
            {
                layer.LabelRenderer = null;
                layer.UseLabelRenderer = false;
            }
            else
            {
                XmlNode labelnode = getParentXmlNode(label[0], 1);

                layer.LabelRenderer = labelnode;
                layer.UseLabelRenderer = true;
                layer.Renderer = xmldoc.OuterXml.Replace(labelnode.OuterXml, "");
            }
        }
        catch
        {
            layer.LabelRenderer = null;
            layer.UseLabelRenderer = false;
        }
    }
    private void SetInitialExtent(XmlNode envelope)
    {
        if (envelope == null ||
            envelope.Attributes["minx"] == null ||
            envelope.Attributes["miny"] == null ||
            envelope.Attributes["maxx"] == null ||
            envelope.Attributes["maxy"] == null)
        {
            return;
        }

        _initialExtent = new Envelope(
            Shape.Convert(envelope.Attributes["minx"].Value, _nfi),
            Shape.Convert(envelope.Attributes["miny"].Value, _nfi),
            Shape.Convert(envelope.Attributes["maxx"].Value, _nfi),
            Shape.Convert(envelope.Attributes["maxy"].Value, _nfi));

    }

    private FieldType GetFieldType(XmlNode fieldNode)
    {
        if (fieldNode.Attributes["type"].Value == "5" && (fieldNode.Attributes["size"] != null && fieldNode.Attributes["size"].Value == "1"))
        {
            return FieldType.Boolean;  // Sql Bit Type wird zu Int16 mit Size=1!!!
        }

        return (FieldType)int.Parse(fieldNode.Attributes["type"].Value);
    }

    #region AXL Renderer
    async private Task<string> ThinRendererAsync(IHttpService httpService, FeatureLayer layer, string renderType, string renderer)
    {
        string ret = renderer;
        if (ret == "")
        {
            return "";
        }

        XmlTextReader xml = new XmlTextReader(ret, XmlNodeType.Element, null);
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(xml);

        XmlNodeList vmr;
        if (renderer.IndexOf("<GROUPRENDERER") != -1)
        {
            vmr = xmldoc.SelectNodes("//" + renderType);
        }
        else
        {
            vmr = xmldoc.SelectNodes(renderType);
        }

        if (vmr == null)
        {
            return "";
        }

        if (vmr.Count == 0)
        {
            return "";
        }

        for (int i = 0; i < vmr.Count; i++)
        {
            string lookupfield = vmr[i].Attributes["lookupfield"].Value.ToString();
            XmlNodeList exacts = vmr[i].SelectNodes("EXACT");
            foreach (XmlNode exact in exacts)
            {
                string val = exact.Attributes["value"].Value.ToString();
                string where = lookupfield + "='" + val + "'";

                if (!await hasLayerFeaturesInVisibleEnvAsync(httpService, layer.ID, where))
                {
                    ret = ret.Replace(exact.OuterXml, "");
                }
            }
        }
        return ret;
    }
    async private Task<string> ThinRendererAsync(IHttpService httpService, FeatureLayer layer)
    {
        string renderer = layer.Renderer;
        /*
        int pos1=m_renderer[index].ToString().IndexOf("<VALUEMAPRENDERER"),
            pos2=m_renderer[index].ToString().IndexOf("<VALUEMAPLABELRENDERER");

        if(pos1==-1 && pos2==-1) return "";

        if(pos1!=-1) renderer=thinRenderer(index,"VALUEMAPRENDERER",renderer);
        if(pos2!=-1) renderer=thinRenderer(index,"VALUEMAPLABELRENDERER",renderer);
			
        return renderer;
        */

        if (renderer == "")
        {
            return "";
        }

        XmlTextReader xml = new XmlTextReader(renderer, XmlNodeType.Element, null);
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(xml);

        bool modified = await ThinRendererAsync(httpService, xmldoc.ChildNodes, layer, false);

        if (modified)
        {
            return xmldoc.OuterXml;
        }

        return "";
    }
    async private Task<bool> ThinRendererAsync(IHttpService httpService, XmlNodeList nl, Layer layer, bool modified)
    {
        foreach (XmlNode xn in nl)
        {
            if (xn.Name == "VALUEMAPRENDERER" || xn.Name == "VALUEMAPLABELRENDERER")
            {
                string lookupfield = xn.Attributes["lookupfield"].Value.ToString();
                List<string> attrval = await GetValuesOfVisualFeaturesAsync(httpService, layer, lookupfield);

                XmlNodeList exacts = xn.SelectNodes("EXACT");
                foreach (XmlNode exact in exacts)
                {
                    string val = exact.Attributes["value"].Value.ToString();
                    bool found = attrval.IndexOf(val) != -1;
                    if (!found && val.Contains(";"))
                    {
                        foreach (string val_ in val.Split(';'))
                        {
                            if (attrval.IndexOf(val_.Trim()) != -1)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        xn.RemoveChild(exact);
                        modified = true;
                    }
                }
            }
            else
            {
                if (xn.HasChildNodes)
                {
                    modified = await ThinRendererAsync(httpService, xn.ChildNodes, layer, modified);
                }
            }
        }

        return modified;
    }
    private string ModifyRenderer(string renderer, FeatureLayer layer, double fontsizeFactor, double widthFactor)
    {
        string input = renderer;
        if (String.IsNullOrEmpty(renderer))
        {
            renderer = layer.Renderer;
        }

        if (String.IsNullOrEmpty(renderer))
        {
            return String.Empty;
        }

        XmlTextReader xml = new XmlTextReader(renderer, XmlNodeType.Element, null);
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(xml);

        bool modified = false;
        ModifyRenderer(xmldoc.ChildNodes, layer.Opacity, fontsizeFactor, widthFactor, ref modified);

        if (modified)
        {
            return xmldoc.OuterXml;
        }

        return input;
    }
    private void ModifyRenderer(XmlNodeList nl, double opacity, double fontsizeFactor, double widthFactor, ref bool modified)
    {
        foreach (XmlNode xn in nl)
        {
            if (opacity >= 0.0)
            {
                switch (xn.Name)
                {
                    case "SIMPLELINESYMBOL":
                    case "SIMPLEMARKERSYMBOL":
                    case "TEXTMARKERSYMBOL":
                    case "TEXTSYMBOL":
                    case "TRUETYPEMARKERSYMBOL":
                        try
                        {
                            if (xn.Attributes["transparency"] == null)
                            {
                                if (opacity >= 99.0)
                                {
                                    break;
                                }

                                xn.Attributes.Append(xn.OwnerDocument.CreateAttribute("transparency"));
                            }
                        }
                        catch { }
                        if (xn.Attributes["transparency"] != null)
                        {
                            xn.Attributes["transparency"].Value = (opacity / 100.0).ToString(_nfi);
                            modified = true;
                        }
                        break;
                    case "SIMPLEPOLYGONSYMBOL":
                        try
                        {
                            if (xn.Attributes["filltransparency"] == null)
                            {
                                if (opacity >= 99.0)
                                {
                                    break;
                                }

                                xn.Attributes.Append(xn.OwnerDocument.CreateAttribute("filltransparency"));
                            }
                        }
                        catch { }
                        if (xn.Attributes["filltransparency"] != null)
                        {
                            xn.Attributes["filltransparency"].Value = (opacity / 100.0).ToString(_nfi);
                            modified = true;
                        }
                        break;
                }
            }
            if (fontsizeFactor > 0.0)
            {
                if (xn.Attributes["fontsize"] != null)
                {
                    string attVal = xn.Attributes["fontsize"].Value;
                    NumberFormatInfo nfi = attVal.Contains(".") ? null : _nfi;

                    double val = Shape.Convert(attVal, nfi);
                    val *= fontsizeFactor;
                    if (val < 1.0)
                    {
                        val = 1.0;
                    }

                    val = Math.Round(val, 3);
                    modified = true;
                    xn.Attributes["fontsize"].Value = nfi != null ? val.ToString(nfi) : val.ToPlatformNumberString();
                }
            }
            if (widthFactor > 0.0)
            {
                if (xn.Attributes["width"] != null)
                {
                    string attVal = xn.Attributes["width"].Value;
                    NumberFormatInfo nfi = attVal.Contains(".") ? null : _nfi;

                    double val = Shape.Convert(attVal, nfi);
                    val *= widthFactor;
                    if (val < 0.0)
                    {
                        val = 0.0;
                    }

                    val = Math.Round(val, 3);
                    modified = true;
                    xn.Attributes["width"].Value = nfi != null ? val.ToString(nfi) : val.ToPlatformNumberString();
                }
                if (xn.Attributes["hotspot"] != null)
                {
                    string[] xy = xn.Attributes["hotspot"].Value.Split(',');
                    if (xy.Length == 2)
                    {
                        //double x = Convert.ToDouble(xy[0].Replace(".", ",")),
                        //       y = Convert.ToDouble(xy[1].Replace(".", ","));
                        double x = xy[0].ToPlatformDouble(),
                               y = xy[1].ToPlatformDouble();
                        x *= widthFactor;
                        y *= widthFactor;
                        x = Math.Round(x, 5);
                        y = Math.Round(y, 5);
                        modified = true;
                        xn.Attributes["hotspot"].Value =
                            x.ToString().Replace(",", ".") + "," +
                            y.ToString().Replace(",", ".");
                    }
                }
            }
            if (xn.HasChildNodes)
            {
                ModifyRenderer(xn.ChildNodes, opacity, fontsizeFactor, widthFactor, ref modified);
            }
        }
    }

    protected void AXLaddHighlightSymbol(ref XmlTextWriter xWriter, LayerType type, string color, string filltype, double trans)
    {
        xWriter.WriteStartElement("SIMPLERENDERER");
        switch (type)
        {
            case LayerType.polygon:
                if (_SelectionRenderers_Template != null &&
                    _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/PolygonAXL") != null)
                {
                    xWriter.WriteRaw(
                        ReplaceInHighlightSymbol(
                        _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/PolygonAXL").InnerXml, color, trans));
                }
                else
                {
                    xWriter.WriteStartElement("SIMPLEPOLYGONSYMBOL");
                    if (selectionFillTransparency == 0.0)
                    {
                        //xWriter.WriteAttributeString("boundarycolor",color);
                        xWriter.WriteAttributeString("boundarywidth", "10");
                    }
                    xWriter.WriteAttributeString("boundarycolor", color);
                    xWriter.WriteAttributeString("boundarywidth", "2");
                    xWriter.WriteAttributeString("boundarytransparency", trans.ToString(_nfi));
                    xWriter.WriteAttributeString("fillcolor", color);
                    xWriter.WriteAttributeString("filltype", filltype);
                    xWriter.WriteAttributeString("filltransparency", selectionFillTransparency.ToString(_nfi));
                    xWriter.WriteEndElement(); // SIMPLEPOLYGONSYMBOL
                }
                break;
            case LayerType.line:
                if (_SelectionRenderers_Template != null &&
                    _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/LineAXL") != null)
                {
                    xWriter.WriteRaw(
                        ReplaceInHighlightSymbol(
                        _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/LineAXL").InnerXml, color, trans));
                }
                else
                {
                    xWriter.WriteStartElement("SIMPLELINESYMBOL");
                    xWriter.WriteAttributeString("transparency", trans.ToString(_nfi));
                    xWriter.WriteAttributeString("color", color);
                    xWriter.WriteAttributeString("width", "10");
                    xWriter.WriteEndElement(); // SIMPLELINESYMBOL
                }
                break;
            default: // point
                if (_SelectionRenderers_Template != null &&
                    _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/PointAXL") != null)
                {
                    xWriter.WriteRaw(
                        ReplaceInHighlightSymbol(
                        _SelectionRenderers_Template.SelectSingleNode("SelectionRenderers/PointAXL").InnerXml, color, trans));
                }
                else
                {
                    xWriter.WriteStartElement("SIMPLEMARKERSYMBOL");
                    xWriter.WriteAttributeString("transparency", trans.ToString(_nfi));
                    xWriter.WriteAttributeString("color", color);
                    xWriter.WriteAttributeString("width", "20");
                    xWriter.WriteEndElement(); // SIMPLEMARKERSYMBOL
                }
                break;
        }
        xWriter.WriteEndElement();
    }
    private string ReplaceInHighlightSymbol(string symbol, string color, double trans)
    {
        return symbol.Replace("[COLOR]", color).Replace("[TRANS]", trans.ToString(_nfi)).Replace("[FILLTRANS]", selectionFillTransparency.ToString(_nfi));
    }
    #endregion

    #region AXL
    async private Task<bool> hasLayerFeaturesInVisibleEnvAsync(IHttpService httpService, string id)
    {
        return await hasLayerFeaturesInVisibleEnvAsync(httpService, id, "");
    }
    async private Task<bool> hasLayerFeaturesInVisibleEnvAsync(IHttpService httpService, string id, string where)
    {
        // REQUEST erzeugen
        StringBuilder axl = new StringBuilder();
        //StringWriter sw=new StringWriter(axl);
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

        xWriter.WriteStartDocument();
        xWriter.WriteStartElement("ARCXML");
        xWriter.WriteAttributeString("version", "1.1");
        xWriter.WriteStartElement("REQUEST");
        xWriter.WriteStartElement("GET_FEATURES");
        xWriter.WriteAttributeString("outputmode", "newxml");
        xWriter.WriteAttributeString("geometry", "false");
        xWriter.WriteAttributeString("envelope", "false");
        xWriter.WriteAttributeString("featurelimit", "1");
        xWriter.WriteAttributeString("beginrecord", "0");

        xWriter.WriteStartElement("LAYER");
        xWriter.WriteAttributeString("id", id);
        xWriter.WriteEndElement();

        xWriter.WriteStartElement("SPATIALQUERY");
        xWriter.WriteAttributeString("subfields", "#SHAPE#");
        if (where != "")
        {
            //
            // Achtung sonderzeichen sind noch nicht berücksichtigt
            // könnte zu Fehlern in der Legende führen...
            //
            xWriter.WriteAttributeString("where", where);
        }

        AxlHelper.AXLaddFeatureCoordsys(ref xWriter, this.FeatureCoordsys);
        AxlHelper.AXLaddFilterCoordsys(ref xWriter, this.FilterCoordsys);

        xWriter.WriteStartElement("SPATIALFILTER");
        xWriter.WriteAttributeString("relation", "area_intersection");

        xWriter.WriteStartElement("ENVELOPE");
        xWriter.WriteAttributeString("minx", _map.Extent.MinX.ToString(_nfi));
        xWriter.WriteAttributeString("maxx", _map.Extent.MaxX.ToString(_nfi));
        xWriter.WriteAttributeString("miny", _map.Extent.MinY.ToString(_nfi));
        xWriter.WriteAttributeString("maxy", _map.Extent.MaxY.ToString(_nfi));

        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        axl.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();
        xWriter.Close();

        // REQUEST verschicken
        /*
        aims.ArcIMSConnector connector=new aims.ArcIMSConnectorClass();
        connector.ServerName=m_server;
        connector.ServerPort=m_port;
        connector.SendAxlRequest(m_serviceName+"&CustomService=Query",axl.ToString());
        string resp=connector.ResponseAXL;
        connector=null;
        */
        // resp = await _connector.SendRequestAsync(axl, _server, _service, "Query");
        string resp = await httpService.SendAxlRequestAsync(_connectionProperties, axl.ToString(), _server, _service, "Query");

        //StringBuilder sb=new StringBuilder();
        //XmlTextReader xml=new XmlTextReader(resp,XmlNodeType.Element,null);
        try
        {
            XmlDocument xmldoc = new XmlDocument();
            // beim Laden können fehler auftreten, wenn im in Ergebnisfeldern < > auftritt 
            // zb. Einwohner < 5000
            xmldoc.LoadXml(resp);
            //xmldoc.Load(xml);

            XmlNodeList node = xmldoc.GetElementsByTagName("FEATURECOUNT");

            if (node == null)
            {
                return false;
            }

            int count = Convert.ToInt16(node[0].Attributes["count"].Value);
            if (count > 0)
            {
                return true;
            }
        }
        catch { }
        return false;
    }
    async private Task<List<string>> GetValuesOfVisualFeaturesAsync(IHttpService httpService, Layer layer, string field)
    {
        List<string> attr = new List<string>();

        int begin = 1;
        bool hasmore = false;

        do
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(await QueryFeaturesInExtentAsync(httpService, layer, begin, 500, field));
                XmlNodeList node = doc.GetElementsByTagName("FEATURECOUNT");
                if (node.Count == 0)
                {
                    break;
                }

                hasmore = Convert.ToBoolean(node[0].Attributes["hasmore"].Value);

                node = doc.SelectNodes("//FEATURE");
                foreach (XmlNode feature in node)
                {
                    string val = Globals.getFieldValue(feature, field);
                    if (attr.IndexOf(val) != -1)
                    {
                        continue;
                    }

                    attr.Add(val);
                }
                begin += 500;
            }
            catch
            {
                break;
            }
        } while (hasmore);

        return attr;
    }
    async private Task<string> QueryFeaturesInExtentAsync(IHttpService httpService, Layer layer, int beginrecord, int featuremax, string fields)
    {
        // REQUEST erzeugen
        StringBuilder axl = new StringBuilder();
        //StringWriter sw=new StringWriter(axl);
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

        xWriter.WriteStartDocument();
        xWriter.WriteStartElement("ARCXML");
        xWriter.WriteAttributeString("version", "1.1");
        xWriter.WriteStartElement("REQUEST");
        xWriter.WriteStartElement("GET_FEATURES");
        xWriter.WriteAttributeString("outputmode", "newxml");
        xWriter.WriteAttributeString("geometry", "false");
        xWriter.WriteAttributeString("envelope", "false");
        xWriter.WriteAttributeString("featurelimit", featuremax.ToString());
        xWriter.WriteAttributeString("beginrecord", beginrecord.ToString());
        xWriter.WriteAttributeString("checkesc", "true");

        xWriter.WriteStartElement("LAYER");
        xWriter.WriteAttributeString("id", layer.ID);
        xWriter.WriteEndElement();

        xWriter.WriteStartElement("SPATIALQUERY");
        xWriter.WriteAttributeString("subfields", fields);

        string filter = layer.Filter;
        //if (filter != "")
        //    filter += ((filter != "") ? " AND " : "") + filter;
        if (filter != "")
        {
            xWriter.WriteAttributeString("where", filter);
        }


        AxlHelper.AXLaddFeatureCoordsys(ref xWriter, this.FeatureCoordsys);
        AxlHelper.AXLaddFilterCoordsys(ref xWriter, this.FilterCoordsys);

        xWriter.WriteStartElement("SPATIALFILTER");
        xWriter.WriteAttributeString("relation", "area_intersection");

        xWriter.WriteStartElement("ENVELOPE");
        xWriter.WriteAttributeString("minx", _map.Extent.MinX.ToString(_nfi));
        xWriter.WriteAttributeString("maxx", _map.Extent.MaxX.ToString(_nfi));
        xWriter.WriteAttributeString("miny", _map.Extent.MinY.ToString(_nfi));
        xWriter.WriteAttributeString("maxy", _map.Extent.MaxY.ToString(_nfi));

        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        axl.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();
        xWriter.Close();

        // REQUEST verschicken
        /*
        aims.ArcIMSConnector connector=new aims.ArcIMSConnectorClass();
        connector.ServerName=m_server;
        connector.ServerPort=m_port;
        connector.SendAxlRequest(m_serviceName+"&CustomService=Query",axl.ToString());
        string resp=connector.ResponseAXL;
        connector=null;
        */
        //string resp = await _connector.SendRequestAsync(axl, _server, _service, "Query");
        string resp = await httpService.SendAxlRequestAsync(_connectionProperties, axl.ToString(), _server, _service, "Query");

        return resp;
    }

    #endregion

    private string ParseError(string xml)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode errNode = doc.SelectSingleNode("//ERROR");
            if (errNode == null)
            {
                return "Invalid error response";
            }

            StringBuilder sb = new StringBuilder();
            if (errNode.Attributes["message"] != null)
            {
                sb.Append(errNode.Attributes["message"].Value);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return ex.Message + " " + ex.StackTrace;
        }
    }
    private string ParseError2(string xml)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode errNode = doc.SelectSingleNode("//ERROR");
            if (errNode == null)
            {
                return "Invalid error response";
            }

            StringBuilder sb = new StringBuilder();
            if (errNode.Attributes["machine"] != null)
            {
                sb.Append("Machine:" + errNode.Attributes["machine"].Value);
            }

            if (errNode.Attributes["service"] != null)
            {
                sb.Append("|Service:" + errNode.Attributes["service"].Value);
            }

            if (errNode.Attributes["processid"] != null)
            {
                sb.Append("|ProcessId:" + errNode.Attributes["processid"].Value);
            }

            if (errNode.Attributes["threadid"] != null)
            {
                sb.Append("|ThreadId:" + errNode.Attributes["threadid"].Value);
            }

            sb.Append("|" + errNode.InnerText);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return ex.Message + " " + ex.StackTrace;
        }
    }

    private ErrorResponse LogException(IRequestContext requestContet, string command, ErrorResponse resp)
    {
        if (resp == null || _map == null)
        {
            return resp;
        }

        string msg = $"Server : {_server}\r\nService: {_service}\r\n{resp.ErrorMessage.Replace("|", "\r\n")}\r\n{resp.ErrorMessage2.Replace("|", "\r\n")}";

        requestContet.GetRequiredService<IExceptionLogger>()
            .LogException(_map, this.Server, this.Service, command, new Exception(msg));

        return resp;
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
        AxlService clone = new AxlService();

        clone._map = parent ?? _map;

        clone._id = _id;
        clone._server = _server;
        clone._service = _service;
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
        clone._SelectionRenderers_Template = _SelectionRenderers_Template;
        clone._isDirty = _isDirty;
        clone._opacity = _opacity;
        clone.OpacityFactor = this.OpacityFactor;
        clone._dpi = _dpi;
        clone._errMsg = _errMsg;
        clone._encoding = _encoding;
        clone._extraCheckUmlaut = _extraCheckUmlaut;
        clone._imageFormat = _imageFormat;

        clone._legendVisible = _legendVisible;
        clone._showServiceLegendInMap = _showServiceLegendInMap;
        clone._legendOptMethod = _legendOptMethod;
        clone._legendOptSymbolScale = _legendOptSymbolScale;
        clone._fixLegendUrl = _fixLegendUrl;

        clone._minScale = _minScale;
        clone._maxScale = _maxScale;
        clone._opacity = _opacity;
        clone.ShowInToc = this.ShowInToc;

        clone._featureCoordsys = _featureCoordsys;
        clone._filterCoordsys = _filterCoordsys;
        clone._projMethode = _projMethode;
        clone._projId = _projId;

        clone._arcmapserver = _arcmapserver;
        clone._culture = _culture;
        clone._nfi = _nfi;

        clone._checkSpatialConstraints = _checkSpatialConstraints;
        clone._collectionId = _collectionId;
        clone._url = _url;

        clone._useFixRefScale = _useFixRefScale;
        clone._fixRefScale = _fixRefScale;
        clone._rotatable = _rotatable;
        clone._cap_refscale = _cap_refscale;

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

        clone.LayerProperties = this.LayerProperties;

        clone._connectionProperties = this._connectionProperties;

        clone.CreatePresentationsDynamic = this.CreatePresentationsDynamic;
        clone.CreateQueriesDynamic = this.CreateQueriesDynamic;
        clone.ImageServiceType = this.ImageServiceType;

        clone.ServiceThemes = this.ServiceThemes;

        return clone;
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
        if (!_legendVisible)
        {
            return new ServiceResponse(_map.Services.IndexOf(this), this.ID);
        }

        if (!String.IsNullOrEmpty(FixLegendUrl))
        {
            return new ImageLocation(_map.Services.IndexOf(this),
                this.ID, String.Empty, FixLegendUrl);
        }

        var httpService = requestContext.Http;

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, this.Service, "GetLegend", ""))
        {
            //_connector.LogString("webgis4.log", "Start Legend Request: " + _service);

            bool optimizeLegend = ((int)_map.MapScale <= (int)this.LegendOptSymbolScale && this.LegendOptMethod == LegendOptimization.Symbols),
                 legendVisThemesOnly = (this.LegendOptMethod == LegendOptimization.Themes || this.LegendOptMethod == LegendOptimization.Symbols);

            double mapScale = _map.MapScale;
            double refScale = this.RefScale;

            double widthFactor = -1.0, fontsizeFactor = -1.0;
            if (refScale > 0.1)
            {
                widthFactor = fontsizeFactor = refScale / Math.Max(mapScale, 1.0);
            }

            // REQUEST erzeugen
            StringBuilder axl = new StringBuilder();
            //StringWriter sw=new StringWriter(axl);
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, _encoding);

            //xWriter.Formatting=Formatting.Indented;
            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("GET_IMAGE");
            xWriter.WriteStartElement("PROPERTIES");

            AxlHelper.AXLaddFeatureCoordsys(ref xWriter, this.FeatureCoordsys);
            AxlHelper.AXLaddFilterCoordsys(ref xWriter, this.FilterCoordsys);

            xWriter.WriteStartElement("ENVELOPE");
            xWriter.WriteAttributeString("maxx", _map.Extent.MaxX.ToString(_nfi));
            xWriter.WriteAttributeString("minx", _map.Extent.MinX.ToString(_nfi));
            xWriter.WriteAttributeString("maxy", _map.Extent.MaxY.ToString(_nfi));
            xWriter.WriteAttributeString("miny", _map.Extent.MinY.ToString(_nfi));
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("BACKGROUND");
            xWriter.WriteAttributeString("color", "255,255,255");
            xWriter.WriteAttributeString("transcolor", "255,255,255");
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("IMAGESIZE");
            xWriter.WriteAttributeString("width", _map.ImageWidth.ToString());
            xWriter.WriteAttributeString("height", _map.ImageHeight.ToString());
            xWriter.WriteAttributeString("dpi", _map.Dpi.ToString());
            if (_dpi != _map.Dpi)
            {
                xWriter.WriteAttributeString("scalesymbols", "true");
            }
            xWriter.WriteEndElement();

            xWriter.WriteStartElement("LAYERLIST");

            foreach (Layer layer in _layers)
            {
                xWriter.WriteStartElement("LAYERDEF");

                xWriter.WriteAttributeString("id", layer.ID);
                bool visible = layer.Visible;

                if (mapScale > 0)
                {
                    int mins = (int)layer.MinScale,
                        maxs = (int)layer.MaxScale;
                    if ((mins > 0) && (mins > Math.Round(mapScale + 0.5, 0))) { visible = false; }
                    if ((maxs > 0) && (maxs < Math.Round(mapScale - 0.5, 0))) { visible = false; }
                }
                xWriter.WriteAttributeString("visible", visible.ToString());

                if (visible)
                {
                    #region Renderer
                    if (layer is FeatureLayer)
                    {
                        string renderer = AxlHelper.addLabels2Renderer((FeatureLayer)layer);
                        if (optimizeLegend)
                        {
                            renderer = await ThinRendererAsync(httpService, (FeatureLayer)layer);
                        }

                        if (renderer != ((FeatureLayer)layer).Renderer)
                        {
                            xWriter.WriteRaw(Types.Umlaute2Esri(renderer));
                        }
                    }
                    #endregion
                }

                xWriter.WriteEndElement(); // LAYERDEF
            }
            xWriter.WriteEndElement(); // LAYERLIST

            #region Legende erstellen
            xWriter.WriteStartElement("LEGEND");
            //xWriter.WriteAttributeString("title","Legende");
            xWriter.WriteAttributeString("font", "Arial");
            xWriter.WriteAttributeString("autoextend", "true");
            xWriter.WriteAttributeString("columns", "1");
            xWriter.WriteAttributeString("width", "160" /*((int)(m_legendwidth * m_dpi / 96.0)).ToString()*/);
            xWriter.WriteAttributeString("height", "170" /*((int)(170.0 * m_dpi / 96.0)).ToString()*/);
            //xWriter.WriteAttributeString("backgroundcolor","215,227,231");
            xWriter.WriteAttributeString("backgroundcolor", "255,255,255");
            //xWriter.WriteAttributeString("antialiasing","false");  // nicht bei ArcMapService
            xWriter.WriteAttributeString("layerfontsize", "11" /*((int)(11 * m_dpi / 96.0)).ToString()*/);
            xWriter.WriteAttributeString("valuefontsize", "10" /*((int)(10 * m_dpi / 96.0)).ToString()*/);
            //xWriter.WriteAttributeString("swatchheight", ((int)(14 * m_dpi / 96.0)).ToString());
            //xWriter.WriteAttributeString("swatchwidth", ((int)(18 * m_dpi / 96.0)).ToString());
            //xWriter.WriteAttributeString("cellspacing", ((int)(2 * m_dpi / 96.0)).ToString());

            //xWriter.WriteAttributeString("titlefontsize","16");
            //xWriter.WriteAttributeString("cansplit","true");

            xWriter.WriteStartElement("LAYERS");
            foreach (Layer layer in _layers)
            {
                bool visible = false;

                // WebGIS 4 Methode
                //if (_map.Toc != null)
                //{
                //    ITocElement theme = _map.Toc.TocElements.FindById(_id + ":" + layer.ID);
                //    if (theme != null)
                //    {
                //        visible = theme.Visible && theme.ShowInLegend;
                //    }
                //    else
                //    {
                //        visible = false;
                //    }
                //}
                //else
                {
                    visible = layer.Visible && this.LayerProperties.ShowInLegend(layer.ID);
                }

                if (mapScale > 0)
                {
                    int mins = (int)layer.MinScale,
                        maxs = (int)layer.MaxScale;
                    if ((mins > 0) && (mins > Math.Round(mapScale + 0.5, 0)))
                    {
                        visible = false;
                    }

                    if ((maxs > 0) && (maxs < Math.Round(mapScale - 0.5, 0)))
                    {
                        visible = false;
                    }
                }

                if (visible)
                {
                    bool show = false;

                    if (legendVisThemesOnly && layer.Type != LayerType.image)
                    {
                        if (!await hasLayerFeaturesInVisibleEnvAsync(httpService, layer.ID))
                        {
                            show = false;
                        }
                    }

                    if (!show)  // Layer ausschließen
                    {
                        xWriter.WriteStartElement("LAYER");
                        xWriter.WriteAttributeString("id", layer.ID);
                        xWriter.WriteEndElement();
                    }
                }
            }
            xWriter.WriteEndElement();  // LAYERS
            xWriter.WriteEndElement(); // LEGEND

            #endregion

            xWriter.WriteStartElement("DRAW");
            xWriter.WriteAttributeString("map", "false");

            xWriter.WriteEndElement(); // PROPERTIES

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();

            #region Request
            string req = axl.ToString().Replace("&amp;", "&");
            // REQUEST verschicken
            int try_counter = 0;
            string resp = "";

            while (true)
            {
                //resp = await _connector.SendRequestAsync(req, _server, _service);
                resp = await httpService.SendAxlRequestAsync(_connectionProperties, req, _server, _service);
                if (resp.IndexOf("<?xml ") == 0)
                {
                    break;
                }

                if (resp.IndexOf("<?xml ") > 0 && resp.IndexOf("data update is in progress.") != -1)
                {
                    try_counter++;
                    if (try_counter > 5)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            #endregion

            #region Parse Response
            string imagePath = String.Empty;
            string imageUrl = String.Empty;

            if (resp.IndexOf("<ERROR") != -1)
            {
                if (resp.ToLower().Contains("timeout"))
                {
                    return new TimeoutResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp));
                }

                return new ErrorResponse(_map.Services.IndexOf(this), this.ID, ParseError(resp), ParseError2(resp));
            }

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(resp);
            }
            catch (Exception ex)
            {
                return new ExceptionResponse(
                    _map.Services.IndexOf(this),
                    this.ID,
                    new Exception("Xml:" + resp + "|" + ex.Message + "|" + ex.StackTrace));
            }

            XmlNode node = xmldoc.SelectSingleNode("//LEGEND");
            //if (node.Attributes["file"] != null)
            //{
            //    imagePath = node.Attributes["file"].Value;
            //}

            if (node.Attributes["url"] != null)
            {
                imageUrl = httpService.ApplyUrlOutputRedirection(node.Attributes["url"].Value);
            }

            // Bild downloaden und zuschneiden,
            // weil Legenden vom IMS immer zu lang sind!

            MemoryStream imageBytes = null;

            if (!this.OutputPath.StartsWith("http://") && !this.OutputPath.StartsWith("https://"))
            {
                string filename = imageUrl.Substring(imageUrl.Replace("\\", "/").LastIndexOf("/") + 1);
                FileInfo fi = new FileInfo(this.OutputPath + @"/" + filename);
                if (fi.Exists)
                {
                    imageBytes = new MemoryStream(File.ReadAllBytes(fi.FullName));
                }
            }
            if (imageBytes == null)
            {
                //imageBytes = await _connector.DownloadImgBytesAsync(imageUrl);
                imageBytes = new MemoryStream(await httpService.GetDataAsync(imageUrl));
            }

            try
            {
                using (var legendPic = Current.Engine.CreateBitmap(imageBytes))
                {
                    int y = legendPic.Height - 1;

                    var backCol = legendPic.GetPixel(0, y);
                    for (; y >= 0; y--)
                    {
                        bool found = false;
                        for (int x = 0; x < legendPic.Width; x++)
                        {
                            var col = legendPic.GetPixel(x, y);
                            if (col.R != backCol.R ||
                                col.G != backCol.G ||
                                col.B != backCol.B)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }

                    using (var bitmap = Current.Engine.CreateBitmap(legendPic.Width, y + 3))
                    using (var canvas = bitmap.CreateCanvas())
                    {
                        canvas.DrawBitmap(legendPic, new CanvasPoint(0, 0));
                        string filename = "legend_" + System.Guid.NewGuid().ToString("N") + ".png";
                        await bitmap.SaveOrUpload(imagePath = this.OutputPath.AddUriPath(filename), ImageFormat.Png);
                        imageUrl = this.OutputUrl.AddUriPath(filename);
                    }
                }
            }
            catch { }

            #endregion

            if (imageBytes != null)
            {
                imageBytes.Dispose();
            }

            //_connector.LogString("webgis4.log", "Finished Map Request: " + _service);
            pLogger.Success = true;
            return new ImageLocation(_map.Services.IndexOf(this), this.ID, imagePath, imageUrl);
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

    #region IServiceCopyrightInfo 

    public string CopyrightInfoId { get; set; }
    public string MetadataLink { get; set; }

    #endregion

    public void SetImageFormat(ServiceImageFormat imageFormat)
    {
        switch (imageFormat)
        {
            case ServiceImageFormat.GIF:
                _imageFormat = "gif";
                break;
            case ServiceImageFormat.JPG:
                _imageFormat = "jpg";
                break;
            case ServiceImageFormat.PNG24:
            case ServiceImageFormat.PNG32:
                _imageFormat = "png";
                break;
            case ServiceImageFormat.PNG8:
                _imageFormat = "png8";
                break;
            default:
                _imageFormat = String.Empty;
                break;
        }
    }

    public bool UseFixRefScale
    {
        get { return _useFixRefScale; }
        set { _useFixRefScale = value; }
    }
    public double FixRefScale
    {
        get { return _fixRefScale; }
        set { _fixRefScale = value; }
    }
    private double RefScale
    {
        get
        {
            if (_map == null)
            {
                return 0.0;
            }

            if (_useFixRefScale == false)
            {
                return _map.RefScale;
            }

            return _fixRefScale;
        }
    }

    public string OverrideLocale
    {
        get { return _overrideLocale; }
        set { _overrideLocale = value; }
    }

    #region IServiceProjection Member

    private ServiceProjectionMethode _projMethode = ServiceProjectionMethode.none;
    public ServiceProjectionMethode ProjectionMethode
    {
        get { return _projMethode; }
        set { _projMethode = value; }
    }

    private int _projId = -1;
    public int ProjectionId
    {
        get { return _projId; }
        set { _projId = value; }
    }

    public void RefreshSpatialReference()
    {
        switch (_projMethode)
        {
            case ServiceProjectionMethode.Map:
                if (_map.SpatialReference != null)
                {
                    this.FeatureCoordsys = this.FilterCoordsys = "id='" + _map.SpatialReference.Id + "'";
                }

                break;
            case ServiceProjectionMethode.Userdefined:
                if (_projId > 0)
                {
                    if (_map.SpatialReference != null)
                    {
                        this.FeatureCoordsys = this.FilterCoordsys = "id='" + _projId + "'";
                    }
                }

                break;
        }
    }
    #endregion

    #region IService2

    public ServiceResponse PreGetMap()
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(_map.Services.IndexOf(this), this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        //if (_diagnosticsErrorResponse != null && this.Diagnostics != null && this.Diagnostics.ThrowExeption(this.DiagnosticsWaringLevel))
        //{
        //    return new ErrorResponse(_map.Services.IndexOf(this), this.ID, _diagnosticsErrorResponse.ErrorMessage, _diagnosticsErrorResponse.ErrorMessage2);
        //}

        if (!ServiceHelper.VisibleInScale(this, _map))
        {
            return new EmptyImage(_map.Services.IndexOf(this), this.ID);
        }

        double mapScale = _map.MapScale;
        bool hasVisibleLayers = false;
        foreach (Layer layer in _layers)
        {
            bool visible = layer.Visible;

            int mins = (int)layer.MinScale,
                maxs = (int)layer.MaxScale;
            if ((mins > 0) && (mins > Math.Round(mapScale + 0.5, 0))) { visible = false; }
            if ((maxs > 0) && (maxs < Math.Round(mapScale - 0.5, 0))) { visible = false; }

            if (visible)
            {
                hasVisibleLayers = true;
                break;
            }
        }
        if (hasVisibleLayers)
        {
            return null;
        }

        return new EmptyImage(_map.Services.IndexOf(this), this.ID);
    }

    public IEnumerable<ILayerProperties> LayerProperties { get; set; }

    private ServiceTheme[] _themes = null;
    public IEnumerable<ServiceTheme> ServiceThemes
    {
        get { return _themes; }
        set { _themes = value?.ToArray(); }
    }

    #endregion

    internal bool Is_gView { get { return _rotatable; } }

    #region Helper

    private string OutputPath => (this._map?.Environment?.UserString("OutputPath"))/*.OrTake(_connectionProperties?.OutputPath)*/;

    private string OutputUrl => (this._map?.Environment?.UserString("OutputUrl"))/*.OrTake(_connectionProperties?.OutputUrl)*/;

    #endregion

    #region ICacheServicePrefix Member

    public string CacheServicePrefix
    {
        get { return _imageFormat; }
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

    #region IMapServiceCapabilities

    private static MapServiceCapability[] _capabilities =
       [MapServiceCapability.Map, MapServiceCapability.Query, MapServiceCapability.Identify, MapServiceCapability.Legend];

    public MapServiceCapability[] Capabilities => _capabilities;

    #endregion
}
