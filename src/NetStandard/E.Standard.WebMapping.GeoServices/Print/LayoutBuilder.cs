using E.Standard.CMS.Core;
using E.Standard.DbConnector;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.Print;

public enum PageSize
{
    A4 = 1,
    A3 = 2,
    A2 = 3,
    A1 = 4,
    A0 = 5,
    A4_A3 = 6,
    A4_A2 = 7,
    A4_A1 = 8,
    A4_A0 = 9,
    A3_A2 = 10,
    A3_A1 = 11,
    A2_A1 = 12
}
public enum PageOrientation
{
    Fixed = 0,
    Portrait = 1,
    Landscape = 2
}
public struct simpleRect { public int width, height; }

public class LayoutBuilder
{
    private readonly IMap _map;
    private readonly XmlDocument _doc = null;
    private readonly XmlNode _root = null;
    private string _errMsg = String.Empty, _rootPath = "", _mapPath = "", _legendPath = "", _ovmapPath = "";
    private PageSize _pageSize = PageSize.A4;
    private PageOrientation _pageOrientation = PageOrientation.Portrait;
    private double _dpi = 96.0;
    //private double _border = 38.1;
    private double _minx = 0.0, _miny = 0.0, _maxx = 0.0, _maxy = 0.0, _scale = 0.0, _rotation = 0.0;
    private string _user = "", _purpose = "", _section = "", _title = "";
    private readonly List<LayoutUserText> _userTextList = null;
    private string _headerID = String.Empty, _coord_format = String.Empty;
    private GeometricTransformer _transformer = null;
    private IHttpService _http;

    public LayoutBuilder(IMap map, IHttpService http, string filename)
    {
        try
        {
            _errMsg = String.Empty;
            _map = map;
            _http = http;

            if (_map != null)
            {
                _rotation = _map.DisplayRotation;
            }

            FileInfo fi = new FileInfo(filename);
            _doc = new XmlDocument();
            var xml = File.ReadAllText(filename);
            _doc.LoadXml(xml);

            _root = _doc.SelectSingleNode("//layout");

            foreach (XmlNode includeNode in _doc.SelectNodes("//include[@file]"))
            {
                try
                {
                    string file = fi.Directory.FullName + @"\" + includeNode.Attributes["file"].Value;
                    XmlDocument includeDoc = new XmlDocument();
                    includeDoc.Load(file);

                    XmlNode includeRoot = includeDoc.SelectSingleNode("include");
                    if (includeRoot == null)
                    {
                        continue;
                    }

                    foreach (XmlNode child in includeRoot.ChildNodes)
                    {
                        XmlNode clone = _doc.ImportNode(child, true);
                        includeNode.ParentNode.InsertBefore(clone, includeNode);
                    }
                    includeNode.ParentNode.RemoveChild(includeNode);

                }
                catch (Exception ex)
                {
                    _errMsg = ex.Message;
                    _doc = null;
                }
            }

            if (_root != null)
            {
                XmlNodeList variablesNode = _root.SelectNodes("variables");
                if (variablesNode.Count > 0)
                {
                    _userTextList = new List<LayoutUserText>();
                    foreach (XmlNode variables in variablesNode)  // variablesNode.Count sollte eigentlich eh immer 1 sein... aber wurscht
                    {
                        foreach (XmlNode variable in variables.SelectNodes("variable"))
                        {
                            if (variable.Attributes["name"] == null || variable.Attributes["alias"] == null)
                            {
                                continue;
                            }

                            _userTextList.Add(new LayoutUserText(variable.Attributes["name"].Value,
                                                                 variable.Attributes["alias"].Value));
                            if (variable.Attributes["default"] != null)
                            {
                                _userTextList[_userTextList.Count - 1].Default = ParseVariableValue(map, variable.Attributes["default"].Value);
                            }

                            if (variable.Attributes["maxlength"] != null)
                            {
                                try
                                {
                                    _userTextList[_userTextList.Count - 1].MaxLength = int.Parse(variable.Attributes["maxlength"].Value);
                                }
                                catch { }
                            }

                            if (variable.Attributes["visible"] != null)
                            {
                                try
                                {
                                    _userTextList[_userTextList.Count - 1].Visible = bool.Parse(variable.Attributes["visible"].Value.ToLower());
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _errMsg = ex.Message;
            _doc = null;
        }
    }
    public LayoutBuilder(IMap map, IHttpService http, string filename, PageSize size, PageOrientation orientation)
        : this(map, http, filename)
    {
        this.PageSize = size;
        this.PageOrientation = orientation;
    }
    public LayoutBuilder(IMap map, IHttpService http, string filename, PageSize size, PageOrientation orientation, double dpi, string rootPath = "")
        : this(map, http, filename, size, orientation)
    {
        this.DotsPerInch = dpi;
        _rootPath = rootPath;
    }

    #region Properties
    public PageSize PageSize
    {
        get { return _pageSize; }
        set { _pageSize = GetAllowedPageSize(value); }
    }
    public PageOrientation PageOrientation
    {
        get { return _pageOrientation; }
        set { _pageOrientation = GetAllowedPageOrientation(value); }
    }
    public double DotsPerInch
    {
        get { return _dpi; }
        set { _dpi = PageDpi(value); }
    }
    public string RootPath
    {
        get { return _rootPath; }
        set { _rootPath = value; }
    }
    public string MapPath
    {
        get { return _mapPath; }
        set { _mapPath = value; }
    }
    public string OverviewMapPath
    {
        get { return _ovmapPath; }
        set { _ovmapPath = value; }
    }
    public string LegendPath
    {
        get { return _legendPath; }
        set { _legendPath = value; }
    }
    public double CoordLeft { get { return _minx; } set { _minx = value; } }
    public double CoordBottom { get { return _miny; } set { _miny = value; } }
    public double CoordRight { get { return _maxx; } set { _maxx = value; } }
    public double CoordTop { get { return _maxy; } set { _maxy = value; } }
    public double Scale { get { return _scale; } set { _scale = value; } }
    public double DisplayRotation { get { return _rotation; } set { _rotation = value; } }
    public string TextUser { get { return _user; } set { _user = value; } }
    public string TextPurpose { get { return _purpose; } set { _purpose = value; } }
    public string TextSection { get { return _section; } set { _section = value; } }
    public string TextTitle { get { return _title; } set { _title = value; } }
    public string HeaderID { get { return _headerID; } set { _headerID = value; } }
    public bool HasHeaderIDQuery
    {
        get { return this.HeaderIDNode != null; }
    }
    public string HeaderIDQueryUrl
    {
        get
        {
            XmlNode headerIdNode = this.HeaderIDNode;
            if (headerIdNode == null)
            {
                return String.Empty;
            }

            return headerIdNode.Attributes["query"].Value;
        }
    }
    public string HeaderIDQueryField
    {
        get
        {
            XmlNode headerIdNode = this.HeaderIDNode;
            if (headerIdNode == null)
            {
                return String.Empty;
            }

            return headerIdNode.Attributes["field"].Value;
        }
    }

    public IMap Map { get { return _map; } }

    public IEnumerable<int> GetAllowedScales()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@scales]");
        if (layoutNode == null)
        {
            return null;
        }

        return layoutNode.Attributes["scales"].Value
                                              .Split(',')
                                              .Select(s => int.Parse(s));
    }

    public IEnumerable<(PageSize size, PageOrientation orientation)> GetAllowedFormats()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@page_sizes and @page_orientations]");
        if (layoutNode == null)
        {
            return null;
        }

        List<(PageSize size, PageOrientation orientation)> formats = new List<(PageSize size, PageOrientation orientation)>();
        var pageSizes = layoutNode.Attributes["page_sizes"].Value
                                              .Split(',')
                                              .Select(s => (PageSize)Enum.Parse(typeof(PageSize), s, true));
        var pageOrientations = layoutNode.Attributes["page_orientations"].Value
                                              .Split(',')
                                              .Select(s => (PageOrientation)Enum.Parse(typeof(PageOrientation), s, true));

        foreach (var pageSize in pageSizes)
        {
            foreach (var pageOrientation in pageOrientations)
            {
                formats.Add((pageSize, pageOrientation));
            }
        }

        return formats.Count() > 0 ? formats : null;
    }

    public PageSize GetAllowedPageSize(PageSize defaultSize)
    {
        var allowedFormats = GetAllowedFormats();
        if (allowedFormats == null || allowedFormats.Count() == 0)
        {
            return defaultSize;
        }

        if (allowedFormats.Any(f => f.size == defaultSize))
        {
            return defaultSize;
        }

        return allowedFormats.First().size;
    }

    public PageOrientation GetAllowedPageOrientation(PageOrientation defaultPageOrientation)
    {
        var allowedFormats = GetAllowedFormats();
        if (allowedFormats == null || allowedFormats.Count() == 0)
        {
            return defaultPageOrientation;
        }

        if (allowedFormats.Any(f => f.orientation == defaultPageOrientation))
        {
            return defaultPageOrientation;
        }

        return allowedFormats.First().orientation;
    }

    public double PageDpi(double defaultDpi)
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@page_dpi]");
        if (layoutNode == null)
        {
            return defaultDpi;
        }

        try
        {
            return double.Parse(layoutNode.Attributes["page_dpi"].Value);
        }
        catch
        {
            return defaultDpi;
        }
    }

