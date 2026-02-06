using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Schema.Reflection;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema;

public enum LegendOptimization
{
    None = 0,
    Themes = 1,
    Symbols = 2
}

public class ServiceLink : Link, IEditable, ISchemaNode, IOnCreateCmsNode, IDisplayNameDefault
{
    private string _tocDisplayName = String.Empty, _metadata = String.Empty;
    private string _tocName = "default";
    private ServiceProjection _projMethode = ServiceProjection.none;
    private int _projId = -1;
    private double _opacity = 100.0;
    private double _opacityFactor = 1.0;
    private bool _showInLegend = true;
    private LegendOptimization _legendOpt = LegendOptimization.None;
    private double _legendOptSymbolScale = 1000.0;
    private string _legendUrl = String.Empty;
    private int _timeout = 20;
    private bool _visible = true, _collapsed = false, _showInToc = true;
    private ServiceImageFormat _imageFormat = ServiceImageFormat.Default;
    private bool _useFixRefScale = false;
    private double _fixRefScale = 0.0;
    private double _minScale = 0.0;
    private double _maxScale = 0.0;
    private bool _useWithSpatialConstraintService = false;
    private bool _isBasemap = false;
    private BasemapType _basemapType = BasemapType.Normal;

    private bool _exportWms = false;
    private string _serviceExtentUrl = String.Empty;

    public ServiceLink()
    {
        _exportWms = false;
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("#opacity")]
    [Category("#category_opacity")]
    public double Opacity
    {
        get { return _opacity; }
        set { _opacity = value; }
    }

    [Browsable(true)]
    [DisplayName("#opacity_factor")]
    [Category("#category_opacity_factor")]
    public double OpacityFactor
    {
        get => _opacityFactor;
        set
        {
            if (value <= 0.001)
            {
                value = 1.0;  // null is not allowed here => Service shoud always be visible
            }

            _opacityFactor = Math.Clamp(value, 0.0, 1.0);
        }
    }

    [Browsable(true)]
    [DisplayName("#timeout")]
    [Category("#category_timeout")]
    [ObsoleteCmsPropeperty]
    public int Timeout
    {
        get { return _timeout; }
        set { _timeout = value; }
    }

    [Browsable(true)]
    [DisplayName("#image_format")]
    [Category("#category_image_format")]
    public ServiceImageFormat ImageFormat
    {
        get { return _imageFormat; }
        set { _imageFormat = value; }
    }

    [DisplayName("#meta_data")]
    [Category("#category_meta_data")]
    public string MetaData
    {
        get { return _metadata; }
        set { _metadata = value; }
    }

    [Browsable(true)]
    [DisplayName("#visible")]
    [Category("#category_visible")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }

    [Browsable(true)]
    [Category("~~#category_toc_display_name")]
    public string TocDisplayName
    {
        get { return _tocDisplayName; }
        set { _tocDisplayName = value; }
    }
    [Browsable(true)]
    [Category("#category_toc_name")]
    [Editor(typeof(TypeEditor.TocNameEditor), typeof(TypeEditor.ITypeEditor))]
    public string TocName
    {
        get { return _tocName; }
        set { _tocName = value; }
    }
    [Browsable(true)]
    [DisplayName("#collapsed")]
    [Category("#category_collapsed")]
    public bool Collapsed
    {
        get { return _collapsed; }
        set { _collapsed = value; }
    }
    [Browsable(true)]
    [DisplayName("#show_in_toc")]
    [Category("#category_show_in_toc")]
    public bool ShowInToc
    {
        get { return _showInToc; }
        set { _showInToc = value; }
    }

    [Browsable(true)]
    [Category("~~#category_projection_methode")]
    public ServiceProjection ProjectionMethode
    {
        get { return _projMethode; }
        set { _projMethode = value; }
    }

    [Browsable(true)]
    [Category("#category_projection_id")]
    //[Editor(typeof(TypeEditor.Proj4TypeEditor), typeof(TypeEditor.ITypeEditor))]
    public int ProjectionId
    {
        get { return _projId; }
        set { _projId = value; }
    }

