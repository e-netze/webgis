using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class Presentation : CopyableXml, ICreatable, IEditable, IUI, IDisplayName
{
    protected string _layernames = String.Empty;
    //private PresentationCheckMode _gdi_mode = PresentationCheckMode.Button;
    //private PresentationAffecting _gdi_affecting = PresentationAffecting.Service;
    //private string _gdi_groupname = String.Empty;
    //private PresentationGroupStyle _gdi_groupstyle = PresentationGroupStyle.Button;
    private List<GdiProperties> _gdi_properties = null;
    private string _thumbnail = String.Empty, _description = String.Empty;
    private bool _useWithBasemap = false;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public Presentation(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("Sichtbare Layer")]
    [Category("Allgemein")]
    [Editor(typeof(TypeEditor.SelectLayersEditor), typeof(TypeEditor.ITypeEditor))]
    virtual public string LayerNames
    {
        get
        {
            return _layernames;
        }
        set
        {
            _layernames = value;
        }
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
    [Description("Beschreibung der Darstellungsvariante. Geben Sie in dieses Feld '#' ein, um automatisch als Beschreibung die betroffenen Layer aufzulisten.")]
    public string Description
    {
        get { return _description; }
        set { _description = value; }
    }

    [Category("~(WebGIS 4) Nur wenn Dienst mit Gdi verwendet wird")]
    [AuthorizablePropertyArray("gdiproperties")]
    [Editor(typeof(TypeEditor.GdiPropertiesPresentationEditor),
            typeof(TypeEditor.ITypeEditor))]
    //[Browsable(false)]
    public GdiProperties[] GdiPropertyArray
    {
        get { return _gdi_properties == null ? null : _gdi_properties.ToArray(); }
        set
        {
            _gdi_properties = (value == null ? null : new List<GdiProperties>(value));
        }
    }

    [Category("~(WebGIS 4) Nur wenn Dienst mit Gdi verwendet wird")]
    [DisplayName("Bei Basemap verwenden")]
    public bool UseForBasemap
    {
        get { return _useWithBasemap; }
        set { _useWithBasemap = value; }
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _layernames = (string)stream.Load("layers", String.Empty);
        _thumbnail = (string)stream.Load("thumbnail", String.Empty);
        _description = (string)stream.Load("description", String.Empty);
        _useWithBasemap = (bool)stream.Load("usewithbasemap", false);

        AuthorizableArray a = new AuthorizableArray("gdiproperties", this.GdiPropertyArray, typeof(GdiProperties));
        a.Load(stream);
        if (a.Array != null && a.Array.Length > 0)
        {
            _gdi_properties = new List<GdiProperties>();
            foreach (object o in a.Array)
            {
                if (o is GdiProperties)
                {
                    ((GdiProperties)o).Parent = this;
                    _gdi_properties.Add((GdiProperties)o);
                }
            }
        }
        else
        {
            _gdi_properties = null;
        }

        ApplySchemaNode2GdiProperties();
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("layers", _layernames);
        stream.Save("thumbnail", _thumbnail);
        stream.Save("description", _description);
        stream.Save("usewithbasemap", _useWithBasemap);

        AuthorizableArray a = new AuthorizableArray("gdiproperties", this.GdiPropertyArray, typeof(GdiProperties));
        a.Save(stream);
    }
    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        IInitParameter ip = new NewPresentationControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IDisplayName Member
    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region ISchemaNode

    public override string RelativePath
    {
        get
        {
            return base.RelativePath;
        }
        set
        {
            base.RelativePath = value;
            ApplySchemaNode2GdiProperties();
        }
    }

    public override CMSManager CmsManager
    {
        get
        {
            return base.CmsManager;
        }
        set
        {
            base.CmsManager = value;
            ApplySchemaNode2GdiProperties();
        }
    }

    #endregion

    private void ApplySchemaNode2GdiProperties()
    {
        if (_gdi_properties == null)
        {
            return;
        }

        foreach (GdiProperties gdi_prop in _gdi_properties)
        {
            gdi_prop.RelativePath = this.RelativePath;
            gdi_prop.CmsManager = this.CmsManager;
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Darstellungsvariante"; }
    }

    #region Helper Classes
    public class GdiProperties : AuthorizableArrayItem, ISchemaNode
    {
        private string _relativePath = String.Empty;
        private CMSManager _manager = null;

        private PresentationCheckMode _gdi_mode = PresentationCheckMode.Button;
        private PresentationAffecting _gdi_affecting = PresentationAffecting.Service;
        private string _gdi_groupname = String.Empty;
        private PresentationGroupStyle _gdi_groupstyle = PresentationGroupStyle.Button;
        private bool _gdi_isContainerDefault = false;
        private string parentUrl = String.Empty;
        private string _gdi_containerurl = String.Empty;
        private bool _visWithService = true;

        public PresentationCheckMode GdiDisplayStyle
        {
            get { return _gdi_mode; }
            set { _gdi_mode = value; }
        }
        [Browsable(true)]
        [Description("Gibt an, was von der Darstellungsvariante betroffen sein soll. Der jeweilige Dienst (service) oder die gesamte Karte (map). F�r Darstellungsvarianten mit Checkbox ist dieser Wert nicht relevant, da dort nur die angef�hrten Themen geschalten werden.")]
        public PresentationAffecting GdiAffecting
        {
            get { return _gdi_affecting; }
            set { _gdi_affecting = value; }
        }

        [Category("Gruppe")]
        public string GdiGroupName
        {
            get { return _gdi_groupname; }
            set { _gdi_groupname = value; }
        }
        [Category("Gruppe")]
        public PresentationGroupStyle GdiGroupDisplayStyle
        {
            get { return _gdi_groupstyle; }
            set { _gdi_groupstyle = value; }
        }
        [Category("Gruppe")]
        [DisplayName("Sichtbar, wenn dieser Dienst in Karte")]
        public bool VisibleWithService
        {
            get { return _visWithService; }
            set { _visWithService = value; }
        }
        [Category("Gruppe")]
        [DisplayName("Sichtbar, wenn einer dieser Dienste in der Karte vorkommt")]
        [Description("Liste der Service-Urls mit Beistrich getrennt")]
        public string VisibleWithOneOfServices { get; set; }

        [Category("Container")]
        [DisplayName("Default f�r Container")]
        public bool IsContainerDefault
        {
            get { return _gdi_isContainerDefault; }
            set { _gdi_isContainerDefault = value; }
        }

        [Category("Container")]
        [DisplayName("Container Url")]
        [Editor(typeof(TypeEditor.ContainerEditor), typeof(TypeEditor.ITypeEditor))]
        public string ContainerUrl
        {
            get { return _gdi_containerurl; }
            set { _gdi_containerurl = value; }
        }

        [Browsable(false)]
        internal Presentation Parent
        {
            set
            {
                if (value != null)
                {
                    parentUrl = value.Url;
                }
            }
        }

        public override string ToString()
        {
            string url = parentUrl;
            if (_gdi_groupname != null)
            {
                switch (_gdi_groupstyle)
                {
                    case PresentationGroupStyle.Button:
                    case PresentationGroupStyle.Checkbox:
                        url = "dvg_" + Globals.NameToUrl(_gdi_groupname);
                        break;
                    case PresentationGroupStyle.Dropdown:
                        url = "dvg_" + Globals.NameToUrl(_gdi_groupname) + "/" + url;
                        break;
                }
            }

            return "Gdi: (" + GdiDisplayStyle.ToString() + ", " + GdiAffecting.ToString() + ", " + GdiGroupName + ", " + GdiGroupDisplayStyle + ") url=" + url;
        }

        public override void Load()
        {
            this.GdiDisplayStyle = (PresentationCheckMode)this.StreamLoad("gdi_checkmode", (int)PresentationCheckMode.Button);
            this.GdiAffecting = (PresentationAffecting)this.StreamLoad("gdi_affecting", (int)PresentationAffecting.Service);

            this.GdiGroupName = (string)this.StreamLoad("gdi_groupname", String.Empty);
            this.GdiGroupDisplayStyle = (PresentationGroupStyle)this.StreamLoad("gdi_groupstyle", (int)PresentationGroupStyle.Button);
            this.VisibleWithService = (bool)this.StreamLoad("gdi_vis_with_service", true);
            this.VisibleWithOneOfServices = (string)this.StreamLoad("gdi_vis_with_on_of_services", String.Empty);

            this.IsContainerDefault = (bool)this.StreamLoad("gdi_containerdefault", false);
            this.ContainerUrl = (string)this.StreamLoad("gdi_containerurl", String.Empty);
        }

        public override void Save()
        {
            this.StreamSave("gdi_checkmode", (int)this.GdiDisplayStyle);
            this.StreamSave("gdi_affecting", (int)this.GdiAffecting);

            this.StreamSave("gdi_groupname", this.GdiGroupName);
            this.StreamSave("gdi_groupstyle", (int)this.GdiGroupDisplayStyle);
            this.StreamSave("gdi_vis_with_service", this.VisibleWithService);
            this.StreamSaveOrRemoveIfEmpty("gdi_vis_with_on_of_services", this.VisibleWithOneOfServices);

            this.StreamSave("gdi_containerdefault", this.IsContainerDefault);
            this.StreamSave("gdi_containerurl", this.ContainerUrl);
        }

        #region ISchemaNode Member
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        virtual public string RelativePath
        {
            get
            {
                return _relativePath;
            }
            set
            {
                _relativePath = value;
            }
        }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        virtual public CMSManager CmsManager
        {
            get { return _manager; }
            set { _manager = value; }
        }

        #endregion
    }
    #endregion
}

public class PresentationForCollection : Presentation
{
    public PresentationForCollection(CmsItemTransistantInjectionServicePack servicePack)
        : base(servicePack)
    {
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("Sichtbar Layer")]
    [Category("Anzeige")]
    [Editor(typeof(TypeEditor.SelectLayersForCollectionEditor), typeof(TypeEditor.ITypeEditor))]
    override public string LayerNames
    {
        get
        {
            return _layernames;
        }
        set
        {
            _layernames = value;
        }
    }
    #endregion
}

public class PresentationGroupGdi : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    //private PresentationCheckMode _mode = PresentationCheckMode.Button;
    private PresentationGroupStyle _style = PresentationGroupStyle.Dropdown;
    private bool _visible = true;

    public PresentationGroupGdi()
    {
        base.StoreUrl = false;
        base.ValidateUrl = true;
    }

    #region Properties
    public PresentationGroupStyle DisplayStyle
    {
        get { return _style; }
        set { _style = value; }
    }

    [Browsable(true)]
    [DisplayName("Sichtbar")]
    [Category("Allgemein")]
    [Description("Darstellungsvariantengruppe ist f�r den Anwender sichtar/schaltbar")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }

    [DisplayName("Metadaten Link")]
    [Category("Metadaten")]
    [Description("Wird im Viewer als [i] Button dargestellt und verwei�t auf angef�hten Link. Im Link k�nnen die Platzhalter f�r die Karte, wie bei benutzerdefnierten Werkzeugen verwendet weden: {map.bbox}, {map.centerx}, {map.centery}, {map.scale}")]

    public string MetadataLink
    {
        get; set;
    }

    [DisplayName("Metadaten Target")]
    [Category("Metadaten")]
    [Description("Gibt an, wie der Link ge�ffnet wird (tab => neuer Tab, dialog => in Dialogfenster im Viewer).")]
    public BrowserWindowTarget2 MetadataTarget { get; set; }

    [DisplayName("Metadaten Titel")]
    [Category("Metadaten")]
    [Description("Hier kann ein Titel f�r den Metadaten Button angeben werden.")]
    public string MetadataTitle { get; set; }

    [DisplayName("Metadaten Button Style")]
    [Category("Metadaten")]
    [Description("Gibt an, wie der Button dargestellt wird: [i] Button oder auff�lliger Link Button mit Titel.")]
    public MetadataButtonStyle MetadataLinkButtonStyle { get; set; }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        //IInitParameter ip = new TocGroupControl();
        //ip.InitParameter = this;

        //return ip;

        base.Create = create;

        IInitParameter ip = new NameUrlControl();
        //((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;
        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            //return Crypto.GetID() + @"\.general";
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
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
        get { return this.Name; }
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _style = (PresentationGroupStyle)stream.Load("checkmode", (int)PresentationGroupStyle.Button);
        _visible = (bool)stream.Load("visible", true);

        this.MetadataLink = (string)stream.Load("metadata", String.Empty);
        this.MetadataTarget = (BrowserWindowTarget2)(int)stream.Load("metadata_target", (int)BrowserWindowTarget2.tab);
        this.MetadataTitle = (string)stream.Load("metadata_title", String.Empty);
        this.MetadataLinkButtonStyle = (MetadataButtonStyle)stream.Load("metadata_button_style", (int)MetadataButtonStyle.i_button);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("checkmode", (int)_style);
        stream.Save("visible", _visible);

        stream.SaveOrRemoveIfEmpty("metadata", this.MetadataLink);
        if (this.MetadataTarget != BrowserWindowTarget2.tab)
        {
            stream.Save("metadata_target", (int)this.MetadataTarget);
        }
        stream.SaveOrRemoveIfEmpty("metadata_title", this.MetadataTitle);
        if (this.MetadataLinkButtonStyle != MetadataButtonStyle.i_button)
        {
            stream.Save("metadata_button_style", (int)this.MetadataLinkButtonStyle);
        }
    }
}