    public int PageMapSrs(int defaultSrs)
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@map_srs]");
        if (layoutNode == null)
        {
            return defaultSrs;
        }

        try
        {
            return int.Parse(layoutNode.Attributes["map_srs"].Value);
        }
        catch
        {
            return defaultSrs;
        }
    }

    public IEnumerable<string> GetShowLayers()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@show_layers]");
        if (layoutNode == null)
        {
            return null;
        }

        return layoutNode.Attributes["show_layers"].Value.Split(',').Select(v => v.Trim()).Where(v => !String.IsNullOrEmpty(v));
    }

    public IEnumerable<string> GetHideLayers()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@hide_layers]");
        if (layoutNode == null)
        {
            return null;
        }

        return layoutNode.Attributes["hide_layers"].Value.Split(',').Select(v => v.Trim()).Where(v => !String.IsNullOrEmpty(v));
    }

    public bool AllowedWithPlotService()
    {
        var pageSizes = GetPlotServicesPageSizes();
        var pageOrientations = GetPlotServicesOrientations();

        return pageSizes != null && pageSizes.Count() > 0 &&
               pageOrientations != null && pageOrientations.Count() > 0;
    }


    public IEnumerable<PageSize> GetPlotServicesPageSizes()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@plot_service_page_sizes]");
        if (layoutNode == null)
        {
            return null;
        }

        try
        {
            return layoutNode.Attributes["plot_service_page_sizes"].Value
                                              .Split(',')
                                              .Select(s => (PageSize)Enum.Parse(typeof(PageSize), s, true));
        }
        catch { return null; }
    }

    public IEnumerable<PageOrientation> GetPlotServicesOrientations()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@plot_service_page_orientations]");
        if (layoutNode == null)
        {
            return null;
        }

        try
        {
            return layoutNode.Attributes["plot_service_page_orientations"].Value
                                              .Split(',')
                                              .Select(s => (PageOrientation)Enum.Parse(typeof(PageOrientation), s, true));
        }
        catch { return null; }
    }

    public IEnumerable<(PageSize size, PageOrientation orientation)> GetPlotServicePageFormats()
    {
        if (!AllowedWithPlotService())
        {
            return null;
        }

        List<(PageSize size, PageOrientation orientation)> formats = new List<(PageSize size, PageOrientation orientation)>();

        foreach (var size in GetPlotServicesPageSizes())
        {
            foreach (var orientation in GetPlotServicesOrientations())
            {
                formats.Add((size, orientation));
            }
        }

        return formats;
    }

    public Envelope GetOvMapBBox()
    {
        XmlNode layoutNode = _doc?.SelectSingleNode("layout[@ovmap_bbox]");
        if (layoutNode == null)
        {
            return null;
        }

        var bbox = layoutNode.Attributes["ovmap_bbox"].Value.Split(',').Select(c => c.ToPlatformDouble()).ToArray();
        if (bbox.Length == 4)
        {
            return new Envelope(bbox);
        }

        return null;
    }

    #endregion

    #region private Members
    internal int mm2pixel(double mm)
    {
        double dpmm = _dpi / 25.4;
        return (int)(mm * dpmm);
    }

    private simpleRect GetPageSize(PageSize ps, PageOrientation po)
    {
        simpleRect rect;
        rect.width = 0;
        rect.height = 0;
        double dpmm = DotsPerInch / 25.4, fitter = .1;
        if (DotsPerInch == 0)
        {
            dpmm = 1.0;
        }

        switch (ps)
        {
            case PageSize.A4:  // 210x297
                rect.width = (int)((210.0) * dpmm);
                rect.height = (int)((297.0) * dpmm);
                break;
            case PageSize.A3: // 297x420
                rect.width = (int)((297.0) * dpmm);
                rect.height = (int)((420.0) * dpmm);
                break;
            case PageSize.A2: // 420x594
                rect.width = (int)((420.0) * dpmm);
                rect.height = (int)((594.0) * dpmm);
                break;
            case PageSize.A1: // 594x841
                rect.width = (int)((594.0) * dpmm);
                rect.height = (int)((841.0) * dpmm);
                break;
            case PageSize.A0: // 841x1189
                rect.width = (int)((841.0) * dpmm);
                rect.height = (int)((1189.0) * dpmm);
                break;

            case PageSize.A4_A3:
                rect.width = (int)((420.0) * dpmm);
                rect.height = (int)((297.0) * dpmm);
                break;
            case PageSize.A4_A2:
                rect.width = (int)((594.0) * dpmm);
                rect.height = (int)((297.0) * dpmm);
                break;
            case PageSize.A4_A1:
                rect.width = (int)((841.0) * dpmm);
                rect.height = (int)((297.0) * dpmm);
                break;
            case PageSize.A4_A0:
                rect.width = (int)((1189.0) * dpmm);
                rect.height = (int)((297.0) * dpmm);
                break;

            case PageSize.A3_A2:
                rect.width = (int)((594.0) * dpmm);
                rect.height = (int)((420.0) * dpmm);
                break;
            case PageSize.A3_A1:
                rect.width = (int)((841.0) * dpmm);
                rect.height = (int)((420.0) * dpmm);
                break;

            case PageSize.A2_A1:
                rect.width = (int)((841.0) * dpmm);
                rect.height = (int)((594.0) * dpmm);
                break;
        }
        if (po == PageOrientation.Landscape)
        {
            int w = rect.width;
            rect.width = rect.height;
            rect.height = w;
        }

        rect.width -= (int)((this.BorderLeft + this.BorderRight + fitter) * dpmm);
        rect.height -= (int)((this.BorderTop + this.BorderBottom + fitter) * dpmm);

        return rect;
    }

    private void ParseXml(LayoutPanel parent)
    {
        if (_root == null || parent == null || parent.XmlNode == null)
        {
            return;
        }

        foreach (XmlNode child in parent.XmlNode.ChildNodes)
        {
            switch (child.Name)
            {
                case "panel":
                case "map":
                case "ovmap":
                case "legend":
                case "scalebar":
                case "northarrow":
                    ParseXml(new LayoutPanel(this, parent, child));
                    break;
            }
        }
    }

    private void GetPanel(LayoutPanel parent, LayoutPanel.PanelType type, ref LayoutPanel p)
    {
        p = null;
        GetPanel(parent, type, false, ref p);
    }

    private void GetPanel(LayoutPanel parent, LayoutPanel.PanelType type, bool findAll, ref LayoutPanel p)
    {
        /*
        if (parent == null) return null;
        if (parent.Type == type) return parent;

        foreach (LayoutPanel child in parent.ChildPanels)
        {
            if (child.Type == type) return child;
            LayoutPanel panel = GetPanel(child, type);
            if (panel != null) return panel;
        }
        return null;
         * */

        if (p != null && p.Type == type)
        {
            return;
        }

        if (_root == null || parent == null || parent.XmlNode == null)
        {
            return;
        }

        foreach (XmlNode child in parent.XmlNode.ChildNodes)
        {
            if (child.Attributes == null)
            {
                continue; // wenn child Kommentar ist...
            }

            if (child.Attributes["if_constraint"] != null)
            {
                if (!TestConstraint(child.Attributes["if_constraint"].Value))
                {
                    continue;
                }
            }
            if (child.Attributes["if_not_constraint"] != null)
            {
                if (TestConstraint(child.Attributes["if_not_constraint"].Value))
                {
                    continue;
                }
            }

            switch (child.Name)
            {
                case "panel":
                case "map":
                case "ovmap":
                case "legend":
                case "scalebar":
                case "northarrow":
                case "overview_window":
                    LayoutPanel panel = new LayoutPanel(this, parent, child);
                    if (panel.Type == type)
                    {
                        if (findAll)
                        {
                            if (!(p is LayoutPanelCollection))
                            {
                                p = new LayoutPanelCollection();
                            } ((LayoutPanelCollection)p).Panels.Add(panel);
                            GetPanel(panel, type, findAll, ref p);
                        }
                        else
                        {
                            p = panel;
                            return;
                        }

                    }
                    else
                    {
                        GetPanel(panel, type, findAll, ref p);
                    }
                    break;
            }
        }
    }

    private LayoutPanel GetMapPanel(LayoutPanel parent)
    {
        LayoutPanel p = null;
        GetPanel(parent, LayoutPanel.PanelType.map, ref p);
        return p;
    }
    private LayoutPanel GetOvMapPanel(LayoutPanel parent)
    {
        LayoutPanel p = null;
        GetPanel(parent, LayoutPanel.PanelType.ovmap, ref p);
        return p;
    }
    private LayoutPanel GetLegendPanel(LayoutPanel parent)
    {
        LayoutPanel p = null;
        GetPanel(parent, LayoutPanel.PanelType.legend, ref p);
        return p;
    }
    private List<LayoutPanel> GetOverview_WindowPanels(LayoutPanel parent)
    {
        LayoutPanel p = null;

        GetPanel(parent, LayoutPanel.PanelType.overview_window, true, ref p);
        if (p is LayoutPanelCollection)
        {
            return ((LayoutPanelCollection)p).Panels;
        }

        return new List<LayoutPanel>();
    }

    private SpatialReference LayoutSpatialReference
    {
        get
        {
            try
            {
                XmlNode layoutNode = _doc?.SelectSingleNode("layout[@coord_srs]");
                if (layoutNode == null)
                {
                    return null;
                }

                return CoreApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(layoutNode.Attributes["coord_srs"].Value));
            }
            catch { return null; }
        }
    }
    private string CoordFormat
    {
        get
        {
            XmlNode layoutNode = _doc.SelectSingleNode("layout[@coord_format]");
            if (layoutNode == null)
            {
                return String.Empty;
            }

            return layoutNode.Attributes["coord_format"].Value;
        }
    }

    private double Border(string attribute, double defaultBorder = 19.0)
    {
        try
        {
            if (_doc == null)
            {
                return defaultBorder;
            }

            XmlNode borderNode = _doc.SelectSingleNode("layout[@" + attribute + "]");
            if (borderNode != null)
            {
                return borderNode.Attributes[attribute].Value.ToPlatformDouble();
            }
        }
        catch
        {
        }
        return defaultBorder;
    }

    public double BorderTop
    {
        get => Border("border-top", Border("border"));
    }

    public double BorderBottom
    {
        get => Border("border-bottom", Border("border"));
    }

    public double BorderLeft
    {
        get => Border("border-left", Border("border"));
    }

    public double BorderRight
    {
        get => Border("border-right", Border("border"));
    }

    #endregion

    public void ReplaceParameters(string[] args)
    {
        foreach (string arg in args)
        {
            int pos = arg.IndexOf("=");
            if (pos < 0)
            {
                continue;
            }

            string parameter = "[" + arg.Substring(0, pos) + "]";
            string val = arg.Substring(pos + 1, arg.Length - pos - 1);

            ReplaceParameters(_doc, parameter, val);
        }
    }
    private void ReplaceParameters(XmlNode node, string arg, string val)
    {

        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                if (!String.IsNullOrEmpty(child.InnerText))
                {
                    child.InnerText = child.InnerText.Replace(arg, val);
                }

                foreach (XmlAttribute attribute in child.Attributes)
                {
                    attribute.Value = attribute.Value.Replace(arg, val);
                }
            }
            ReplaceParameters(child, arg, val);
        }
    }

    public CanvasSize MapPixels
    {
        get
        {
            if (_root == null)
            {
                return new CanvasSize(0, 0);
            }

            simpleRect rect = GetPageSize(_pageSize, _pageOrientation);
            LayoutPanel rootPanel = new LayoutPanel(this, new CanvasPointF(0, 0), new CanvasSizeF(rect.width, rect.height), _root);

            LayoutPanel map = GetMapPanel(rootPanel);

            if (map == null)
            {
                return new CanvasSize(0, 0);
            }

            return new CanvasSize((int)map.Size.Width, (int)map.Size.Height);
        }
    }
    public CanvasSize OverviewMapPixels
    {
        get
        {
            if (_root == null)
            {
                return new CanvasSize(0, 0);
            }

            simpleRect rect = GetPageSize(_pageSize, _pageOrientation);
            LayoutPanel rootPanel = new LayoutPanel(this, new CanvasPointF(0, 0), new CanvasSizeF(rect.width, rect.height), _root);

            LayoutPanel ovmap = GetOvMapPanel(rootPanel);

            if (ovmap == null)
            {
                return new CanvasSize(0, 0);
            }

            return new CanvasSize((int)ovmap.Size.Width, (int)ovmap.Size.Height);
        }
    }
    public CanvasSize LegendPixels
    {
        get
        {
            if (_root == null)
            {
                return new CanvasSize(0, 0);
            }

            simpleRect rect = GetPageSize(_pageSize, _pageOrientation);
            LayoutPanel rootPanel = new LayoutPanel(this, new CanvasPointF(0, 0), new CanvasSizeF(rect.width, rect.height), _root);

            LayoutPanel legend = GetLegendPanel(rootPanel);

            if (legend == null)
            {
                return new CanvasSize(0, 0);
            }

            return new CanvasSize((int)legend.Size.Width, (int)legend.Size.Height);
        }
    }
    public Envelope MapEnvelope
    {
        get
        {
            double dpm = _dpi / 0.0254;
            CanvasSize pixels = this.MapPixels;

            double W = _scale * pixels.Width / dpm;
            double H = _scale * pixels.Height / dpm;

            return new Envelope(0, 0, W, H);
        }
    }

    public List<string> SubPages
    {
        get
        {
            List<string> ret = new List<string>();

            foreach (XmlNode subPageNode in _root.SelectNodes("subpages/subpage[@name]"))
            {
                ret.Add(subPageNode.Attributes["name"].Value);
            }

            return ret;
        }
    }

    private List<OverviewWindow> _fixedScaleMaps = null;
    public List<OverviewWindow> Overview_Windows
    {
        get
        {
            if (_fixedScaleMaps != null)
            {
                return _fixedScaleMaps;
            }

            if (_root == null)
            {
                return null;
            }

            simpleRect rect = GetPageSize(_pageSize, _pageOrientation);
            LayoutPanel rootPanel = new LayoutPanel(this, new CanvasPointF(0, 0), new CanvasSizeF(rect.width, rect.height), _root);

            List<LayoutPanel> smapPanels = GetOverview_WindowPanels(rootPanel);
            if (smapPanels == null || smapPanels.Count == 0)
            {
                return null;
            }

            List<OverviewWindow> fsms = new List<OverviewWindow>();
            foreach (LayoutPanel panel in smapPanels)
            {
                OverviewWindow fsm = new OverviewWindow();
                fsm.Size = panel.Size;
                fsm.Scale = panel._node.Attributes["scale"].Value.ToPlatformDouble();
                fsm.Presentations = panel._node.Attributes["presentations"] != null ? panel._node.Attributes["presentations"].Value : String.Empty;
                fsm.ImagePath = String.Empty;
                fsm._node = panel._node;

                fsms.Add(fsm);
            }

            return _fixedScaleMaps = fsms;
        }
    }

    public List<LayoutUserText> UserText
    {
        get
        {
            return _userTextList;
        }
    }

    async public Task<IBitmap> Draw()
    {
        try
        {
            _errMsg = String.Empty;
            simpleRect rect = GetPageSize(_pageSize, _pageOrientation);
            LayoutPanel rootPanel = new LayoutPanel(this, new CanvasPointF(0, 0), new CanvasSizeF(rect.width, rect.height), _root);

            SpatialReference layoutSRef = LayoutSpatialReference;
            if (_map != null && _map.SpatialReference != null && layoutSRef != null)
            {
                _transformer = new GeometricTransformer();
                _transformer.FromSpatialReference(_map.SpatialReference.Proj4, !_map.SpatialReference.IsProjective);
                _transformer.ToSpatialReference(layoutSRef.Proj4, !layoutSRef.IsProjective);
            }
            _coord_format = CoordFormat;

            IBitmap bitmap = Current.Engine.CreateBitmap((int)rootPanel.Size.Width, (int)rootPanel.Size.Height);

            await Draw(rootPanel, bitmap);

            return bitmap;

        }
        catch (Exception ex)
        {
            _errMsg = ex.Message;
            return null;
        }
        finally
        {
            if (_transformer != null)
            {
                _transformer.Dispose();
            }
        }
    }

    private int ToJpegIfGreater
    {
        get
        {
            try
            {
                if (_doc == null)
                {
                    return 0;
                }

                XmlNode layoutNode = _doc.SelectSingleNode("layout[@to_jpg_if_greater]");
                if (layoutNode == null)
                {
                    return 0;
                }

                return int.Parse(layoutNode.Attributes["to_jpg_if_greater"].Value);
            }
            catch { return 0; }
        }
    }

    private ImageFormat ImageFormat
    {
        get
        {
            try
            {
                if (_doc != null)
                {

                    XmlNode layoutNode = _doc.SelectSingleNode("layout[@image_format]");
                    if (layoutNode != null)
                    {
                        switch (layoutNode.Attributes["image_format"].Value.ToLower())
                        {
                            case "jpg":
                            case "jpeg":
                                return ImageFormat.Jpeg;
                            case "gif":
                                return ImageFormat.Gif;
                        }
                    }
                }
            }
            catch
            {

            }

            return ImageFormat.Png;
        }
    }

    async public Task<bool> Draw(string outputPath)
    {
        try
        {
            using (IBitmap bm = await Draw())
            {
                MemoryStream ms = new MemoryStream();

                var imageFormat = this.ImageFormat;
                bm.Save(ms, imageFormat);

                byte[] buffer = ms.ToArray();
                if (imageFormat != ImageFormat.Jpeg)
                {
                    if (ToJpegIfGreater > 0 && buffer.Length > ToJpegIfGreater * 1024 * 1024) // MB
                    {
                        ms = new MemoryStream();
                        bm.Save(ms, ImageFormat.Jpeg);
                        buffer = ms.ToArray();
                    }
                }

                //File.WriteAllBytes(outputPath, buffer);
                await buffer.SaveOrUpload(outputPath);

                return true;
            }
        }
        catch { }
        return false;
    }

    async private Task Draw(LayoutPanel parent, IBitmap bitmap)
    {
        if (parent == null)
        {
            return;
        }

        foreach (XmlNode child in parent.XmlNode.ChildNodes)
        {
            if (child.Attributes == null)
            {
                continue; // wenn child Kommentar ist...
            }

            if (child.Attributes["if_constraint"] != null)
            {
                if (!TestConstraint(child.Attributes["if_constraint"].Value))
                {
                    continue;
                }
            }
            if (child.Attributes["if_not_constraint"] != null)
            {
                if (TestConstraint(child.Attributes["if_not_constraint"].Value))
                {
                    continue;
                }
            }
            switch (child.Name)
            {
                case "panel":
                case "map":
                case "ovmap":
                case "legend":
                case "scalebar":
                case "northarrow":
                case "overview_window":
                    LayoutPanel map = new LayoutPanel(this, parent, child);
                    await map.Draw(bitmap, _http);
                    await Draw(map, bitmap);
                    break;
            }
        }
    }

    #region Coord 2 String
    private string deg2GMS(double deg, int digits)
    {
        int g = (int)Math.Floor(deg);
        deg -= g;
        int m = (int)Math.Floor(deg * 60);
        deg -= m / 60.0;
        double s = Math.Round(deg * 3600.0, 3);

        if (s >= 60.0) { m++; s = 0.0; }
        if (m == 60.0) { g++; m = 0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00}'{2:00" + digs + "}''", g, m, s);
        //return g.ToString()+"°"+m.ToString()+"'"+s.ToString()+"''";
    }
    private string deg2GM(double deg, int digits)
    {
        int g = (int)Math.Floor(deg);
        deg -= g;
        double m = deg * 60;
        if (m >= 60.0) { g++; m = 0.0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00" + digs + "}'", g, m);
    }
    private string Coord2String(double c, int digits)
    {
        switch (_coord_format.ToLower())
        {
            case "dms":
                return deg2GMS(c, digits);
            case "dm":
                return deg2GM(c, digits);
        }
        return Math.Round(c, digits).ToString();
    }
    #endregion

    internal string ReplaceText(string text)
    {
        text = text.Replace("[SCALE]", "1:" + ((int)Math.Round(_scale)).ToString());
        text = text.Replace("[DATE]", DateTime.Now.ToShortDateString());
        text = text.Replace("[TIME]", DateTime.Now.ToShortTimeString());
        text = text.Replace("[EPSG]", _map?.SpatialReference?.Id.ToString() ?? "");
        text = text.Replace("[MAP_SRS_NAME]", $"{_map?.SpatialReference?.Name}"); 
        text = text.Replace("[PAGE_SIZE]", _pageSize.ToString());

        //   ul---------------------ur
        //   |                       |
        //   |           p0          |
        //   |                       |
        //   ll---------------------lr

        double minx = _minx, miny = _miny, maxx = _maxx, maxy = _maxy;
        if (_transformer != null)
        {
            Core.Geometry.Point p1 = new Core.Geometry.Point(minx, miny);
            Core.Geometry.Point p2 = new Core.Geometry.Point(maxx, maxy);
            _transformer.Transform2D(p1);
            _transformer.Transform2D(p2);

            minx = p1.X; miny = p1.Y;
            maxx = p2.X; maxy = p2.Y;
        }

        Core.Geometry.Point p0 = new Core.Geometry.Point((minx + maxx) * 0.5, (miny + maxy) * 0.5);
        Core.Geometry.Point ll = new Core.Geometry.Point(minx, miny);
        Core.Geometry.Point lr = new Core.Geometry.Point(maxx, miny);
        Core.Geometry.Point ur = new Core.Geometry.Point(maxx, maxy);
        Core.Geometry.Point ul = new Core.Geometry.Point(minx, maxy);

        if (_map.DisplayRotation != 0.0)
        {
            Display.TransformPoint(_map, ll, p0);
            Display.TransformPoint(_map, lr, p0);
            Display.TransformPoint(_map, ur, p0);
            Display.TransformPoint(_map, ul, p0);
        }

        text = text.Replace("[COORD_CENTER_X_0]", Coord2String(p0.X, 0));
        text = text.Replace("[COORD_CENTER_Y_0]", Coord2String(p0.Y, 0));
        text = text.Replace("[COORD_CENTER_X_1]", Coord2String(p0.X, 1));
        text = text.Replace("[COORD_CENTER_Y_1]", Coord2String(p0.Y, 1));
        text = text.Replace("[COORD_CENTER_X_2]", Coord2String(p0.X, 2));
        text = text.Replace("[COORD_CENTER_Y_2]", Coord2String(p0.Y, 2));
        text = text.Replace("[COORD_CENTER_X_3]", Coord2String(p0.X, 3));
        text = text.Replace("[COORD_CENTER_Y_3]", Coord2String(p0.Y, 3));

        text = text.Replace("[COORD_CENTER_EASTING_0]", Coord2String(p0.X, 0));
        text = text.Replace("[COORD_CENTER_NORTHING_0]", Coord2String(p0.Y, 0));
        text = text.Replace("[COORD_CENTER_EASTING_1]", Coord2String(p0.X, 1));
        text = text.Replace("[COORD_CENTER_NORTHING_1]", Coord2String(p0.Y, 1));
        text = text.Replace("[COORD_CENTER_EASTING_2]", Coord2String(p0.X, 2));
        text = text.Replace("[COORD_CENTER_NORTHING_2]", Coord2String(p0.Y, 2));
        text = text.Replace("[COORD_CENTER_EASTING_3]", Coord2String(p0.X, 3));
        text = text.Replace("[COORD_CENTER_NORTHING_3]", Coord2String(p0.Y, 3));

        #region New (gedreht möglich)

        text = text.Replace("[COORD_LL_NORTHING_0]", Coord2String(ll.Y, 0));
        text = text.Replace("[COORD_LL_NORTHING_1]", Coord2String(ll.Y, 1));
        text = text.Replace("[COORD_LL_NORTHING_2]", Coord2String(ll.Y, 2));
        text = text.Replace("[COORD_LL_NORTHING_3]", Coord2String(ll.Y, 3));

        text = text.Replace("[COORD_LL_EASTING_0]", Coord2String(ll.X, 0));
        text = text.Replace("[COORD_LL_EASTING_1]", Coord2String(ll.X, 1));
        text = text.Replace("[COORD_LL_EASTING_2]", Coord2String(ll.X, 2));
        text = text.Replace("[COORD_LL_EASTING_3]", Coord2String(ll.X, 3));

        text = text.Replace("[COORD_LR_NORTHING_0]", Coord2String(lr.Y, 0));
        text = text.Replace("[COORD_LR_NORTHING_1]", Coord2String(lr.Y, 1));
        text = text.Replace("[COORD_LR_NORTHING_2]", Coord2String(lr.Y, 2));
        text = text.Replace("[COORD_LR_NORTHING_3]", Coord2String(lr.Y, 3));

        text = text.Replace("[COORD_LR_EASTING_0]", Coord2String(lr.X, 0));
        text = text.Replace("[COORD_LR_EASTING_1]", Coord2String(lr.X, 1));
        text = text.Replace("[COORD_LR_EASTING_2]", Coord2String(lr.X, 2));
        text = text.Replace("[COORD_LR_EASTING_3]", Coord2String(lr.X, 3));

        text = text.Replace("[COORD_UR_NORTHING_0]", Coord2String(ur.Y, 0));
        text = text.Replace("[COORD_UR_NORTHING_1]", Coord2String(ur.Y, 1));
        text = text.Replace("[COORD_UR_NORTHING_2]", Coord2String(ur.Y, 2));
        text = text.Replace("[COORD_UR_NORTHING_3]", Coord2String(ur.Y, 3));

        text = text.Replace("[COORD_UR_EASTING_0]", Coord2String(ur.X, 0));
        text = text.Replace("[COORD_UR_EASTING_1]", Coord2String(ur.X, 1));
        text = text.Replace("[COORD_UR_EASTING_2]", Coord2String(ur.X, 2));
        text = text.Replace("[COORD_UR_EASTING_3]", Coord2String(ur.X, 3));

        text = text.Replace("[COORD_UL_NORTHING_0]", Coord2String(ul.Y, 0));
        text = text.Replace("[COORD_UL_NORTHING_1]", Coord2String(ul.Y, 1));
        text = text.Replace("[COORD_UL_NORTHING_2]", Coord2String(ul.Y, 2));
        text = text.Replace("[COORD_UL_NORTHING_3]", Coord2String(ul.Y, 3));

        text = text.Replace("[COORD_UL_EASTING_0]", Coord2String(ul.X, 0));
        text = text.Replace("[COORD_UL_EASTING_1]", Coord2String(ul.X, 1));
        text = text.Replace("[COORD_UL_EASTING_2]", Coord2String(ul.X, 2));
        text = text.Replace("[COORD_UL_EASTING_3]", Coord2String(ul.X, 3));
        #endregion

        #region Old

        if (_map.DisplayRotation == 0.0)
        {
            text = text.Replace("[COORD_LEFT_0]", Coord2String(minx, 0));
            text = text.Replace("[COORD_LEFT_1]", Coord2String(minx, 1));
            text = text.Replace("[COORD_LEFT_2]", Coord2String(minx, 2));
            text = text.Replace("[COORD_LEFT_3]", Coord2String(minx, 3));

            text = text.Replace("[COORD_RIGHT_0]", Coord2String(maxx, 0));
            text = text.Replace("[COORD_RIGHT_1]", Coord2String(maxx, 1));
            text = text.Replace("[COORD_RIGHT_2]", Coord2String(maxx, 2));
            text = text.Replace("[COORD_RIGHT_3]", Coord2String(maxx, 3));

            text = text.Replace("[COORD_BOTTOM_0]", Coord2String(miny, 0));
            text = text.Replace("[COORD_BOTTOM_1]", Coord2String(miny, 1));
            text = text.Replace("[COORD_BOTTOM_2]", Coord2String(miny, 2));
            text = text.Replace("[COORD_BOTTOM_3]", Coord2String(miny, 3));

            text = text.Replace("[COORD_TOP_0]", Coord2String(maxy, 0));
            text = text.Replace("[COORD_TOP_1]", Coord2String(maxy, 1));
            text = text.Replace("[COORD_TOP_2]", Coord2String(maxy, 2));
            text = text.Replace("[COORD_TOP_3]", Coord2String(maxy, 3));
        }
        else
        {
            text = text.Replace("[COORD_LEFT_0]", String.Empty);
            text = text.Replace("[COORD_LEFT_1]", String.Empty);
            text = text.Replace("[COORD_LEFT_2]", String.Empty);
            text = text.Replace("[COORD_LEFT_3]", String.Empty);

            text = text.Replace("[COORD_RIGHT_0]", String.Empty);
            text = text.Replace("[COORD_RIGHT_1]", String.Empty);
            text = text.Replace("[COORD_RIGHT_2]", String.Empty);
            text = text.Replace("[COORD_RIGHT_3]", String.Empty);

            text = text.Replace("[COORD_BOTTOM_0]", String.Empty);
            text = text.Replace("[COORD_BOTTOM_1]", String.Empty);
            text = text.Replace("[COORD_BOTTOM_2]", String.Empty);
            text = text.Replace("[COORD_BOTTOM_3]", String.Empty);

            text = text.Replace("[COORD_TOP_0]", String.Empty);
            text = text.Replace("[COORD_TOP_1]", String.Empty);
            text = text.Replace("[COORD_TOP_2]", String.Empty);
            text = text.Replace("[COORD_TOP_3]", String.Empty);
        }
        #endregion

        if (_userTextList != null)
        {
            foreach (LayoutUserText userText in _userTextList)
            {
                if (userText.Visible == false &&
                    !String.IsNullOrEmpty(userText.Default) &&
                    String.IsNullOrEmpty(userText.Value))
                {
                    userText.Value = userText.Default;   // Damit zb auch RoleParameter funktioniern, die nicht in der Maske angezeigt werden!!
                }

                switch (userText.Name)
                {
                    case "PURPOSE":
                        if (!String.IsNullOrWhiteSpace(_purpose))
                        {
                            continue;
                        }

                        break;
                    case "USER":
                        if (!String.IsNullOrWhiteSpace(_user))
                        {
                            continue;
                        }

                        break;
                    case "DPI":
                        continue;
                    case "SELCTION":
                        if (!String.IsNullOrWhiteSpace(_section))
                        {
                            continue;
                        }

                        break;
                    case "TITLE":
                        if (!String.IsNullOrWhiteSpace(_title))
                        {
                            continue;
                        }

                        break;
                }

                text = text.Replace("[" + userText.Name + "]", XmlReplace(userText.Value));
            }
        }

        text = text.Replace("[PURPOSE]", XmlReplace(_purpose));
        text = text.Replace("[USER]", XmlReplace(_user));
        text = text.Replace("[DPI]", ((int)_dpi).ToString());
        text = text.Replace("[SECTION]", XmlReplace(_section));
        text = text.Replace("[TITLE]", XmlReplace(_title));

        text = text.Replace("[HEADERID]", _headerID);

        if (text.Contains("[DATE("))
        {
            int pos1 = text.IndexOf("[DATE(") + 6;
            int pos2 = text.IndexOf(")]", pos1);
            if (pos1 != -1 && pos2 != -1)
            {
                string dateFormat = text.Substring(pos1, pos2 - pos1);
                text = text.Replace("[DATE(" + dateFormat + ")]", DateTime.Now.ToString(dateFormat));
            }
        }

        return text;
    }

    private string ParseVariableValue(IMap map, string val)
    {
        if (val.Contains(":") && map != null)
        {
            CmsDocument.UserIdentification ui = map.Environment.UserValue(webgisConst.UserIdentification, null) as CmsDocument.UserIdentification;

            if (val.StartsWith("role-parameter:") && ui != null)
            {
                string parameterName = val.Substring(15, val.Length - 15);
                string parameterVal = String.Empty;
                if (ui.UserrolesParameters != null)
                {
                    foreach (string roleParameter in ui.UserrolesParameters)
                    {
                        if (roleParameter.StartsWith(parameterName + "="))
                        {
                            parameterVal = roleParameter.Substring(parameterName.Length + 1, roleParameter.Length - parameterName.Length - 1);
                            break;
                        }
                    }
                }

                val = parameterVal;
            }
        }

        return val;
    }

    internal string XmlReplace(string text)
    {
        return text;
        //return text.Replace("&", "amp;").Replace("<", "&lt;").Replace(">", "&gt").Replace("\"", "@qoute;").Replace("'", "@apos;");
    }

    internal bool TestConstraint(string contraintID)
    {
        XmlNode constraintNode = _doc.SelectSingleNode("layout/constraints/constraint[@id='" + contraintID + "']");
        if (constraintNode == null)
        {
            return false;
        }

        if (constraintNode.Attributes["value"] == null ||
            constraintNode.Attributes["tester"] == null)
        {
            return false;
        }

        string value = ReplaceText(constraintNode.Attributes["value"].Value);
        string tester = ReplaceText(constraintNode.Attributes["tester"].Value);

        if (constraintNode.Attributes["connectionid"] != null)
        {
            string connectionID = constraintNode.Attributes["connectionid"].Value;
            XmlNode connNode = constraintNode.OwnerDocument.SelectSingleNode("layout/dbconnections/dbconnection[@id='" + connectionID + "']");
            if (connNode == null || connNode.Attributes["connectionstring"] == null)
            {
                return false;
            }

            using (var dbFactor = new DBFactory(connNode.Attributes["connectionstring"].Value))
            using (var dbConnection = dbFactor.GetConnection())
            using (var dbCommand = dbFactor.GetCommand(dbConnection))
            {
                dbCommand.CommandText = ReplaceText(value);

                dbConnection.Open();
                value = dbCommand.ExecuteScalar()?.ToString();
            }
        }

        return value?.Equals(tester) == true;
    }

    public string ErrorMessage
    {
        get { return _errMsg; }
    }

    private XmlNode HeaderIDNode
    {
        get
        {
            XmlNode headerIdNode = _doc.SelectSingleNode("layout/dbconnections/headerid[@query and @field]");
            return headerIdNode;
        }
    }
}