    [Browsable(true)]
    [Category("Kartenprojektion")]
    public int[] DatumTransformations
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("#show_in_legend")]
    [Category("~#category_show_in_legend")]
    public bool ShowInLegend
    {
        get { return _showInLegend; }
        set { _showInLegend = value; }
    }
    [Browsable(true)]
    [DisplayName("#legend_opt_method")]
    [Category("#category_legend_opt_method")]
    public LegendOptimization LegendOptMethod
    {
        get { return _legendOpt; }
        set { _legendOpt = value; }
    }
    [Browsable(true)]
    [DisplayName("#legend_opt_symbol_scale")]
    [Category("#category_legend_opt_symbol_scale")]
    public double LegendOptSymbolScale
    {
        get { return _legendOptSymbolScale; }
        set { _legendOptSymbolScale = value; }
    }
    [Browsable(true)]
    [DisplayName("#legend_url")]
    [Category("#category_legend_url")]
    public string LegendUrl
    {
        get { return _legendUrl; }
        set { _legendUrl = value; }
    }

    [Browsable(true)]
    [DisplayName("#use_fix_ref_scale")]
    [Category("~~#category_use_fix_ref_scale")]
    public bool UseFixRefScale
    {
        get { return _useFixRefScale; }
        set { _useFixRefScale = value; }
    }
    [Browsable(true)]
    [DisplayName("#fix_ref_scale")]
    [Category("#category_fix_ref_scale")]
    public double FixRefScale
    {
        get { return _fixRefScale; }
        set { _fixRefScale = value; }
    }

    [Browsable(true)]
    [DisplayName("#min_scale")]
    [Category("~#category_min_scale")]
    public double MinScale
    {
        get { return _minScale; }
        set { _minScale = value; }
    }
    [Browsable(true)]
    [DisplayName("#max_scale")]
    [Category("#category_max_scale")]
    public double MaxScale
    {
        get { return _maxScale; }
        set { _maxScale = value; }
    }

    [Browsable(true)]
    [DisplayName("#use_with_spatial_constraint_service")]
    [Category("~#category_use_with_spatial_constraint_service")]
    public bool UseWithSpatialConstraintService
    {
        get { return _useWithSpatialConstraintService; }
        set { _useWithSpatialConstraintService = value; }
    }

    [DisplayName("#is_basemap")]
    [Category("~#category_is_basemap")]
    public bool IsBasemap
    {
        get { return _isBasemap; }
        set { _isBasemap = value; }
    }

    [DisplayName("#basemap_type")]
    [Category("#category_basemap_type")]
    public BasemapType BasemapType
    {
        get { return _basemapType; }
        set { _basemapType = value; }
    }

    [Category("#category_basemap_preview_image_url")]
    [DisplayName("#basemap_preview_image_url")]
    public string BasemapPreviewImageUrl { get; set; } = "";

    public int[] _supportedCrs = null;
    [DisplayName("Supported CRS")]
    [Category("~OGC Export")]
    public int[] SupportedCrs
    {
        get { return _supportedCrs; }
        set { _supportedCrs = value; }
    }

    [DisplayName("#export_w_m_s")]
    [Category("~#category_export_w_m_s")]
    [AuthorizablePropertyAttribute("exportwms", false)]
    public bool ExportWMS
    {
        get { return _exportWms; }
        set { _exportWms = value; }
    }

    [Browsable(true)]
    [DisplayName("#map_extent_url")]
    [Category("~#category_map_extent_url")]
    [Editor(typeof(TypeEditor.MapExtentsEditor), typeof(TypeEditor.ITypeEditor))]
    public string MapExtentUrl
    {
        get { return _serviceExtentUrl; }
        set { _serviceExtentUrl = value; }
    }

    [Browsable(true)]
    [DisplayName("#warning_level")]
    [Category("~#category_warning_level")]
    public ServiceDiagnosticsWarningLevel WarningLevel
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("#copyright_info")]
    [Category("#category_copyright_info")]
    [Editor(typeof(TypeEditor.CopyrightInfoEditor), typeof(TypeEditor.ITypeEditor))]
    public string CopyrightInfo
    {
        get; set;
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _tocDisplayName = (string)stream.Load("tocdisplayname", String.Empty);
        _tocName = (string)stream.Load("tocname", "default");
        _collapsed = (bool)stream.Load("collapsed", false);
        _showInToc = (bool)stream.Load("showintoc", true);
        this.CopyrightInfo = (string)stream.Load("copyright", String.Empty);

        _projMethode = (ServiceProjection)stream.Load("projmethode", (int)ServiceProjection.none);
        _projId = (int)stream.Load("projid", -1);
        string datums = (string)stream.Load("datums", null);
        if (!String.IsNullOrWhiteSpace(datums))
        {
            this.DatumTransformations = datums.Split(',').Select(d => int.Parse(d)).ToArray();
        }

        _opacity = (double)stream.Load("opacity", 100.0);
        this.OpacityFactor = Math.Clamp((double)stream.Load("opacity_factor", 1.0), 0f, 1);
        _timeout = (int)stream.Load("timeout", 20);
        _visible = (bool)stream.Load("visible", true);
        _imageFormat = (ServiceImageFormat)stream.Load("imageformat", (int)ServiceImageFormat.Default);

        _showInLegend = (bool)stream.Load("showinlegend", true);
        _legendOpt = (LegendOptimization)stream.Load("legendopt", (int)LegendOptimization.None);
        _legendOptSymbolScale = (double)stream.Load("legendoptsymbolscale", 1000.0);
        _legendUrl = (string)stream.Load("legendurl", String.Empty);

        _useFixRefScale = (bool)stream.Load("usefixrefscale", false);
        _fixRefScale = (double)stream.Load("fixrefscale", 0.0);

        _minScale = (double)stream.Load("minscale", 0.0);
        _maxScale = (double)stream.Load("maxscale", 0.0);

        _metadata = (string)stream.Load("metadata", String.Empty);

        _useWithSpatialConstraintService = (bool)stream.Load("usewithspatialconstraintservice", false);

        _isBasemap = (bool)stream.Load("isbasemap", false);
        _basemapType = (BasemapType)stream.Load("basemaptype", (int)BasemapType.Normal);
        this.BasemapPreviewImageUrl = (string)stream.Load("basemap_previewimageurl", String.Empty);

        this.WarningLevel = (ServiceDiagnosticsWarningLevel)stream.Load("warninglevel", (int)ServiceDiagnosticsWarningLevel.Never);

        string supportedcrs = (string)stream.Load("supportedcrs", String.Empty);
        if (!String.IsNullOrEmpty(supportedcrs))
        {
            string[] crs = supportedcrs.Split(',');
            _supportedCrs = new int[crs.Length];
            for (int i = 0; i < crs.Length; i++)
            {
                _supportedCrs[i] = int.Parse(crs[i]);
            }
        }

        _exportWms = (bool)stream.Load("exportwms", false);
        _serviceExtentUrl = (string)stream.Load("serviceextenturl", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("tocdisplayname", _tocDisplayName);
        stream.Save("tocname", _tocName);
        stream.Save("collapsed", _collapsed);
        stream.Save("showintoc", _showInToc);
        stream.Save("copyright", this.CopyrightInfo ?? String.Empty);

        stream.Save("projmethode", (int)_projMethode);
        stream.Save("projid", _projId);
        stream.SaveOrRemoveIfEmpty("datums",
            this.DatumTransformations != null && this.DatumTransformations.Length > 0 ?
                String.Join(",", this.DatumTransformations) :
                null);

        stream.Save("opacity", _opacity);
        stream.Save("opacity_factor",
            Math.Clamp(
                _opacityFactor <= 0.001   // avoid 0 here => service will be invisible forever
                    ? 1.0 :
                    _opacityFactor,
                0.0, 1.0));
        stream.Save("timeout", _timeout);
        stream.Save("visible", _visible);
        stream.Save("imageformat", (int)_imageFormat);

        stream.Save("showinlegend", _showInLegend);
        stream.Save("legendopt", (int)_legendOpt);
        stream.Save("legendoptsymbolscale", _legendOptSymbolScale);
        stream.Save("legendurl", _legendUrl);

        stream.Save("usefixrefscale", _useFixRefScale);
        stream.Save("fixrefscale", _fixRefScale);

        stream.Save("minscale", _minScale);
        stream.Save("maxscale", _maxScale);

        stream.Save("metadata", _metadata);

        stream.Save("usewithspatialconstraintservice", _useWithSpatialConstraintService);

        stream.Save("isbasemap", _isBasemap);
        stream.Save("basemaptype", (int)_basemapType);
        stream.SaveOrRemoveIfEmpty("basemap_previewimageurl", this.BasemapPreviewImageUrl);

        stream.Save("warninglevel", (int)this.WarningLevel);

        if (_supportedCrs != null && _supportedCrs.Length > 0)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int crs in _supportedCrs)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                sb.Append(crs);
            }
            stream.Save("supportedcrs", sb.ToString());
        }
        else
        {
            stream.Remove("supportedcrs");
        }

        stream.Save("exportwms", _exportWms, false);
        stream.Save("serviceextenturl", _serviceExtentUrl);
    }

    #region ISchemaNode Member

    private string _relPath = String.Empty;
    [Browsable(false)]
    public string RelativePath
    {
        get
        {
            return _relPath;
        }
        set
        {
            _relPath = value;
        }
    }

    private CMSManager _cms = null;
    [Browsable(false)]
    public CMSManager CmsManager
    {
        get
        {
            return _cms;
        }
        set
        {
            _cms = value;
        }
    }

    public string DisplayName
    {
        get
        {
            return String.Empty;  // Target Name (wenn zB bei Karten eingebunden wurde);
        }
    }

    public string DefaultDisplayName => "Advanced Settings";

    #endregion

    #region IOnCreateCmsNode

    public void OnCreateCmsNode(string path)
    {
        path = path.ToLower().Replace("\\", "/");
        if (path.Contains("/ogc/wmts/") ||
            path.Contains("/arcgisserver/wmtsservice/") ||
            path.Contains("/arcgisserver/tileservice/") ||
            path.Contains("/miscellaneous/generaltilecache/") ||
            path.Contains("/miscellaneous/generalvectortilecache/"))
        {
            this.IsBasemap = true;
        }
    }

    #endregion
}

