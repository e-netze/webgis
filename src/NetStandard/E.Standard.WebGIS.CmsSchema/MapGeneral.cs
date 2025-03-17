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
    [DisplayName("Suchergebnis Darstellung")]
    [Category("Darstellung")]
    public FeatureTableType FeatureTableType
    {
        get { return _featureTableType; }
        set { _featureTableType = value; }
    }
    [DisplayName("Kartenprojektion")]
    [Category("Koordinatensystem")]
    [Editor(typeof(Proj4TypeEditor), typeof(ITypeEditor))]
    public int ProjId
    {
        get { return _projId; }
        set { _projId = value; }
    }
    [DisplayName("Kartenprojektion (ESRI Datum)")]
    [Category("Koordinatensystem")]
    [Editor(typeof(Proj4TypeEditor), typeof(ITypeEditor))]
    public int EsriDatum
    {
        get { return _esriDatum; }
        set { _esriDatum = value; }
    }
    [DisplayName("Name des Browserfensters (f�r Hotlinks)")]
    [Category("Browserfenster")]
    public string BrowserWindowName
    {
        get { return _browserWindowName; }
        set { _browserWindowName = value; }
    }

    [DisplayName("Standardkarte f�r Rolle")]
    [Category("UserManagement")]
    public string DefaultForRole
    {
        get { return _defaultForRole; }
        set { _defaultForRole = value; }
    }

    [DisplayName("Standardkarte f�r Benutzer")]
    [Category("UserManagement")]
    public string DefaultForUser
    {
        get { return _defaultForUser; }
        set { _defaultForUser = value; }
    }
    [DisplayName("Karte als WMS exportierbar")]
    [Category("Export")]
    public bool ExportWMS
    {
        get { return _exportwms; }
        set { _exportwms = value; }
    }

    [Browsable(true)]
    [DisplayName("Vorschau Bild")]
    [Category("Allgemein")]
    public string ThumbNail
    {
        get { return _thumbnail; }
        set { _thumbnail = value; }
    }

    [Browsable(true)]
    [DisplayName("Beschreibung")]
    [Category("Allgemein")]
    public string Description
    {
        get { return _description; }
        set { _description = value; }
    }

    [Browsable(true)]
    [DisplayName("Karten Tipps im TOC anzeigen")]
    [Category("Allgemein")]
    public bool ShowMapTipsToc
    {
        get { return _showMapTipsToc; }
        set { _showMapTipsToc = value; }
    }

    [Browsable(true)]
    [DisplayName("Warning Level")]
    [Description("Gibt an, ab wann Fehler in der Karte angezeigt werden")]
    [Category("Diagnostics")]
    public ServiceDiagnosticsWarningLevel WaringLevel
    {
        get; set;
    }

    [DisplayName("Basemap Transparenz Klassen")]
    [Description("Gibt Transparenzwerte an, die der Anwender �ber das Basemap Control ausw�hlen kann")]
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