public class OverviewWindow
{
    public CanvasSizeF Size;
    public double Scale;
    public string Presentations;
    public string ImagePath;

    internal XmlNode _node;
}

internal class LayoutPanel
{
    public enum PanelType { unknown, panel, map, ovmap, legend, scalebar, northarrow, overview_window };

    private readonly LayoutBuilder _builder = null;
    private CanvasPointF _origin;
    private CanvasSizeF _size;
    private readonly LayoutPanel _parent;
    internal XmlNode _node;
    private readonly List<LayoutPanel> _child = new List<LayoutPanel>();

    public LayoutPanel(LayoutBuilder builder, CanvasPointF origin, CanvasSizeF size, XmlNode node)
    {
        _builder = builder;
        _origin = origin;
        _size = size;

        _parent = null;
        _node = node;
    }

    public LayoutPanel(LayoutBuilder builder, LayoutPanel parent, XmlNode node)
    {
        if (parent == null || builder == null)
        {
            return;
        }

        _builder = builder;

        _origin = new CanvasPointF(parent._origin.X, parent._origin.Y);
        _size = new CanvasSizeF(parent._size.Width, parent._size.Height);

        _node = node;
        _parent = parent;

        _parent._child.Add(this);
        if (_node == null)
        {
            return;
        }

        if (_node.Attributes["width"] != null)
        {
            if (_node.Attributes["width"].Value.EndsWith("%"))
            {
                string widthPercent = _node.Attributes["width"].Value.Substring(0, _node.Attributes["width"].Value.Length - 1);
                float width;
                if (widthPercent.TryToPlatformFloat(out width))
                {
                    _size.Width = parent._size.Width * width / 100;
                }
            }
            else
            {
                float width;
                if (_node.Attributes["width"].Value.TryToPlatformFloat(out width))
                {
                    _size.Width = _builder.mm2pixel(width);
                }
            }
        }
        else
        {
            _size.Height /= 4;
        }
        if (_node.Attributes["height"] != null)
        {
            if (_node.Attributes["height"].Value.EndsWith("%"))
            {
                string heightPercent = _node.Attributes["width"].Value.Substring(0, _node.Attributes["width"].Value.Length - 1);
                float height;
                if (heightPercent.TryToPlatformFloat(out height))
                {
                    _size.Height = parent._size.Height * height / 100;
                }
            }
            else
            {
                float height;
                if (_node.Attributes["height"].Value.TryToPlatformFloat(out height))
                {
                    _size.Height = _builder.mm2pixel(height);
                }
            }
        }
        else
        {
            _size.Height /= 4;
        }
        if (_node.Attributes["x"] != null)
        {
            float x;
            if (_node.Attributes["x"].Value.TryToPlatformFloat(out x))
            {
                _origin.X += _builder.mm2pixel(x);
            }
        }
        if (_node.Attributes["y"] != null)
        {
            float y;
            if (_node.Attributes["y"].Value.TryToPlatformFloat(out y))
            {
                _origin.Y += _builder.mm2pixel(y);
            }
        }
        if (_node.Attributes["dock"] != null)
        {
            switch (_node.Attributes["dock"].Value.ToLower())
            {
                case "left":
                    _origin.X = _parent._origin.X;
                    _origin.Y = _parent._origin.Y;
                    _size.Height = _parent._size.Height;

                    _parent._origin.X = _origin.X + _size.Width;
                    _parent._size.Width -= _size.Width;
                    break;
                case "top":
                    _origin.X = _parent._origin.X;
                    _origin.Y = _parent._origin.Y;
                    _size.Width = _parent._size.Width;

                    _parent._origin.Y = _origin.Y + _size.Height;
                    _parent._size.Height -= _size.Height;
                    break;
                case "right":
                    _origin.X = _parent._origin.X + _parent._size.Width - _size.Width;
                    _origin.Y = _parent._origin.Y;
                    _size.Height = _parent._size.Height;

                    _parent._size.Width -= _size.Width;
                    break;
                case "bottom":
                    _origin.X = _parent._origin.X;
                    _origin.Y = _parent._origin.Y + _parent._size.Height - _size.Height;
                    _size.Width = _parent._size.Width;

                    _parent._size.Height -= _size.Height;
                    break;
                case "fill":
                    _origin.X = _parent._origin.X;
                    _origin.Y = _parent._origin.Y;
                    _size.Width = _parent._size.Width;
                    _size.Height = _parent._size.Height;
                    break;
            }
        }
    }