public class PresentationGroup : PresentationGroupGdi
{
    private string _containerUrl = String.Empty;

    [Category("Container")]
    [DisplayName("Container Name")]
    [Editor(typeof(TypeEditor.ContainerEditor), typeof(TypeEditor.ITypeEditor))]
    public string ContainerUrl
    {
        get { return _containerUrl; }
        set { _containerUrl = value; }
    }

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _containerUrl = (string)stream.Load("containerurl", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("containerurl", _containerUrl);
    }
}

public class PresentationLinkGdi : SchemaNodeLink, IEditable, IDisplayName
{
    private PresentationLinkCheckMode _mode = PresentationLinkCheckMode.CheckBox;
    private PresentationAffecting _affecting = PresentationAffecting.Service;
    private bool _visible = true, _isContainerDefault = false, _visWithService = true;


    public PresentationLinkGdi()
    {
    }

    #region Properties


    public PresentationLinkCheckMode DisplayStyle
    {
        get { return _mode; }
        set { _mode = value; }
    }

    [Browsable(true)]
    [Description("Gibt an, was von der Darstellungsvariante betroffen sein soll. Der jeweilige Dienst (service) oder die gesamte Karte (map). F�r Darstellungsvarianten mit Checkbox ist dieser Wert nicht relevant, da dort nur die angef�hrten Themen geschalten werden.")]
    public PresentationAffecting Affecting
    {
        get { return _affecting; }
        set { _affecting = value; }
    }

