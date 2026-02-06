using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using E.Standard.WebGIS.CmsSchema.UI;
using E.Standard.WebMapping.Core;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class MapGeneral : CopyableNode, IUI, ICreatable, IEditable, IDisplayName
{
    private FeatureTableType _featureTableType = FeatureTableType.Default;
    private int _projId = -1, _esriDatum = -1;
    private string _browserWindowName = "webgis3";
    private string _defaultForRole = String.Empty, _defaultForUser = String.Empty;
    private bool _exportwms = false, _showMapTipsToc = true;
    private string _thumbnail = String.Empty, _description = String.Empty;

    public MapGeneral()
    {
        base.StoreUrl = false;
        base.Create = true;
    }

    #region Properties
    [DisplayName("#feature_table_type")]
    [Category("#category_feature_table_type")]
    public FeatureTableType FeatureTableType
    {
        get { return _featureTableType; }
        set { _featureTableType = value; }
    }
    [DisplayName("#proj_id")]
    [Category("#category_proj_id")]
    [Editor(typeof(Proj4TypeEditor), typeof(ITypeEditor))]
    public int ProjId
    {
        get { return _projId; }
        set { _projId = value; }
    }
    [DisplayName("#esri_datum")]
    [Category("#category_esri_datum")]
    [Editor(typeof(Proj4TypeEditor), typeof(ITypeEditor))]
    public int EsriDatum
    {
        get { return _esriDatum; }
        set { _esriDatum = value; }
    }
    [DisplayName("#browser_window_name")]
    [Category("#category_browser_window_name")]
    public string BrowserWindowName
    {
        get { return _browserWindowName; }
        set { _browserWindowName = value; }
    }

    [DisplayName("#default_for_role")]
    [Category("#category_default_for_role")]
    public string DefaultForRole
    {
        get { return _defaultForRole; }
        set { _defaultForRole = value; }
    }

    [DisplayName("#default_for_user")]
    [Category("#category_default_for_user")]
    public string DefaultForUser
    {
        get { return _defaultForUser; }
        set { _defaultForUser = value; }
    }
    [DisplayName("#export_w_m_s")]
    [Category("#category_export_w_m_s")]
    public bool ExportWMS
    {
        get { return _exportwms; }
        set { _exportwms = value; }
    }

    [Browsable(true)]
    [DisplayName("#thumb_nail")]
    [Category("#category_thumb_nail")]
    public string ThumbNail
    {
        get { return _thumbnail; }
        set { _thumbnail = value; }
    }

    [Browsable(true)]
    [DisplayName("#description")]
    [Category("#category_description")]
    public string Description
    {
        get { return _description; }
        set { _description = value; }
    }

    [Browsable(true)]
    [DisplayName("#show_map_tips_toc")]
    [Category("#category_show_map_tips_toc")]
    public bool ShowMapTipsToc
    {
        get { return _showMapTipsToc; }
        set { _showMapTipsToc = value; }
    }

    [Browsable(true)]
    [DisplayName("#waring_level")]
    [Category("#category_waring_level")]
    public ServiceDiagnosticsWarningLevel WaringLevel
    {
        get; set;
    }

    [DisplayName("Basemap Transparenz Klassen")]
    [Category("Darstellung")]
    public int[] BasemapOpacityClasses
    {
        get; set;
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        //base.Create = create;

        //IInitParameter ip = Helper.GetRelInstance("webgisCMS.UI.dll", "webgisCMS.UI.MapGeneralControl") as IInitParameter;
        //if (ip != null) ip.InitParameter = this;

        IInitParameter ip = new MapGeneralControl();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"\general";
        }
        else
        {
            return "general";
        }
    }
    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }
    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return Name; }
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _featureTableType = (FeatureTableType)stream.Load("ftabtype", (int)FeatureTableType.Default);
        _projId = (int)stream.Load("projid", -1);
        _esriDatum = (int)stream.Load("esridatum", -1);
        _browserWindowName = (string)stream.Load("browserwinname", "webgis3");

        _defaultForUser = (string)stream.Load("defaultforuser", String.Empty);
        _defaultForRole = (string)stream.Load("defaultforrole", String.Empty);

        _showMapTipsToc = (bool)stream.Load("showmaptipstoc", true);
        _exportwms = (bool)stream.Load("exportwms", false);

        _thumbnail = (string)stream.Load("thumbnail", String.Empty);
        _description = (string)stream.Load("description", String.Empty);

        this.WaringLevel = (ServiceDiagnosticsWarningLevel)stream.Load("warninglevel", (int)ServiceDiagnosticsWarningLevel.Never);

        string basmapOpacityClasses = (string)stream.Load("basemap_opacity_classes", String.Empty);
        if (!String.IsNullOrWhiteSpace(basmapOpacityClasses))
        {
            string[] classes = basmapOpacityClasses.Split(',');
            this.BasemapOpacityClasses = new int[classes.Length];
            for (int i = 0; i < classes.Length; i++)
            {
                this.BasemapOpacityClasses[i] = int.Parse(classes[i]);
            }
        }
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("ftabtype", (int)_featureTableType);
        stream.Save("projid", _projId);
        stream.Save("esridatum", _esriDatum);
        stream.Save("browserwinname", _browserWindowName);

        stream.Save("defaultforuser", _defaultForUser);
        stream.Save("defaultforrole", _defaultForRole);

        stream.Save("showmaptipstoc", _showMapTipsToc);
        stream.Save("exportwms", _exportwms);

        stream.Save("thumbnail", _thumbnail);
        stream.Save("description", _description);

        stream.Save("warninglevel", (int)this.WaringLevel);

        if (BasemapOpacityClasses != null)
        {
            stream.Save("basemap_opacity_classes", String.Join(",", BasemapOpacityClasses));
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Karte"; }
    }
}