public class OverlayServiceLink : Link, IEditableWrapper
{
    ServiceLink _link = null;

    #region Properties
    [Browsable(true)]
    [Category("#category_display_name")]
    public string DisplayName
    {
        get
        {
            if (_link != null)
            {
                return _link.TocDisplayName;
            }

            return String.Empty;
        }
        set
        {
            if (_link != null)
            {
                _link.TocDisplayName = value;
            }
        }
    }

    [Browsable(true)]
    [Category("#category_toc_name")]
    [Editor(typeof(TypeEditor.TocNameEditor), typeof(TypeEditor.ITypeEditor))]
    public string TocName
    {
        get
        {
            if (_link != null)
            {
                return _link.TocName;
            }

            return String.Empty;
        }
        set
        {
            if (_link != null)
            {
                _link.TocName = value;
            }
        }
    }

    [Browsable(true)]
    [Category("#category_projection_methode")]
    public ServiceProjection ProjectionMethode
    {
        get
        {
            if (_link != null)
            {
                return _link.ProjectionMethode;
            }

            return ServiceProjection.none;
        }
        set
        {
            if (_link != null)
            {
                _link.ProjectionMethode = value;
            }
        }
    }

    [Browsable(true)]
    [Category("#category_projection_id")]
    //[Editor(typeof(TypeEditor.Proj4TypeEditor), typeof(TypeEditor.ITypeEditor))]
    public int ProjectionId
    {
        get
        {
            if (_link != null)
            {
                return _link.ProjectionId;
            }

            return -1;
        }
        set
        {
            if (_link != null)
            {
                _link.ProjectionId = value;
            }
        }
    }
    #endregion