    [Browsable(true)]
    [DisplayName("Sichtbar")]
    [Category("Sichtbarkeit")]
    [Description("Darstellungsvariante ist f�r den Anwender sichtar/schaltbar")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }

    [Browsable(true)]
    [DisplayName("Sichtbar, falls Client")]
    [Category("Sichtbarkeit")]
    [Description("Hier kann eingeschr�nkt werden, ob eine Darstellungsvariante nur auf einem bestimmten Endger�t angezeigt wird.")]
    public ClientVisibility ClientVisibility { get; set; }

    [Category("Container")]
    [DisplayName("Default f�r Container")]
    public bool IsContainerDefault
    {
        get { return _isContainerDefault; }
        set { _isContainerDefault = value; }
    }

    [Category("Sichtbarkeit")]
    [DisplayName("Sichtbar, wenn dieser Dienst in Karte")]
    [Description("Die Anzeige einer Darstellungsvariante machte nicht immer Sinn. M�chte man zB beim Einschalten einer Darstellungsvariante (zB Naturbestand) Themen aus einem anderen Dienst (zB Kataster) ausschalten, hat es keinen Sinn, wenn die Container angezeigt wird, wenn nur der Kataster Dienste in einer Karte vorkommt. F�r diesen Fall kann man hier diese Option ausschalten. Die Eigentliche Gruppe wird dann nur angezeigt, wenn sich auch der Dienst (zB Naturbestand) in der Karte eingebunden ist.")]
    virtual public bool VisibleWithService
    {
        get { return _visWithService; }
        set { _visWithService = value; }
    }
    [Category("Sichtbarkeit")]
    [DisplayName("Sichtbar, wenn einer dieser Dienste in der Karte vorkommt")]
    [Description("Liste der Service-Urls mit Beistrich getrennt")]
    public string VisibleWithOneOfServices { get; set; }