    public XmlNode XmlNode
    {
        get
        {
            return _node;
        }
    }
    public PanelType Type
    {
        get
        {
            if (_node == null)
            {
                return PanelType.unknown;
            }

            switch (_node.Name)
            {
                case "panel":
                    return PanelType.panel;
                case "map":
                    return PanelType.map;
                case "ovmap":
                    return PanelType.ovmap;
                case "legend":
                    return PanelType.legend;
                case "scalebar":
                    return PanelType.scalebar;
                case "northarrow":
                    return PanelType.northarrow;
                case "overview_window":
                    return PanelType.overview_window;
                default:
                    return PanelType.unknown;
            }
        }
    }
    public List<LayoutPanel> ChildPanels
    {
        get { return _child; }
    }
    public CanvasSizeF Size
    {
        get { return _size; }
    }
    public CanvasPointF Origin
    {
        get { return _origin; }
    }

    async public Task Draw(IBitmap bitmap, IHttpService http)
    {
        if (_node == null || bitmap == null)
        {
            return;
        }

        using (var canvas = bitmap.CreateCanvas())
        {
            canvas.SetClip(new CanvasRectangleF(_origin.X, _origin.Y, _size.Width + 1, _size.Height + 1));

            using (IBrush brush = Brush(_node.Attributes["fillcolor"], ArgbColor.Transparent))
            {
                canvas.FillRectangle(brush, new CanvasRectangleF(_origin.X, _origin.Y, _size.Width, _size.Height));
            }

            try
            {
                if (Type == PanelType.map)
                {
                    using (var mapImage = await _builder.MapPath.ImageFromUri(http))
                    {
                        canvas.DrawBitmap(mapImage, _origin);
                    }
                }
                else if (Type == PanelType.ovmap)
                {
                    using (var ovmapImage = await _builder.OverviewMapPath.ImageFromUri(http))
                    {
                        canvas.DrawBitmap(ovmapImage, _origin);
                    }
                }
                else if (Type == PanelType.overview_window)
                {
                    List<OverviewWindow> fsms = _builder.Overview_Windows;
                    OverviewWindow fsm = null;
                    if (fsms != null)
                    {
                        foreach (OverviewWindow f in fsms)
                        {
                            if (f._node == this._node)
                            {
                                fsm = f;
                                break;
                            }
                        }
                    }
                    if (fsm != null)
                    {
                        try
                        {
                            using (var mapImage = await fsm.ImagePath.ImageFromUri(http))
                            {
                                canvas.DrawBitmap(mapImage, _origin);
                            }
                        }
                        catch { }
                    }
                }
                else if (Type == PanelType.legend)
                {
                    using (var legend = await _builder.LegendPath.BitmapFromUri(http))
                    {
                        using (var bm = FitLegend(legend, _builder.LegendPath))
                        {
                            if (bm != null)
                            {
                                if (bm.Width <= _size.Width && bm.Height <= _size.Height)
                                {
                                    canvas.DrawBitmap(bm, _origin);
                                }
                                else
                                {
                                    var destRectangle = new CanvasRectangleF(0, 0, bm.Width, bm.Height);
                                    if (_size.Width > 1 && _size.Height > 1)
                                    {
                                        while (true)
                                        {
                                            destRectangle.Width *= 0.95f;
                                            destRectangle.Height *= 0.95f;

                                            if (destRectangle.Width <= _size.Width && destRectangle.Height <= _size.Height)
                                            {
                                                break;
                                            }
                                        }
                                        destRectangle.Offset(_origin.X, _origin.Y);

                                        canvas.DrawBitmap(bm, destRectangle, new CanvasRectangleF(0, 0, bm.Width, bm.Height));
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Type == PanelType.scalebar)
                {
                    Graphics.Scalebar scaleBar = new Graphics.Scalebar(_builder.Scale, _builder.DotsPerInch, SystemInfo.DefaultFontName);
                    //int width = sb.ScaleBarWidth;
                    if (_node.Attributes["showtext"] != null && _node.Attributes["showtext"].Value == "false")
                    {
                        scaleBar.ShowText = false;
                    }

                    scaleBar.Create(canvas, (int)(_origin.X /*+ _size.Width / 2 - width / 2*/) + _builder.mm2pixel(2), (int)_origin.Y);
                }
                else if (Type == PanelType.northarrow)
                {
                    Graphics.NorthArrow na = new Graphics.NorthArrow(_builder.DisplayRotation);
                    canvas.TranslateTransform(new CanvasPointF(_origin.X, _origin.Y));
                    na.Create(canvas, (int)_size.Width, (int)_size.Height);
                    canvas.ResetTransform();
                }

                if (_node.Attributes["backcolor"] != null)
                {
                    using (var brush = this.Brush(_node.Attributes["backcolor"]))
                    {
                        var rect = new CanvasRectangleF(_origin.X, _origin.Y, _size.Width, _size.Height);
                        canvas.FillRectangle(brush, rect);
                    }
                }
                foreach (XmlNode child in _node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "text":
                            DrawText(canvas, child);
                            break;
                        case "image":
                            await DrawImage(canvas, http, child);
                            break;
                        case "line":
                            DrawLine(canvas, child);
                            break;
                        case "dbtext":
                            DrawDbText(canvas, child);
                            break;
                    }
                }
                if (_node.Attributes["border"] != null)
                {
                    using (var pen = this.Pen(_node.Attributes["bordercolor"]))
                    {
                        foreach (string border in _node.Attributes["border"].Value.Split(','))
                        {
                            switch (border.ToLower())
                            {
                                case "all":
                                    var rect = new CanvasRectangleF(_origin.X, _origin.Y, _size.Width, _size.Height);
                                    canvas.DrawRectangle(pen, new CanvasRectangleF(rect.Left, rect.Top, rect.Width - 1, rect.Height - 1));
                                    break;
                                case "left":
                                    canvas.DrawLine(pen,
                                        _origin,
                                        new CanvasPointF(_origin.X, _origin.Y + _size.Height - 1));
                                    break;
                                case "right":
                                    canvas.DrawLine(pen,
                                        new CanvasPointF(_origin.X + _size.Width - 1, _origin.Y),
                                        new CanvasPointF(_origin.X + _size.Width - 1, _origin.Y + _size.Height - 1));
                                    break;
                                case "top":
                                    canvas.DrawLine(pen,
                                        _origin,
                                        new CanvasPointF(_origin.X + _size.Width - 1, _origin.Y));
                                    break;
                                case "bottom":
                                    canvas.DrawLine(pen,
                                        new CanvasPointF(_origin.X, _origin.Y + _size.Height - 1),
                                        new CanvasPointF(_origin.X + _size.Width - 1, _origin.Y + _size.Height - 1));
                                    break;
                            }
                        }
                    }
                }
            }

            catch (Exception /*ex*/)
            {
            }
        }
    }

    private IPen Pen(XmlAttribute attr)
    {
        try
        {
            return Current.Engine.CreatePen(XmlColor(attr, ArgbColor.Black), 1);
        }
        catch
        {
        }
        return Current.Engine.CreatePen(ArgbColor.Black, 1);
    }

    private IBrush Brush(XmlAttribute attr)
    {
        return Brush(attr, ArgbColor.Black);
    }
    private IBrush Brush(XmlAttribute attr, ArgbColor defaultColor)
    {
        try
        {
            return Current.Engine.CreateSolidBrush(XmlColor(attr, defaultColor));
        }
        catch
        {
        }
        return Current.Engine.CreateSolidBrush(defaultColor);
    }

    private ArgbColor XmlColor(XmlAttribute attr, ArgbColor defaultColor)
    {
        if (attr == null)
        {
            return defaultColor;
        }

        try
        {
            string[] rgb = attr.Value.Split(',');
            if (rgb.Length == 3)
            {
                return ArgbColor.FromArgb(
                    Convert.ToInt32(rgb[0]),
                    Convert.ToInt32(rgb[1]),
                    Convert.ToInt32(rgb[2]));

            }
            if (rgb.Length == 4)
            {
                return ArgbColor.FromArgb(
                    Convert.ToInt32(rgb[0]),
                    Convert.ToInt32(rgb[1]),
                    Convert.ToInt32(rgb[2]),
                    Convert.ToInt32(rgb[3]));
            }
        }
        catch
        {
        }
        return defaultColor;
    }
    private IFont Font(XmlNode node)
    {
        if (node == null)
        {
            return Current.Engine.CreateFont(SystemInfo.DefaultFontName, 2);
        }

        string fontname = SystemInfo.DefaultFontName;
        float fontsize = 2;
        FontStyle fontstyle = FontStyle.Regular;

        try
        {
            if (node.Attributes["font"] != null)
            {
                fontname = node.Attributes["font"].Value;
            }

            if (node.Attributes["fontsize"] != null)
            {
                node.Attributes["fontsize"].Value.TryToPlatformFloat(out fontsize);
            }

            if (node.Attributes["fontstyle"] != null)
            {
                foreach (string style in node.Attributes["fontstyle"].Value.ToLower().Split(','))
                {
                    switch (style)
                    {
                        case "bold":
                            fontstyle |= FontStyle.Bold;
                            break;
                        case "italic":
                            fontstyle |= FontStyle.Italic;
                            break;
                    }
                }
            }
        }
        catch { }
        return Current.Engine.CreateFont(fontname, _builder.mm2pixel(Math.Max(fontsize, 0.1)), fontstyle);
    }

    private void DrawText(ICanvas canvas, XmlNode textNode)
    {
        if (textNode == null || textNode.Attributes["string"] == null)
        {
            return;
        }

        if (textNode.Attributes["if_constraint"] != null)
        {
            if (!_builder.TestConstraint(textNode.Attributes["if_constraint"].Value))
            {
                return;
            }
        }
        if (textNode.Attributes["if_not_constraint"] != null)
        {
            if (_builder.TestConstraint(textNode.Attributes["if_not_constraint"].Value))
            {
                return;
            }
        }

        try
        {
            float x = 1, y = 1;

            if (textNode.Attributes["x"] != null)
            {
                x = _builder.mm2pixel(textNode.Attributes["x"].Value.ToPlatformDouble());
            }

            if (textNode.Attributes["y"] != null)
            {
                y = _builder.mm2pixel(textNode.Attributes["y"].Value.ToPlatformDouble());
            }

            using (var brush = this.Brush(textNode.Attributes["fontcolor"]))
            {
                string text = _builder.ReplaceText(textNode.Attributes["string"].Value);
                using (var font = this.Font(textNode))
                {
                    if (textNode.Attributes["align"] != null)
                    {
                        var stringSize = canvas.MeasureText(text, font);

                        switch (textNode.Attributes["align"].Value.ToLower())
                        {
                            case "left":
                                x = 1;
                                break;
                            case "right":
                                x = _size.Width - stringSize.Width - 1;
                                break;
                            case "center":
                                x = _size.Width / 2 - stringSize.Width / 2;
                                break;
                        }
                    }

                    var format = Current.Engine.CreateDrawTextFormat();
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Near;

                    if (textNode.Attributes["halocolor"] != null)
                    {
                        using (var haloBrush = this.Brush(textNode.Attributes["halocolor"]))
                        {
                            for (int i = -2; i <= 2; i++)
                            {
                                for (int j = -2; j <= 2; j++)
                                {
                                    canvas.DrawText(
                                        text,
                                        font,
                                        haloBrush,
                                        _origin.X + x + i, _origin.Y + y + j,
                                        format);
                                }
                            }
                        }
                    }

                    canvas.DrawText(
                        text,
                        font,
                        brush,
                        _origin.X + x, _origin.Y + y,
                        format);

                }
            }
        }
        catch { }
    }

    private void DrawDbText(ICanvas canvas, XmlNode textNode)
    {
        if (textNode == null || textNode.Attributes["connectionid"] == null || textNode.Attributes["sql"] == null)
        {
            return;
        }

        if (textNode.Attributes["if_constraint"] != null)
        {
            if (!_builder.TestConstraint(textNode.Attributes["if_constraint"].Value))
            {
                return;
            }
        }
        if (textNode.Attributes["if_not_constraint"] != null)
        {
            if (_builder.TestConstraint(textNode.Attributes["if_not_constraint"].Value))
            {
                return;
            }
        }

        string connectionID = textNode.Attributes["connectionid"].Value;
        XmlNode connNode = textNode.OwnerDocument.SelectSingleNode("layout/dbconnections/dbconnection[@id='" + connectionID + "']");
        if (connNode == null || connNode.Attributes["connectionstring"] == null)
        {
            return;
        }

        string text = String.Empty;

        using (var dbFactor = new DBFactory(connNode.Attributes["connectionstring"].Value))
        using (var dbConnection = dbFactor.GetConnection())
        using (var dbCommand = dbFactor.GetCommand(dbConnection))
        {
            dbCommand.CommandText = _builder.ReplaceText(textNode.Attributes["sql"].Value);

            dbConnection.Open();
            text = dbCommand.ExecuteScalar()?.ToString();
        }

        if (String.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            float x = 1, y = 1;

            if (textNode.Attributes["x"] != null)
            {
                x = _builder.mm2pixel(textNode.Attributes["x"].Value.ToPlatformDouble());
            }

            if (textNode.Attributes["y"] != null)
            {
                y = _builder.mm2pixel(textNode.Attributes["y"].Value.ToPlatformDouble());
            }

            using (var brush = this.Brush(textNode.Attributes["fontcolor"]))
            {
                using (var font = this.Font(textNode))
                {
                    if (textNode.Attributes["align"] != null)
                    {
                        var stringSize = canvas.MeasureText(text, font);

                        switch (textNode.Attributes["align"].Value.ToLower())
                        {
                            case "left":
                                x = 1;
                                break;
                            case "right":
                                x = _size.Width - stringSize.Width - 1;
                                break;
                            case "center":
                                x = _size.Width / 2 - stringSize.Width / 2;
                                break;
                        }
                    }

                    if (textNode.Attributes["wrap"] != null &&
                        textNode.Attributes["wrap"].Value.ToLower() == "true")
                    {
                        text = WrapText(canvas, font, x, text);
                    }

                    var format = Current.Engine.CreateDrawTextFormat(); format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Near;

                    canvas.DrawText(
                        text,
                        font,
                        brush,
                        _origin.X + x, _origin.Y + y,
                        format);

                }
            }
        }
        catch { }
    }

    async private Task DrawImage(ICanvas canvas, IHttpService http, XmlNode imageNode)
    {
        if (imageNode.Attributes["src"] == null)
        {
            return;
        }

        if (imageNode.Attributes["if_constraint"] != null)
        {
            if (!_builder.TestConstraint(imageNode.Attributes["if_constraint"].Value))
            {
                return;
            }
        }
        if (imageNode.Attributes["if_not_constraint"] != null)
        {
            if (_builder.TestConstraint(imageNode.Attributes["if_not_constraint"].Value))
            {
                return;
            }
        }

        float x = _origin.X, y = _origin.Y, width = _size.Width, height = _size.Height;
        try
        {
            using (var img = await (_builder.RootPath != "" ? _builder.RootPath + @"/" + imageNode.Attributes["src"].Value : imageNode.Attributes["src"].Value).ImageFromUri(http))
            {
                if (imageNode.Attributes["x"] != null)
                {
                    x += _builder.mm2pixel(imageNode.Attributes["x"].Value.ToPlatformDouble());
                }

                if (imageNode.Attributes["y"] != null)
                {
                    y += _builder.mm2pixel(imageNode.Attributes["y"].Value.ToPlatformDouble());
                }

                if (imageNode.Attributes["width"] != null)
                {
                    width = _builder.mm2pixel(imageNode.Attributes["width"].Value.ToPlatformDouble());
                }

                if (imageNode.Attributes["height"] != null)
                {
                    height = _builder.mm2pixel(imageNode.Attributes["height"].Value.ToPlatformDouble());
                }

                if (imageNode.Attributes["align"] != null)
                {
                    switch (imageNode.Attributes["align"].Value.ToLower())
                    {
                        case "center":
                            x = _origin.X + _size.Width / 2f - width / 2f;
                            y = _origin.Y + _size.Height / 2f - height / 2f;
                            break;
                    }
                }

                canvas.DrawBitmap(img, new CanvasRectangleF(x, y, width, height), new CanvasRectangleF(0, 0, img.Width, img.Height));
            }
        }
        catch (Exception /*ex*/)
        {
        }
    }

    private void DrawLine(ICanvas canvas, XmlNode lineNode)
    {
        if (lineNode.Attributes["x1"] == null ||
            lineNode.Attributes["y1"] == null ||
            lineNode.Attributes["x2"] == null ||
            lineNode.Attributes["y2"] == null)
        {
            return;
        }

        if (lineNode.Attributes["if_constraint"] != null)
        {
            if (!_builder.TestConstraint(lineNode.Attributes["if_constraint"].Value))
            {
                return;
            }
        }
        if (lineNode.Attributes["if_not_constraint"] != null)
        {
            if (_builder.TestConstraint(lineNode.Attributes["if_not_constraint"].Value))
            {
                return;
            }
        }

        try
        {
            double x1 = lineNode.Attributes["x1"].Value.ToPlatformDouble();
            double y1 = lineNode.Attributes["y1"].Value.ToPlatformDouble();
            double x2 = lineNode.Attributes["x2"].Value.ToPlatformDouble();
            double y2 = lineNode.Attributes["y2"].Value.ToPlatformDouble();

            using (var pen = this.Pen(lineNode.Attributes["color"]))
            {
                canvas.DrawLine(pen,
                    (float)(_origin.X + _builder.mm2pixel(x1)),
                    (float)(_origin.Y + _builder.mm2pixel(y1)),
                    (float)(_origin.X + _builder.mm2pixel(x2)),
                    (float)(_origin.Y + _builder.mm2pixel(y2)));
            }
        }
        catch
        {
        }
    }

    private IBitmap ShrinkLegend(IBitmap bm)
    {
        try
        {
            var back = bm.GetPixel(0, 0);

            int newWidth = bm.Width;
            int newHeight = bm.Height;

            for (newWidth = bm.Width; newWidth > 0; newWidth--)
            {
                bool found = false;
                for (int y = 0; y < bm.Height; y++)
                {
                    if (!back.Equals(bm.GetPixel(newWidth - 1, y)))
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

            for (newHeight = bm.Height; newHeight > 0; newHeight--)
            {
                bool found = false;
                for (int x = 0; x < bm.Width; x++)
                {
                    if (!back.Equals(bm.GetPixel(x, newHeight - 1)))
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

            var newBitmap = Current.Engine.CreateBitmap(newWidth, newHeight);
            using (var canvas = newBitmap.CreateCanvas())
            {
                canvas.DrawBitmap(bm, new CanvasPoint(0, 0));
            }

            return newBitmap;
        }
        catch
        {
            return null;
        }
    }

    private CanvasRectangleF ResizeLegend(CanvasRectangleF rect)
    {
        int ch = (int)(rect.Height / _size.Height) + 1;
        int cb = (int)(_size.Width / rect.Width);

        if (cb < ch)
        {
            return ResizeLegend(new CanvasRectangleF(rect.Left, rect.Top, rect.Width * 0.95f, rect.Height * 0.95f));
        }
        else
        {
            return rect;
        }
    }

    private IBitmap FitLegend(IBitmap bm, string layoutPath)
    {
        IBitmap legend = null, shrink = null;
        try
        {
            // DPI nicht mehr umrechnen. Sollte schon richtig übergeben werden!
            /*
            FileInfo fi = new FileInfo(layoutPath);
            if (_builder.DotsPerInch != 96.0 && !fi.Name.ToLower().Contains("_dpi_"))
            {
                var o = new CanvasRectangleF(0, 0, bm.Width, bm.Height);
                var r = new CanvasRectangleF(0, 0, (float)bm.Width * (float)_builder.DotsPerInch / 96f, (float)bm.Height * (float)_builder.DotsPerInch / 96f);
                var tmp = Current.Engine.CreateBitmap((int)r.Width, (int)r.Height);
                using (var gr = tmp.CreateCanvas())
                {
                    gr.DrawBitmap(bm, r, o, GraphicsUnit.Pixel);
                }
                //bm.Dispose();
                bm = tmp;
            }
             */

            if (bm.Height <= (int)_size.Height)
            {
                return bm;
            }

            shrink = ShrinkLegend(bm);

            var original = new CanvasRectangleF(0, 0, shrink.Width, shrink.Height);
            var resized = ResizeLegend(original);

            if (!original.Equals(resized))
            {
                var tmp = Current.Engine.CreateBitmap((int)resized.Width, (int)resized.Height);
                using (var canvas = tmp.CreateCanvas())
                {
                    canvas.InterpolationMode = InterpolationMode.High;
                    canvas.DrawBitmap(shrink, resized, original);
                }
                shrink.Dispose();
                shrink = tmp;
            }

            int cols = Math.Min((int)(_size.Width / shrink.Width), (int)(shrink.Height / _size.Height) + 1);
            if (cols < 2)
            {
                return bm;
            }

            var back = shrink.GetPixel(0, 0);

            legend = Current.Engine.CreateBitmap(shrink.Width * cols, (int)_size.Height);
            using (var canvas = legend.CreateCanvas())
            {
                canvas.Clear(ArgbColor.White);
            }

            int y = 0;
            for (int col = 0; col < cols; col++)
            {
                int y0 = y;
                int miny = Math.Min(y + shrink.Height / cols, shrink.Height - 1);
                int maxy = Math.Min(y + (int)_size.Height, shrink.Height - 1);

                double sum_min = 1.0;
                int y_min = miny;
                for (y = miny; y < maxy; y++)
                {
                    double sum = 0.0;
                    for (int x = 0; x < shrink.Width; x++)
                    {
                        if (!back.Equals(shrink.GetPixel(x, y)))
                        {
                            sum += 1.0 / shrink.Width;
                        }
                    }
                    if (sum < sum_min)
                    {
                        sum_min = sum;
                        y_min = y;
                        if (sum_min == 0.0)
                        {
                            break;
                        }
                    }
                }
                y = y_min;

                using (var canvas = legend.CreateCanvas())
                {
                    var source = new CanvasRectangle(0, y0, shrink.Width, y - y0);
                    var dest = new CanvasRectangle(col * shrink.Width, 0, shrink.Width, y - y0);

                    canvas.InterpolationMode = InterpolationMode.High;
                    canvas.DrawBitmap(shrink, dest, source);
                }
            }
        }
        catch
        {
            if (legend != null)
            {
                legend.Dispose();
                legend = null;
            }
        }
        finally
        {
            if (shrink != null)
            {
                shrink.Dispose();
                shrink = null;
            }
        }
        return legend;
    }

    private string WrapText(ICanvas canvas, IFont font, float x, string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        text = text.Replace("\r\n", " ").Replace("\n\r", " ").Replace("\n", " ").Replace("\r", " ");
        StringBuilder sb = new StringBuilder();

        string[] words = text.Split(' ');
        string line = String.Empty;
        foreach (string word in words)
        {
            var size = canvas.MeasureText(line + (String.IsNullOrEmpty(line) ? "" : " ") + word, font);
            if (size.Width + 2 * x > _size.Width) // 2*x -> links und rechtes gleich viel Rand...
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }

                sb.Append(line);
                line = String.Empty;
            }
            if (!String.IsNullOrEmpty(line))
            {
                line += " ";
            }

            line += word;
        }
        if (!String.IsNullOrEmpty(line))
        {
            if (sb.Length > 0)
            {
                sb.Append("\n");
            }

            sb.Append(line);
        }
        return sb.ToString();
    }
}

internal class LayoutPanelCollection : LayoutPanel
{
    public List<LayoutPanel> _panels = new List<LayoutPanel>();

    public LayoutPanelCollection()
        : base(null, null, null)
    {
    }

    public List<LayoutPanel> Panels
    {
        get { return _panels; }
    }
}

public class LayoutUserText
{
    public string Name = "";
    public string Aliasname = "";
    public string Value = "";
    public string Default = String.Empty;
    public int MaxLength = -1;
    public bool Visible = true;

    public LayoutUserText(string name, string alias)
    {
        Name = name;
        Aliasname = alias;
    }
}