    #region IEditableWrapper Member

    [Browsable(false)]
    public IEditable WrappedObject
    {
        get
        {
            return _link;
        }
        set
        {
            _link = value as ServiceLink;
        }
    }

    #endregion
}

public class SpatialConstraintServiceLink : Link, IEditable
{
    private string _queryLayerId = String.Empty;
    private string _serviceUrlFieldName = String.Empty;

    [DisplayName("#query_layer_id")]
    [Category("#category_query_layer_id")]
    public string QueryLayerId
    {
        get { return _queryLayerId; }
        set { _queryLayerId = value; }
    }

    [DisplayName("#service_url_field_name")]
    [Category("#category_service_url_field_name")]
    public string ServiceUrlFieldName
    {
        get { return _serviceUrlFieldName; }
        set { _serviceUrlFieldName = value; }
    }

    #region IEditable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _queryLayerId = (string)stream.Load("querylayerid", String.Empty);
        _serviceUrlFieldName = (string)stream.Load("serviceurlfieldname", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("querylayerid", _queryLayerId);
        stream.Save("serviceurlfieldname", _serviceUrlFieldName);
    }
    #endregion
}

public class ServiceLinkGdi : Link, IEditable, ISchemaNode
{
    #region ISchemaNode Member

    private string _relPath = String.Empty;
    [Browsable(false)]
    public string RelativePath
    {
        get
        {
            return _relPath;
        }
        set
        {
            _relPath = value;
        }
    }

    private CMSManager _cms = null;
    [Browsable(false)]
    public CMSManager CmsManager
    {
        get
        {
            return _cms;
        }
        set
        {
            _cms = value;
        }
    }

    #endregion
}

public class MapServiceLink : Link, IEditable
{
    [Browsable(true)]
    [DisplayName("#layer_visibility")]
    [Category("#category_layer_visibility")]
    public MapServiceLayerVisibility LayerVisibility { get; set; }

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        LayerVisibility = (MapServiceLayerVisibility)stream.Load("layer_visibility", (int)MapServiceLayerVisibility.ServiceDefaults);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("layer_visibility", (int)LayerVisibility);
    }
}