    [DisplayName("Metadaten Link")]
    [Category("Metadaten")]
    [Description("Wird im Viewer als [i] Button dargestellt und verwei�t auf angef�hten Link. Im Link k�nnen die Platzhalter f�r die Karte, wie bei benutzerdefnierten Werkzeugen verwendet weden: {map.bbox}, {map.centerx}, {map.centery}, {map.scale}")]

    public string MetadataLink
    {
        get; set;
    }

    [DisplayName("Metadaten Target")]
    [Category("Metadaten")]
    [Description("Gibt an, wie der Link ge�ffnet wird (tab => neuer Tab, dialog => in Dialogfenster im Viewer).")]
    public BrowserWindowTarget2 MetadataTarget { get; set; }

    [DisplayName("Metadaten Titel")]
    [Category("Metadaten")]
    [Description("Hier kann ein Titel f�r den Metadaten Button angeben werden.")]
    public string MetadataTitle { get; set; }

    [DisplayName("Metadaten Button Style")]
    [Category("Metadaten")]
    [Description("Gibt an, wie der Button dargestellt wird: [i] Button oder auff�lliger Link Button mit Titel.")]
    public MetadataButtonStyle MetadataLinkButtonStyle { get; set; }

    [DisplayName("Gruppierung")]
    [Category("~User Interface")]
    [Description("Der Darstellungsvarianten Baum besteht aus Container (�bergeordnetes Element) und den eigentlichen Darstellungsvarianten, die sich wiederum in einer (aufklappbaren) Gruppe befinden k�nnen. Mehre Ebenen werden standardm��ig nicht angeboten, damit der Anwender nicht zu viele Ebenen klicken muss. Eine weiter Ebene wird darum hier in der Oberfl�che nicht angeboten. Allerdings gibt es immer wieder Ausnahmen, bei der eine weitere Ebene die Benutzerelemente im Viewer schlanker und einfache machen kann. F�r diese Ausnahmen ist es m�chglich, hier noch eine weiter Gruppierung anzugeben. Der hier angegebene Name entspricht dem Namen einer weiteren aufklappbaren Gruppe, die im Darstellungsvarianten Baum dargestellt wird. Mehre Darstellungsvarianten in der aktuellen Ebnen k�nnen hier den selben Gruppennamen aufweisen und werden unter dieser Gruppe angezeigt. Achtung: der hier angef�hrte Wert sollte in der Regel leer sein, au�er eine weiter Gruppierung bringt f�r die Bedienung Vorteile. Der hier eingetrage Wert wird sp�ter nur f�r Darstellungsvarianten ber�cksichtigit, die sich bereits in der aufklappbaren Gruppe befinden. Befindet sich die Darstellungsvariante in der obersten Ebene des Containers, bleibit dieser Wert unber�cksichtigt. Die Weg ist hier eine Gruppe zu erstellen und die Darstellungsvariante dort abzulegen! Es k�nnen mehrere Ebenen angegeben werden. Das Trennzeichen ist ein Schr�gstrich (/). Solle ein '/' als Text vorkommen ist dieser mittels '\\/ zu kodieren.'")]
    public string UIGroupName { get; set; }

