using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class GeneralTileCache : CopyableNode, ICreatable, IEditable, IUI, IDisplayName
{
    public enum CreateTemplate
    {
        Empty = 0,
        OSM_Mapnik = 1,
        Google_Map_Tiles = 2,
        Google_Satellit_Tiles = 3
    }
    private CreateTemplate _createTemplate = CreateTemplate.Empty;
    private string _guid;
    private TileGridRendering _rendering = TileGridRendering.Quality;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public GeneralTileCache(CmsItemTransistantInjectionServicePack servicePack)
    {
        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        _servicePack = servicePack;
    }

    internal CreateTemplate Tempate
    {
        get { return _createTemplate; }
        set { _createTemplate = value; }
    }

    #region Properties

    [DisplayName("Rendering")]
    [Description("Für Luftbilder 'Quality' verwenden. Für Ortspläne (mit Text) 'Readablility'...")]
    public TileGridRendering Rendering
    {
        get { return _rendering; }
        set { _rendering = value; }
    }

    //[DisplayName("Max. Level")]
    //[Description("Der höchste Level, der für diesen Dienst verwendet werden kann. Ein Wert kleiner als 0, gibt an, dass das maximale Level dem maximalen Matrixset Level aus den Capabilities entspricht.")]
    //public int MaxLevel { get; set; }

    [DisplayName("Unter Max. Level verbergen")]
    [Description("Zoomt der Anwender weiter in die Karte, als dieser Tiling Dienst zur Verfügung steht, werden die Tiles nicht mehr angezeigt. Per Default (Wert = false) wird der Dienst trotzdem angezeigt und die Tiles entsprechend \"vergrößert/unscharf\" dargestellt.")]
    public bool HideBeyondMaxLevel { get; set; }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        BuildServiceInfo(FullName);
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new FormGeneralTileCache();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region Helper
    public void BuildServiceInfo(string fullName)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"\themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        TOC toc = new TOC();
        toc.Name = "default";
        IStreamDocument xmlStream = DocumentFactory.New(di.FullName);
        toc.Save(xmlStream);
        var fi = DocumentFactory.DocumentInfo(di.FullName + @"\tocs\" + toc.CreateAs(true) + ".xml");
        xmlStream.SaveDocument(fi.FullName);
        var di_tocs_default = fi.Directory;

        ServiceLayer layer = new ServiceLayer();
        layer.Name = "_tilecache";

        layer.Url = Crypto.GetID();
        layer.Id = "0";

        layer.Visible = true;

        xmlStream = DocumentFactory.New(di.FullName);
        layer.Save(xmlStream);
        xmlStream.SaveDocument(di.FullName + @"\themes\" + layer.CreateAs(true) + ".xml");

        string themeLinkUri = ThemeExists(fullName, layer);

        TocTheme tocTheme = new TocTheme();
        tocTheme.LinkUri = themeLinkUri;
        tocTheme.AliasName = layer.Name;
        tocTheme.Visible = layer.Visible;

        string tocThemeConfig = di_tocs_default.FullName + @"\l" + GuidEncoder.Encode(Guid.NewGuid()).ToString().ToLower() + ".link";
        xmlStream = DocumentFactory.New(di.FullName);
        tocTheme.Save(xmlStream);
        xmlStream.SaveDocument(tocThemeConfig);

        ItemOrder itemOrder = new ItemOrder(di.FullName + @"\themes");
        itemOrder.Save();

        GeneralTileCacheProperties props = null;
        switch (_createTemplate)
        {
            case CreateTemplate.OSM_Mapnik:
                props = new GeneralTileCacheProperties();
                props.Origin = TileGridOrientation.UpperLeft;
                props.TileWidth = props.TileHeight = 256;
                props.TileUrl = "http://tile.openstreetmap.org/mapnik/[LEVEL]/[COL]/[ROW].png";
                props.TileCacheExtent.MinX = -20037508.342789244;
                props.TileCacheExtent.MinY = -20037508.342789244;
                props.TileCacheExtent.MaxX = 20037508.342789244;
                props.TileCacheExtent.MaxY = 20037508.342789244;
                props.Resolutions = new double[] {
                    591658710.909132,
                    295829355.454566,
                    147914677.727283,
                    73957338.8636414,
                    36978669.4318207,
                    18489334.7159103,
                    9244667.35795517,
                    4622333.67897759,
                    2311166.83948879,
                    1155583.4197444,
                    577791.709872198,
                    288895.854936099,
                    144447.92746805,
                    72223.9637340248,
                    36111.9818670124,
                    18055.9909335062,
                    9027.9954667531,
                    4513.99773337655,
                    2256.99886668828
                };

                // Mastäbe in Resolutions umwanden
                for (int i = 0; i < props.Resolutions.Length; i++)
                {
                    props.Resolutions[i] /= (96.0 / 0.0254);
                }

                break;
            case CreateTemplate.Google_Map_Tiles:
            case CreateTemplate.Google_Satellit_Tiles:
                props = new GeneralTileCacheProperties();
                props.Origin = TileGridOrientation.UpperLeft;
                props.TileWidth = props.TileHeight = 256;
                if (_createTemplate == CreateTemplate.Google_Map_Tiles)
                {
                    props.TileUrl = "http://mt0.google.com/vt/lyrs=m@121&hl=de&x=[COL]&y=[ROW]&z=[LEVEL]";
                }
                else
                {
                    props.TileUrl = "http://khm0.google.com/kh/v=58&x=[COL]&y=[ROW]&z=[LEVEL]";
                }

                props.TileCacheExtent.MinX = -20037508.342789244;
                props.TileCacheExtent.MinY = -20037508.342789244;
                props.TileCacheExtent.MaxX = 20037508.342789244;
                props.TileCacheExtent.MaxY = 20037508.342789244;
                props.Resolutions = new double[] {
                    591658710.909132,
                    295829355.454566,
                    147914677.727283,
                    73957338.8636414,
                    36978669.4318207,
                    18489334.7159103,
                    9244667.35795517,
                    4622333.67897759,
                    2311166.83948879,
                    1155583.4197444,
                    577791.709872198,
                    288895.854936099,
                    144447.92746805,
                    72223.9637340248,
                    36111.9818670124,
                    18055.9909335062,
                    9027.9954667531,
                    4513.99773337655,
                    2256.99886668828,
                    1128.49943334414,
                    564.249716672069
                };

                // Mastäbe in Resolutions umwanden
                for (int i = 0; i < props.Resolutions.Length; i++)
                {
                    props.Resolutions[i] /= (96.0 / 0.0254);
                }

                break;
        }
        if (props != null)
        {
            xmlStream = DocumentFactory.New(di.FullName);
            props.Save(xmlStream);
            xmlStream.SaveDocument(di.FullName + @"\properties.xml");
        }
    }
    private string ThemeExists(string fullName, ServiceLayer layer)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + @"\themes");
        if (!di.Exists)
        {
            return String.Empty;
        }

        foreach (var fi in di.GetFiles("*.xml"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            ServiceLayer l = new ServiceLayer();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            l.Load(xmlStream);

            if (l.Name == layer.Name)
            {
                return "services/miscellaneous/generaltilecache/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }
    #endregion

    #region IPersistable
    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
        _guid = (string)stream.Load("guid", String.Empty);

        _rendering = (TileGridRendering)stream.Load("rendering", (int)TileGridRendering.Quality);

        //MaxLevel = (int)stream.Load("maxlevel", (int)-1);
        HideBeyondMaxLevel = (bool)stream.Load("hide_beyond_maxlevel", false);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
        stream.Save("guid", _guid);

        stream.Save("rendering", (int)_rendering);

        //stream.Save("maxlevel", this.MaxLevel);
        stream.Save("hide_beyond_maxlevel", this.HideBeyondMaxLevel);
    }
    #endregion

    protected override void BeforeCopy()
    {
        base.BeforeCopy();
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Allgemeiner TileCache"; }
    }
}

public class GeneralTileCacheProperties : SchemaNode, IEditable
{
    private TileGridOrientation _origin = TileGridOrientation.UpperLeft;
    private Extent _extent = new Extent();
    private double[] _resolutions;
    private string _tileUrl = String.Empty, _tilePath = String.Empty;
    private int _tileWidth = 256, _tileHeight = 256, _projId = -1;

    #region Properties
    //[TypeConverter(typeof(ExpandableObjectConverter))]
    [DisplayName("Ausdehnung")]
    [Category("Ausprägung")]
    public Extent TileCacheExtent
    {
        get { return _extent; }
        set { _extent = value; }
    }

    [DisplayName("Lage des Ursprunges")]
    [Category("Ausprägung")]
    public TileGridOrientation Origin
    {
        get { return _origin; }
        set { _origin = value; }
    }

    [DisplayName("Kartenprojektion")]
    [Category("Ausprägung")]
    //[Editor(typeof(TypeEditor.Proj4TypeEditor), typeof(TypeEditor.ITypeEditor))]
    public int ProjId
    {
        get { return _projId; }
        set { _projId = value; }
    }

    [DisplayName("Auflösungen (Resolutions)")]
    [Category("Ebenen")]
    public double[] Resolutions
    {
        get { return _resolutions; }
        set { _resolutions = value; }
    }

    [DisplayName("Url für Tiles")]
    [Category("Tiles")]
    public string TileUrl
    {
        get { return _tileUrl; }
        set { _tileUrl = value; }
    }

    [DisplayName("Dateisystem-Pfad für Tiles (optional)")]
    [Category("Optional")]
    public string TilePath
    {
        get { return _tilePath; }
        set { _tilePath = value; }
    }

    private string _domains;
    [DisplayName("Domains für Url")]
    [Description("Dieses Domains werden in der Url zufällig beim Platzhalter {0} einsetzt")]
    [Category("Optional")]
    public string[] Domains
    {
        get
        {
            return _domains.Split('|');
        }
        set
        {
            _domains = String.Empty;
            if (value != null)
            {
                foreach (string domain in value)
                {
                    if (!String.IsNullOrEmpty(_domains))
                    {
                        _domains += "|";
                    }

                    _domains += domain;
                }
            }
        }
    }

    [DisplayName("Breite in Pixel")]
    [Category("Tiles")]
    public int TileWidth
    {
        get { return _tileWidth; }
        set { _tileWidth = value; }
    }

    [DisplayName("Höhe in Pixel")]
    [Category("Tiles")]
    public int TileHeight
    {
        get { return _tileHeight; }
        set { _tileHeight = value; }
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _extent.Load(stream);

        int i = 0;
        double? res = null;
        List<double> resList = new List<double>();
        while ((res = (double?)stream.Load("res" + i, null)) != null)
        {
            resList.Add((double)res);
            i++;
        }
        _resolutions = resList.ToArray();

        _origin = (TileGridOrientation)stream.Load("origin", (int)TileGridOrientation.UpperLeft);
        _tileUrl = (string)stream.Load("tileurl", String.Empty);
        _tilePath = (string)stream.Load("tilepath", String.Empty);
        _tileWidth = (int)stream.Load("tilewidth", 256);
        _tileHeight = (int)stream.Load("tileheight", 256);

        _domains = (string)stream.Load("domains", String.Empty);
        _projId = (int)stream.Load("projid", -1);
    }

    public void Save(IStreamDocument stream)
    {
        _extent.Save(stream);

        for (int i = 0; i < 99; i++)
        {
            if (stream.Remove("res" + i) == false)
            {
                break;
            }
        }

        if (_resolutions != null)
        {
            int i = 0;
            foreach (double res in _resolutions)
            {
                stream.Save("res" + i, res);
                i++;
            }
        }
        stream.Save("origin", (int)_origin);
        stream.Save("tileurl", _tileUrl);
        stream.Save("tilepath", _tilePath);
        stream.Save("tilewidth", _tileWidth);
        stream.Save("tileheight", _tileHeight);

        if (!String.IsNullOrEmpty(_domains))
        {
            stream.Save("domains", _domains);
        }

        stream.Save("projid", _projId);
    }

    #endregion

    #region HelperClasses
    public class Extent : IPersistable
    {
        double _minx = 0.0, _miny = 0.0, _maxx = 0.0, _maxy = 0.0;

        #region Properties
        public double MinX
        {
            get { return _minx; }
            set { _minx = value; }
        }
        public double MinY
        {
            get { return _miny; }
            set { _miny = value; }
        }
        public double MaxX
        {
            get { return _maxx; }
            set { _maxx = value; }
        }
        public double MaxY
        {
            get { return _maxy; }
            set { _maxy = value; }
        }
        #endregion

        #region IPersistable Member

        public void Load(IStreamDocument stream)
        {
            _minx = (double)stream.Load("minx", 0.0);
            _miny = (double)stream.Load("miny", 0.0);
            _maxx = (double)stream.Load("maxx", 0.0);
            _maxy = (double)stream.Load("maxy", 0.0);
        }

        public void Save(IStreamDocument stream)
        {
            stream.Save("minx", _minx);
            stream.Save("miny", _miny);
            stream.Save("maxx", _maxx);
            stream.Save("maxy", _maxy);
        }

        #endregion

        public override string ToString()
        {
            return "BBOX:" + _minx.ToString() + ";" + _miny.ToString() + ";" + _maxx.ToString() + ";" + _maxy.ToString();
        }
    }
    #endregion
}
