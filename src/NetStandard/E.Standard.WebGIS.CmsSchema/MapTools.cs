using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class MapTools : SchemaNode, IEditable
{
    private bool _zoomIn = true;
    private bool _zoomOut = true;
    private bool _pan = true;
    private bool _refresh = true;
    private bool _fullextent = true;
    private bool _zoomback = true;
    private bool _zoomforward = true;
    private bool _refscale = true;
    private bool _visfilter = true;
    private bool _labelling = true;
    private bool _search = true;
    private bool _deepsearch = true;
    private bool _searchservice = false;
    private bool _identify = true;
    private bool _pointidentify = false;
    private bool _deepidentify = true;
    private bool _hotlink = true;
    private bool _chainage = true;
    private bool _spatialidentify = true;
    private bool _select = true;
    private bool _buffer = true;
    private int _maxbufferdist = 0;
    private bool _measure = true;
    private bool _measure3d = false;
    private bool _xy = true;
    private bool _mapoverlay = true;
    private bool _redlining = true;
    private bool _email = false;
    private bool _editing = true;
    private bool _snapping = true;
    private bool _io = true;
    private bool _print = true;
    private bool _verticalAlignment = false;
    private string _verticalAlignmentConfig = String.Empty;
    private bool _help = true, _casehelp = true;
    private bool _heightAboveDatum = false;
    private string _heightAboveDatumConfig = String.Empty;
    private bool _docmanagement = false;
    private bool _procServerTool = false;
    private string _procServerMask = String.Empty, _procServer = String.Empty;
    private bool _currentpos = false;
    private bool _geotagging = false;
    private string _geotaggingSchema = "default";
    private bool _fleetmanagement = false;
    private string _fleetmanagementSchema = String.Empty;
    private bool _fleetmanagementhistory = false;
    private bool _network = false;
    private string _networkSchema = String.Empty;
    private bool _addservice = true;
    private bool _sketchConstruct = true, _sketchTransform = true;
    private bool _sketchGpx = true;
    private bool _identifywizard = true;
    private string _geojuhuterm = String.Empty;
    private bool _solarpotential = false;
    private bool _maprotation = false, _plotservicetool = false;
    private bool _saveimage = true;
    private bool _geolocation = false;
    private SearchServiceType _searchServiceType = SearchServiceType.None;
    private string _searchServiceUrl = String.Empty;
    private bool _magnifier = false;

    private DefaultToolId _defaultTool = DefaultToolId.Pan;

    #region Properties
    [Browsable(true)]
    [DisplayName("Standardwerkzeug")]
    [Category("Allgemein")]
    public DefaultToolId DefaultTool
    {
        get { return _defaultTool; }
        set { _defaultTool = value; }
    }
    [Browsable(true)]
    [DisplayName("Zoom In Dynamisch")]
    [Category("Kartennavigation")]
    public bool ZoomIn
    {
        get { return _zoomIn; }
        set { _zoomIn = value; }
    }
    [Browsable(true)]
    [DisplayName("Zoom Out Dynamisch")]
    [Category("Kartennavigation")]
    public bool ZoomOut
    {
        get { return _zoomOut; }
        set { _zoomOut = value; }
    }
    [Browsable(true)]
    [DisplayName("Verschieben/Pan")]
    [Category("Kartennavigation")]
    public bool Pan
    {
        get { return _pan; }
        set { _pan = value; }
    }
    [Browsable(true)]
    [DisplayName("Aktuelle GPS Position")]
    [Category("Kartennavigation")]
    public bool CurrentPos
    {
        get { return _currentpos; }
        set { _currentpos = value; }
    }
    [Browsable(true)]
    [DisplayName("Geo Location ermitteln")]
    [Category("Kartennavigation")]
    public bool GeoLocation
    {
        get { return _geolocation; }
        set { _geolocation = value; }
    }
    [Browsable(true)]
    [DisplayName("Karte Aktualisieren")]
    [Category("Maßstab/Atkualisierung")]
    public bool Refresh
    {
        get { return _refresh; }
        set { _refresh = value; }
    }
    [Browsable(true)]
    [DisplayName("Auf maximalen Extent zoomen")]
    [Category("Kartennavigation")]
    public bool FullExtent
    {
        get { return _fullextent; }
        set { _fullextent = value; }
    }
    [Browsable(true)]
    [DisplayName("Zoom Zurück")]
    [Category("Kartennavigation")]
    public bool ZoomBack
    {
        get { return _zoomback; }
        set { _zoomback = value; }
    }
    [Browsable(true)]
    [DisplayName("Zoom Vorwärts")]
    [Category("Kartennavigation")]
    public bool ZoomForward
    {
        get { return _zoomforward; }
        set { _zoomforward = value; }
    }
    [Browsable(true)]
    [DisplayName("Karte Drehen (Map Roation)")]
    [Category("Kartennavigation")]
    public bool MapRotation
    {
        get { return _maprotation; }
        set { _maprotation = value; }
    }
    [Browsable(true)]
    [DisplayName("Referenzmaßstab setzen")]
    [Category("Maßstab/Darstellung")]
    [AuthorizablePropertyAttribute("refscale", false)]
    public bool RefScale
    {
        get { return _refscale; }
        set { _refscale = value; }
    }
    [DisplayName("Darstellungsfilter setzen")]
    [Category("Maßstab/Darstellung")]
    [AuthorizablePropertyAttribute("visfilter", false)]
    public bool VisFilter
    {
        get { return _visfilter; }
        set { _visfilter = value; }
    }
    [DisplayName("Themen beschriften")]
    [Category("Maßstab/Darstellung")]
    [AuthorizablePropertyAttribute("labelling", false)]
    public bool Labelling
    {
        get { return _labelling; }
        set { _labelling = value; }
    }
    [Browsable(true)]
    [DisplayName("Suchen")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("search", false)]
    public bool Search
    {
        get { return _search; }
        set { _search = value; }
    }
    [Browsable(true)]
    [DisplayName("Identify")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("identify", false)]
    public bool Identify
    {
        get { return _identify; }
        set { _identify = value; }
    }
    [Browsable(true)]
    [DisplayName("Punkt Identify (kein Rechteck aufziehen, nur klicken!)")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("pointidentify", false)]
    public bool PointIdentify
    {
        get { return _pointidentify; }
        set { _pointidentify = value; }
    }
    [Browsable(true)]
    [DisplayName("Identify Wizard")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("identifywizard", false)]
    public bool IdentifyWizard
    {
        get { return _identifywizard; }
        set { _identifywizard = value; }
    }

    [Browsable(true)]
    [DisplayName("Deep Identify")]
    [Category("GeoJUHU")]
    [AuthorizablePropertyAttribute("deepidentify", false)]
    public bool DeepIdentify
    {
        get { return _deepidentify; }
        set { _deepidentify = value; }
    }
    [Browsable(true)]
    [DisplayName("GeoJuhu Suche")]
    [Category("GeoJUHU")]
    [AuthorizablePropertyAttribute("deepsearch", false)]
    public bool DeepSearch
    {
        get { return _deepsearch; }
        set { _deepsearch = value; }
    }
    [Browsable(true)]
    [DisplayName("GeoJUHU Themenbezeichnung")]
    [Category("GeoJUHU")]
    public string GeoJuhuTerm
    {
        get { return _geojuhuterm; }
        set { _geojuhuterm = value; }
    }

    [Browsable(true)]
    [DisplayName("Geo(land) Suchservice")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("searchservice", false)]
    public bool SearchService
    {
        get { return _searchservice; }
        set { _searchservice = value; }
    }

    [Browsable(true)]
    [DisplayName("Hotlink")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("hotlink", false)]
    public bool Hotlink
    {
        get { return _hotlink; }
        set { _hotlink = value; }
    }
    [Browsable(true)]
    [DisplayName("Stationierungsabfrage")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("chainage", false)]
    public bool Chainage
    {
        get { return _chainage; }
        set { _chainage = value; }
    }
    [Browsable(false)]
    [DisplayName("Räumliche Abfrage")]
    [Category("Abfragewerkzeuge")]
    [AuthorizablePropertyAttribute("spatialidentify", false)]
    public bool SpatialIdentify
    {
        get { return _spatialidentify; }
        set { _spatialidentify = value; }
    }
    [Browsable(true)]
    [DisplayName("Auswählen")]
    [Category("Auswahlwerkzeuge")]
    [AuthorizablePropertyAttribute("select", false)]
    public bool Select
    {
        get { return _select; }
        set { _select = value; }
    }
    [Browsable(true)]
    [DisplayName("Nachbarschaftsberechnung (Puffern)")]
    [Category("Auswahlwerkzeuge")]
    [AuthorizablePropertyAttribute("buffer", false)]
    public bool Buffer
    {
        get { return _buffer; }
        set { _buffer = value; }
    }
    [Browsable(true)]
    [DisplayName("Nachbarschaftsberechnung (Puffern) - max. Distance")]
    [Description("0 = keine Einschränkung für den Benutzer.")]
    [Category("Auswahlwerkzeuge")]
    public int MaxBufferDist
    {
        get { return _maxbufferdist; }
        set { _maxbufferdist = value; }
    }
    [Browsable(true)]
    [DisplayName("Messen")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("measure", false)]
    public bool Measure
    {
        get { return _measure; }
        set { _measure = value; }
    }
    [Browsable(true)]
    [DisplayName("Messen 3d")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("measure3d", false)]
    public bool Measure3d
    {
        get { return _measure3d; }
        set { _measure3d = value; }
    }
    [Browsable(true)]
    [DisplayName("Koordinaten abfragen")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("xy", false)]
    public bool XY
    {
        get { return _xy; }
        set { _xy = value; }
    }
    [Browsable(true)]
    [DisplayName("Karte Überblenden")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("mapoverlay", false)]
    public bool MapOverlay
    {
        get { return _mapoverlay; }
        set { _mapoverlay = value; }
    }
    [Browsable(true)]
    [DisplayName("Lupe")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("magnifier", false)]
    public bool Magnifier
    {
        get { return _magnifier; }
        set { _magnifier = value; }
    }
    [Browsable(true)]
    [DisplayName("Redlining")]
    [Category("Werkzeuge Pro")]
    [AuthorizablePropertyAttribute("redlining", false)]
    public bool Redlining
    {
        get { return _redlining; }
        set { _redlining = value; }
    }
    [Browsable(true)]
    [DisplayName("Email/Treffpunkt")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("email", false)]
    public bool EMail
    {
        get { return _email; }
        set { _email = value; }
    }
    [Browsable(true)]
    [DisplayName("Service hinzufügen")]
    [Category("Werkzeuge")]
    [AuthorizablePropertyAttribute("addservice", false)]
    public bool AddService
    {
        get { return _addservice; }
        set { _addservice = value; }
    }
    [Browsable(true)]
    [DisplayName("Webediting")]
    [Category("Werkzeuge Pro")]
    [AuthorizablePropertyAttribute("edit", false)]
    public bool Editing
    {
        get { return _editing; }
        set { _editing = value; }
    }
    [Browsable(true)]
    [DisplayName("Dokumentenmanagement")]
    [Category("Werkzeuge Pro")]
    [AuthorizablePropertyAttribute("docmanagement", false)]
    public bool DocumentManagement
    {
        get { return _docmanagement; }
        set { _docmanagement = value; }
    }

    [Browsable(true)]
    [Category("Processing Server")]
    [DisplayName("Processing Server Werkzeug")]
    [AuthorizablePropertyAttribute("ps", false)]
    public bool ProcessingServerTool
    {
        get { return _procServerTool; }
        set { _procServerTool = value; }
    }
    [Browsable(true)]
    [Category("Processing Server")]
    [DisplayName("Processing Server Maske (Url)")]
    public string ProcessingServerMask
    {
        get { return _procServerMask; }
        set { _procServerMask = value; }
    }
    [Browsable(true)]
    [Category("Processing Server")]
    [DisplayName("Processing Server (server:port)")]
    public string ProcessingServer
    {
        get { return _procServer; }
        set { _procServer = value; }
    }


    [Browsable(true)]
    [DisplayName("Snapping")]
    [Category("Werkzeuge Pro")]
    [AuthorizablePropertyAttribute("snapping", false)]
    public bool Snapping
    {
        get { return _snapping; }
        set { _snapping = value; }
    }
    [Browsable(true)]
    [DisplayName("Drucken")]
    [Category("Datei")]
    [AuthorizablePropertyAttribute("print", false)]
    public bool Print
    {
        get { return _print; }
        set { _print = value; }
    }
    [Browsable(true)]
    [DisplayName("Plot Service Tool")]
    [Category("Datei")]
    [AuthorizablePropertyAttribute("plotservicetool", false)]
    public bool PlotServiceTool
    {
        get { return _plotservicetool; }
        set { _plotservicetool = value; }
    }
    [Browsable(true)]
    [DisplayName("Laden / Speichern")]
    [Category("Datei")]
    [AuthorizablePropertyAttribute("io", false)]
    public bool IO
    {
        get { return _io; }
        set { _io = value; }
    }
    [Browsable(true)]
    [DisplayName("Kartenbild speichern")]
    [Category("Datei")]
    [AuthorizablePropertyAttribute("saveimage", false)]
    public bool SaveImage
    {
        get { return _saveimage; }
        set { _saveimage = value; }
    }
    [Browsable(true)]
    [DisplayName("Höhenprofil")]
    [Category("Höhenprofil")]
    [AuthorizablePropertyAttribute("verticalalignment", false)]
    public bool VerticalAlignment
    {
        get { return _verticalAlignment; }
        set { _verticalAlignment = value; }
    }
    [Browsable(true)]
    [DisplayName("Höhenprofil-Konfiguration")]
    [Category("Höhenprofil")]
    [Editor(typeof(ProfilesConfigEditor),
        typeof(ITypeEditor))]
    public string VerticalAlignmentConfig
    {
        get { return _verticalAlignmentConfig; }
        set { _verticalAlignmentConfig = value; }
    }
    [Browsable(true)]
    [DisplayName("Höhenkoten Werkzeug")]
    [Category("Höhenkote")]
    [AuthorizablePropertyAttribute("had", false)]
    public bool HeightAboveDatum
    {
        get { return _heightAboveDatum; }
        set { _heightAboveDatum = value; }
    }
    [Browsable(true)]
    [DisplayName("Höhenkoten-Konfiguration")]
    [Category("Höhenkote")]
    //[Editor(typeof(HeightAboveDatumConfigEditor),
    //        typeof(ITypeEditor))]
    public string HeightAboveDatumConfig
    {
        get { return _heightAboveDatumConfig; }
        set { _heightAboveDatumConfig = value; }
    }
    [Browsable(true)]
    [DisplayName("Hilfe")]
    [Category("Hilfe")]
    public bool Help
    {
        get { return _help; }
        set { _help = value; }
    }
    [Browsable(true)]
    [DisplayName("Maus-Sensitive-Hilfe")]
    [Category("Hilfe")]
    public bool CaseHelp
    {
        get { return _casehelp; }
        set { _casehelp = value; }
    }

    [Browsable(true)]
    [DisplayName("GeoTagging")]
    [Category("Module")]
    [AuthorizablePropertyAttribute("geotagging", false)]
    public bool GeoTagging
    {
        get { return _geotagging; }
        set { _geotagging = value; }
    }
    [Browsable(true)]
    [DisplayName("GeoTagging Serverschema")]
    [Category("Module")]
    //[Editor(typeof(GeoTaggingServerSchemaEditor),
    //        typeof(ITypeEditor))]
    public string GeoTaggingSchema
    {
        get { return _geotaggingSchema; }
        set { _geotaggingSchema = value; }
    }

    [Browsable(true)]
    [DisplayName("Flottenmanagement")]
    [Category("Module")]
    [AuthorizablePropertyAttribute("fleetmanagement", false)]
    public bool FleetManagement
    {
        get { return _fleetmanagement; }
        set { _fleetmanagement = value; }
    }
    [Browsable(true)]
    [DisplayName("Flottenmanagement History")]
    [Category("Module")]
    [AuthorizablePropertyAttribute("fleetmanagementhistory", false)]
    public bool FleetManagementHistory
    {
        get { return _fleetmanagementhistory; }
        set { _fleetmanagementhistory = value; }
    }
    [Browsable(true)]
    [DisplayName("Flottenmanagement Serverschema")]
    [Category("Module")]
    //[Editor(typeof(FleetManagementServerSchemaEditor),
    //        typeof(ITypeEditor))]
    public string FleetManagementSchema
    {
        get { return _fleetmanagementSchema; }
        set { _fleetmanagementSchema = value; }
    }

    [Browsable(true)]
    [DisplayName("Netzwerk")]
    [Category("Module")]
    [AuthorizablePropertyAttribute("network", false)]
    public bool Network
    {
        get { return _network; }
        set { _network = value; }
    }
    [Browsable(true)]
    [DisplayName("Netzwerk Serverschema")]
    [Category("Module")]
    //[Editor(typeof(NetworkSchemaEditor),
    //        typeof(ITypeEditor))]
    public string NetworkSchema
    {
        get { return _networkSchema; }
        set { _networkSchema = value; }
    }

    [Browsable(true)]
    [Category("Module")]
    [DisplayName("Solar Potential")]
    [AuthorizablePropertyAttribute("solarpotential", false)]
    public bool SolarPotential
    {
        get { return _solarpotential; }
        set { _solarpotential = value; }
    }

    [Browsable(true)]
    [DisplayName("Konstruieren")]
    [Category("Sketch")]
    [AuthorizablePropertyAttribute("sketch_construct", false)]
    public bool SketchConstruct
    {
        get { return _sketchConstruct; }
        set { _sketchConstruct = value; }
    }
    [Browsable(true)]
    [DisplayName("Transformieren")]
    [Category("Sketch")]
    [AuthorizablePropertyAttribute("sketch_transform", false)]
    public bool SketchTransform
    {
        get { return _sketchTransform; }
        set { _sketchTransform = value; }
    }
    [Browsable(true)]
    [DisplayName("GPX Upload/Download")]
    [Category("Sketch")]
    [AuthorizablePropertyAttribute("sketch_gpx", false)]
    public bool SketchGpx
    {
        get { return _sketchGpx; }
        set { _sketchGpx = value; }
    }

    [Browsable(true)]
    [DisplayName("Search Service Type")]
    [Category("Search Service")]
    public SearchServiceType SearchServType
    {
        get { return _searchServiceType; }
        set { _searchServiceType = value; }
    }
    [Browsable(true)]
    [DisplayName("Search Service Url")]
    [Category("Search Service")]
    [Description("Url zum gDSS Service")]
    public string SearchServiceUrl
    {
        get { return _searchServiceUrl; }
        set { _searchServiceUrl = value; }
    }
    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _defaultTool = (DefaultToolId)stream.Load("defaulttool", (int)DefaultToolId.Pan);

        _zoomIn = (bool)stream.Load("zoomin", true);
        _zoomOut = (bool)stream.Load("zoomout", true);
        _pan = (bool)stream.Load("pan", true);
        _refresh = (bool)stream.Load("refresh", true);
        _visfilter = (bool)stream.Load("visfilter", true);
        _labelling = (bool)stream.Load("labelling", true);
        _fullextent = (bool)stream.Load("fullextent", true);
        _zoomback = (bool)stream.Load("zoomback", true);
        _zoomforward = (bool)stream.Load("zoomforward", true);
        _refscale = (bool)stream.Load("refscale", true);
        _maprotation = (bool)stream.Load("maprotation", false);

        _search = (bool)stream.Load("search", true);
        _deepsearch = (bool)stream.Load("deepsearch", true);
        _searchservice = (bool)stream.Load("searchservice", false);
        _identify = (bool)stream.Load("identify", true);
        _pointidentify = (bool)stream.Load("pointidentify", false);
        _deepidentify = (bool)stream.Load("deepidentify", true);
        _hotlink = (bool)stream.Load("hotlink", true);
        _chainage = (bool)stream.Load("chainage", true);
        _spatialidentify = (bool)stream.Load("spatialidentify", true);

        _geojuhuterm = (string)stream.Load("geojuhuterm", String.Empty);

        _select = (bool)stream.Load("select", true);
        _buffer = (bool)stream.Load("buffer", true);
        _maxbufferdist = (int)stream.Load("maxbufferdist", 0);

        _measure = (bool)stream.Load("measure", true);
        _measure3d = (bool)stream.Load("measure3d", false);
        _xy = (bool)stream.Load("xy", true);
        _mapoverlay = (bool)stream.Load("mapoverlay", true);
        _redlining = (bool)stream.Load("redlining", true);
        _email = (bool)stream.Load("email", true);
        _editing = (bool)stream.Load("edit", true);
        _snapping = (bool)stream.Load("snapping", true);
        _docmanagement = (bool)stream.Load("docmanagement", false);
        _procServerTool = (bool)stream.Load("ps", false);
        _procServerMask = (string)stream.Load("psmask", String.Empty);
        _procServer = (string)stream.Load("psserver", String.Empty);

        _io = (bool)stream.Load("io", true);
        _saveimage = (bool)stream.Load("saveimage", true);
        _print = (bool)stream.Load("print", true);
        _plotservicetool = (bool)stream.Load("plotservicetool", false);

        _verticalAlignment = (bool)stream.Load("verticalalignment", false);
        _verticalAlignmentConfig = (string)stream.Load("verticalalignmentconfig", String.Empty);

        _heightAboveDatum = (bool)stream.Load("had", false);
        _heightAboveDatumConfig = (string)stream.Load("hadconfig", String.Empty);

        _help = (bool)stream.Load("help", true);
        _casehelp = (bool)stream.Load("casehelp", true);

        _currentpos = (bool)stream.Load("currentpos", false);
        _geolocation = (bool)stream.Load("geolocation", false);

        _geotagging = (bool)stream.Load("geotagging", false);
        _geotaggingSchema = (string)stream.Load("geotaggingschema", "default");

        _fleetmanagement = (bool)stream.Load("fleetmanagement", false);
        _fleetmanagementhistory = (bool)stream.Load("fleetmanagementhistory", false);
        _fleetmanagementSchema = (string)stream.Load("fleetmanagementschema", String.Empty);

        _network = (bool)stream.Load("network", false);
        _networkSchema = (string)stream.Load("networkschema", String.Empty);

        _addservice = (bool)stream.Load("addservice", true);

        _sketchConstruct = (bool)stream.Load("sketch_construct", true);
        _sketchTransform = (bool)stream.Load("sketch_transform", true);
        _sketchGpx = (bool)stream.Load("sketch_gpx", true);

        _identifywizard = (bool)stream.Load("identifywizard", true);

        _solarpotential = (bool)stream.Load("solarpotential", false);

        _searchServiceType = (SearchServiceType)stream.Load("searchservicetype", (int)SearchServiceType.None);
        _searchServiceUrl = (string)stream.Load("searchserviceurl", String.Empty);

        _magnifier = (bool)stream.Load("magnifier", false);
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("defaulttool", (int)_defaultTool);

        stream.Save("zoomin", _zoomIn);
        stream.Save("zoomout", _zoomOut);
        stream.Save("pan", _pan);
        stream.Save("refresh", _refresh);
        stream.Save("visfilter", _visfilter, false);
        stream.Save("labelling", _labelling, false);
        stream.Save("fullextent", _fullextent);
        stream.Save("zoomback", _zoomback);
        stream.Save("zoomforward", _zoomforward);
        stream.Save("refscale", _refscale, false);
        stream.Save("maprotation", _maprotation);

        stream.Save("search", _search, false);
        stream.Save("deepsearch", _deepsearch, false);
        stream.Save("searchservice", _searchservice, false);
        stream.Save("identify", _identify, false);
        stream.Save("pointidentify", _pointidentify, false);
        stream.Save("deepidentify", _deepidentify, false);
        stream.Save("hotlink", _hotlink, false);
        stream.Save("chainage", _chainage, false);
        stream.Save("spatialidentify", _spatialidentify, false);

        stream.Save("geojuhuterm", _geojuhuterm);

        stream.Save("select", _select, false);
        stream.Save("buffer", _buffer, false);
        stream.Save("maxbufferdist", _maxbufferdist);

        stream.Save("measure", _measure, false);
        stream.Save("measure3d", _measure3d, false);
        stream.Save("xy", _xy, false);
        stream.Save("mapoverlay", _mapoverlay, false);
        stream.Save("redlining", _redlining, false);
        stream.Save("email", _email, false);
        stream.Save("edit", _editing, false);
        stream.Save("snapping", _snapping, false);
        stream.Save("docmanagement", _docmanagement, false);
        stream.Save("ps", _procServerTool, false);
        stream.Save("psmask", _procServerMask, String.Empty);
        stream.Save("psserver", _procServer, String.Empty);
        stream.Save("io", _io, false);
        stream.Save("saveimage", _saveimage, false);
        stream.Save("print", _print, false);
        stream.Save("plotservicetool", _plotservicetool, false);

        stream.Save("verticalalignment", _verticalAlignment, false);
        stream.Save("verticalalignmentconfig", _verticalAlignmentConfig);

        stream.Save("had", _heightAboveDatum, false);
        stream.Save("hadconfig", _heightAboveDatumConfig);

        stream.Save("help", _help);
        stream.Save("casehelp", _casehelp);

        stream.Save("currentpos", _currentpos);
        stream.Save("geolocation", _geolocation);

        stream.Save("geotagging", _geotagging, false);
        stream.Save("geotaggingschema", _geotaggingSchema);

        stream.Save("fleetmanagement", _fleetmanagement, false);
        stream.Save("fleetmanagementhistory", _fleetmanagementhistory, false);
        stream.Save("fleetmanagementschema", _fleetmanagementSchema);

        stream.Save("network", _network, false);
        stream.Save("networkschema", _networkSchema);

        stream.Save("addservice", _addservice, false);

        stream.Save("sketch_construct", _sketchConstruct);
        stream.Save("sketch_transform", _sketchTransform);
        stream.Save("sketch_gpx", _sketchGpx);

        stream.Save("identifywizard", _identifywizard);

        if (_solarpotential)
        {
            stream.Save("solarpotential", _solarpotential);
        }

        stream.Save("searchservicetype", (int)_searchServiceType);
        stream.Save("searchserviceurl", _searchServiceUrl);

        stream.Save("magnifier", _magnifier);
    }

    #endregion
}