    //[Browsable(true)]
    //[Category("~Experimentell")]
    //[DisplayName("Als dynamische Abfrage Marker darstellbar")]
    //[Description("Gibt es f�r das �ber der Darstellungsvariante schaltbare Thema eine Abfrage, kann diese vom Anwender als ausschnittsabh�nger dynamischer Inhalt eingef�gt werden. In der Karte werden die Features als Marker anzeigt und k�nnen angeklickt werden. ")]
    //public bool ShowDynamicMarkers
    //{
    //    get; set;
    //}

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _mode = (PresentationLinkCheckMode)stream.Load("checkmode", (int)PresentationLinkCheckMode.Button);
        _affecting = (PresentationAffecting)stream.Load("affecting", (int)PresentationAffecting.Service);

        _visible = (bool)stream.Load("visible", true);
        this.ClientVisibility = (ClientVisibility)stream.Load("client_visibility", (int)ClientVisibility.Any);
        _isContainerDefault = (bool)stream.Load("containerdefault", false);
        _visWithService = (bool)stream.Load("vis_with_service", true);
        this.VisibleWithOneOfServices = (string)stream.Load("vis_with_on_of_services", String.Empty);

        this.MetadataLink = (string)stream.Load("metadata", String.Empty);
        this.MetadataTarget = (BrowserWindowTarget2)stream.Load("metadata_target", (int)BrowserWindowTarget2.tab);
        this.MetadataTitle = (string)stream.Load("metadata_title", String.Empty);
        this.MetadataLinkButtonStyle = (MetadataButtonStyle)stream.Load("metadata_button_style", (int)MetadataButtonStyle.i_button);

        //this.ShowDynamicMarkers = (bool)stream.Load("showdynamicmarkers", false);

        this.UIGroupName = (string)stream.Load("ui_group", String.Empty);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("checkmode", (int)_mode);
        stream.Save("affecting", (int)_affecting);

        stream.Save("visible", _visible);
        stream.Save("client_visibility", (int)this.ClientVisibility);
        stream.Save("containerdefault", _isContainerDefault);
        stream.Save("vis_with_service", _visWithService);
        stream.SaveOrRemoveIfEmpty("vis_with_on_of_services", this.VisibleWithOneOfServices);

        stream.SaveOrRemoveIfEmpty("metadata", this.MetadataLink);
        if (this.MetadataTarget != BrowserWindowTarget2.tab)
        {
            stream.Save("metadata_target", (int)this.MetadataTarget);
        }
        stream.SaveOrRemoveIfEmpty("metadata_title", this.MetadataTitle);
        if (this.MetadataLinkButtonStyle != MetadataButtonStyle.i_button)
        {
            stream.Save("metadata_button_style", (int)this.MetadataLinkButtonStyle);
        }

        //if(this.ShowDynamicMarkers)
        //{
        //    stream.Save("showdynamicmarkers", this.ShowDynamicMarkers);
        //}

        stream.SaveOrRemoveIfEmpty("ui_group", this.UIGroupName);
    }

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get
        {
            return String.IsNullOrWhiteSpace(this.UIGroupName)
                ? String.Empty
                : $"{this.UIGroupName}/{{0}}".Replace(@"\/", "&sol;").Replace("/", " � ").Replace("&sol;", "/");
        }  // slash can be encoded as \/
    }

    #endregion
}

public class PresentationLink : PresentationLinkGdi
{
    private string _containerUrl = String.Empty;

    [Category("Container")]
    [DisplayName("Container Name")]
    [Editor(typeof(TypeEditor.ContainerEditor), typeof(TypeEditor.ITypeEditor))]
    public string ContainerUrl
    {
        get { return _containerUrl; }
        set { _containerUrl = value; }
    }

    [Browsable(false)]
    public override bool VisibleWithService
    {
        get
        {
            return base.VisibleWithService;
        }
        set
        {
            base.VisibleWithService = value;
        }
    }

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _containerUrl = (string)stream.Load("containerurl", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("containerurl", _containerUrl);
    }
}

public class PresentationThemeLinkGdi : SchemaNodeLink, IEditable
{
    #region Properties

    [Browsable(true)]
    [DisplayName("Sichtbar")]
    [Category("Allgemein")]
    [Description("Thema ist beim start eingeschalten")]
    public bool Checked { get; set; }

    [Browsable(true)]
    [DisplayName("Name")]
    [Category("Allgemein")]
    [Description("Name des Themas bei den Darstellungsvarianten")]
    public string Name { get; set; }

    [DisplayName("Metadaten Link")]
    [Category("Metadaten")]
    [Description("Wird im Viewer als [i] Button dargestellt und verwei�t auf angef�hten Link. Im Link k�nnen die Platzhalter f�r die Karte, wie bei benutzerdefnierten Werkzeugen verwendet weden: {map.bbox}, {map.centerx}, {map.centery}, {map.scale}")]
    public string MetadataLink
    {
        get; set;
    }

    [DisplayName("Metadaten Target")]
    [Category("Metadaten")]
    [Description("Gibt an, wie der Link ge�ffnet wird (tab => neuer Tab, dialog => in Dialogfenster im Viewer).")]
    public BrowserWindowTarget2 MetadataTarget { get; set; }

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.Checked = (bool)stream.Load("checked", true);
        this.Name = (string)stream.Load("name", String.Empty);
        this.MetadataLink = (string)stream.Load("metadata", String.Empty);
        this.MetadataTarget = (BrowserWindowTarget2)(int)stream.Load("metadata_target", (int)BrowserWindowTarget2.tab);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("checked", this.Checked);
        stream.Save("name", this.Name);
        stream.SaveOrRemoveIfEmpty("metadata", this.MetadataLink);
        if (this.MetadataTarget != BrowserWindowTarget2.tab)
        {
            stream.Save("metadata_target", (int)this.MetadataTarget);
        }
    }

    #endregion
}

public class PresentationThemeAssistent : SchemaNode, IAutoCreatable, IUI
{
    ThemeAssistentControl _ctrl = null;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public PresentationThemeAssistent(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        return "Assistent";
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {

    }

    public void Save(IStreamDocument stream)
    {

    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        _ctrl = new ThemeAssistentControl(_servicePack, this);

        return _ctrl;
    }

    #endregion

    #region IAutoCreatable Member

    public bool AutoCreate()
    {
        if (_ctrl == null)
        {
            return false;
        }

        string path = this.CmsManager.ConnectionString + @"/" + Helper.TrimPathRight(this.RelativePath, 1).Replace(@"\", @"/");
        foreach (string theme in _ctrl.SelectedThemes)
        {
            string name = theme.Replace(@"\", "/");
            if (name.Contains("/"))
            {
                name = name.Substring(name.LastIndexOf("/") + 1);
            }

            Presentation presentation = new Presentation(_servicePack);
            presentation.Url = Helper.ToValidUrl("dv_" + theme);
            presentation.Name = name;
            presentation.LayerNames = theme;

            string fullName = path + @"/" + presentation.CreateAs(false) + ".xml";
            //if (new FileInfo(fullName).Exists == false)
            {
                IStreamDocument xmlStream = DocumentFactory.New(path);
                presentation.Save(xmlStream);
                xmlStream.SaveDocument(fullName);
                presentation.CreatedAsync(fullName).Wait();
            }
        }
        return true;
    }

    #endregion
}
