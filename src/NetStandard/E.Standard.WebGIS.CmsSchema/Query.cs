using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.Extensions;
using E.Standard.WebGIS.CmsSchema.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public enum ImportQueryTable
{
    None,
    Dynamic,
    Fields
}

public class Query : CopyableNode, IUI, ICreatable, IEditable, IDisplayName
{
    //private FeatureTableType _featureTableType = FeatureTableType.Default;
    //private bool _geoJuhu = true;
    private double _minZoomScale = 0;
    private bool _allowEmptySearch = true;
    private string _networktracer = String.Empty;
    private GdiProperties _gdi_properties;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public Query(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        base.Create = true;

        _gdi_properties = new GdiProperties(this);
    }

    #region Properties

    [DisplayName("#min_zoom_to_scale")]
    [Category("#category_min_zoom_to_scale")]
    public double MinZoomToScale
    {
        get { return _minZoomScale; }
        set { _minZoomScale = value; }
    }
    [DisplayName("#allow_empty_search")]
    [Category("#category_allow_empty_search")]
    public bool AllowEmptySearch
    {
        get { return _allowEmptySearch; }
        set { _allowEmptySearch = value; }
    }

    [Browsable(true)]
    [DisplayName("#preview_text_template")]
    [Category("#category_preview_text_template")]
    public string PreviewTextTemplate
    {
        get; set;
    }

    [Category("#category_draggable")]
    [DisplayName("#draggable")]
    public bool Draggable { get; set; }

    [Category("#category_show_attachments")]
    [DisplayName("#show_attachments")]
    [AuthorizableProperty("show_attachments", false)]
    public bool ShowAttachments { get; set; }

    [Category("#category_distict")]
    [DisplayName("#distict")]
    public bool Distict { get; set; }

    [Category("#category_union")]
    [DisplayName("#union")]
    public bool Union { get; set; }

    [Category("#category_apply_zoom_limits")]
    [DisplayName("#apply_zoom_limits")]
    public bool ApplyZoomLimits { get; set; }

    [Category("#category_max_features")]
    [DisplayName("#max_features")]
    public int MaxFeatures { get; set; }

    [Category("~~#category_network_tracer")]
    [DisplayName("#network_tracer")]
    [Editor(typeof(TypeEditor.NetworkTracersTypeEditor), typeof(TypeEditor.ITypeEditor))]
    public string NetworkTracer
    {
        get { return _networktracer; }
        set
        {
            _networktracer = value;
        }
    }

    [Category("~~#category_gdi_props")]
    [DisplayName("#gdi_props")]
    //[TypeConverter(typeof(ExpandableObjectConverter))]
    public GdiProperties GdiProps
    {
        get { return _gdi_properties; }
        set
        {
            _gdi_properties = value;
        }
    }

    [Browsable(false)]
    public string QueryThemeId { get; set; }

    #region MapTips

    double _minScale = 0, _maxScale = 0;
    string _mapInfoSymbol = String.Empty;
    bool _mapInfoVisible = true, _isMapInfo = false, _setVisWithTheme = false;

    [Browsable(true)]
    [DisplayName("#min_scale")]
    [Category("~~#category_min_scale")]
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
    [DisplayName("#map_info_symbol")]
    [Category("#category_map_info_symbol")]
    [Editor(typeof(TypeEditor.GeoRssMarkerEditor),
        typeof(TypeEditor.ITypeEditor))]
    public string MapInfoSymbol
    {
        get { return _mapInfoSymbol; }
        set { _mapInfoSymbol = value; }
    }
    [Browsable(true)]
    [DisplayName("#map_info_visible")]
    [Category("#category_map_info_visible")]
    public bool MapInfoVisible
    {
        get { return _mapInfoVisible; }
        set { _mapInfoVisible = value; }
    }
    [Browsable(true)]
    [DisplayName("#is_map_info")]
    [Category("#category_is_map_info")]
    public bool IsMapInfo
    {
        get { return _isMapInfo; }
        set { _isMapInfo = value; }
    }
    [Browsable(true)]
    [DisplayName("#set_visible_with_theme")]
    [Category("#category_set_visible_with_theme")]
    public bool SetVisibleWithTheme
    {
        get { return _setVisWithTheme; }
        set { _setVisWithTheme = value; }
    }

    #endregion

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewQueryControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    [Browsable(false)]
    public ImportQueryTable AutoImportAllFields { get; set; }

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"/.general";
        }
        else
        {
            return ".general";
        }
    }

    async public Task<bool> CreatedAsync(string fullName)
    {
        var serviceLayer = this.CmsManager.SchemaNodeInstances(_servicePack, this.RelativePath.TrimAndAppendSchemaNodePath(3, "Themes"), true)?
            .Where(o => o is ServiceLayer && ((ServiceLayer)o).Id == this.QueryThemeId)
            .FirstOrDefault() as ServiceLayer;

        if (serviceLayer != null)  // Link to QueryTheme
        {
            var link = new Link(serviceLayer.RelativePath);

            string newLinkName = Helper.NewLinkName();
            IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
            link.Save(xmlStream);
            xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "QueryTheme") + "/" + newLinkName);
        }

        if (AutoImportAllFields == ImportQueryTable.Dynamic)
        {
            var tableColumn = new TableColumn();
            tableColumn.Name = "All";
            tableColumn.ColumnType = ColumnType.Field;
            ((TableColumn.FieldDataField)tableColumn.Data).FieldName = "*";

            IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
            tableColumn.Save(xmlStream);
            xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "TableColumns") + "/" + tableColumn.CreateAs(true) + ".xml");
        }
        else if (AutoImportAllFields == ImportQueryTable.Fields)
        {
            var objects = this.CmsManager.SchemaNodeInstances(_servicePack, this.RelativePath.TrimAndAppendSchemaNodePath(1, "QueryTheme"), true);

            List<string> fieldOrder = new List<string>();
            foreach (var field in await objects.FieldsNames(this.CmsManager, _servicePack, excludeShape: true))
            {
                var tableColumn = new TableColumn();
                tableColumn.Name = field.Aliasname;
                tableColumn.ColumnType = ColumnType.Field;
                ((TableColumn.FieldDataField)tableColumn.Data).FieldName = field.Name;

                var createAs = $"{tableColumn.CreateAs(true)}.xml";
                fieldOrder.Add(createAs);

                IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
                tableColumn.Save(xmlStream);
                xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "TableColumns") + "/" + createAs);
            }

            var itemOrder = new ItemOrder(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "TableColumns"));
            itemOrder.Items = fieldOrder.ToArray();
            itemOrder.Save();
        }

        return true;
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

        //_featureTableType = (FeatureTableType)stream.Load("ftabtype", (int)FeatureTableType.Default);
        //_geoJuhu = (bool)stream.Load("geojuhu", _geoJuhu);

        _minZoomScale = (double)stream.Load("minzoomscale", 0.0);
        _allowEmptySearch = (bool)stream.Load("allowemptysearch", true);

        _gdi_properties.Load(stream);

        // MapTips 
        _minScale = (double)stream.Load("minscale", 0.0);
        _maxScale = (double)stream.Load("maxscale", 0.0);

        _isMapInfo = (bool)stream.Load("ismapinfo", false);
        _mapInfoSymbol = (string)stream.Load("mapinfosymbol", String.Empty);
        _mapInfoVisible = (bool)stream.Load("mapinfovisible", true);
        _setVisWithTheme = (bool)stream.Load("viswiththeme", false);
        /////////////////////////////////////////////////////////////

        _networktracer = (string)stream.Load("networktracer", String.Empty);

        this.ShowAttachments = (bool)stream.Load("show_attachments", false);

        this.Draggable = (bool)stream.Load("draggable", false);
        this.Distict = (bool)stream.Load("distinct", false);
        this.Union = (bool)stream.Load("union", false);
        this.ApplyZoomLimits = (bool)stream.Load("applyzoomlimits", false);
        this.MaxFeatures = (int)stream.Load("maxfeatures", 0);

        this.PreviewTextTemplate = (string)stream.Load("preview_text_template", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        //stream.Save("ftabtype", (int)FeatureTableType.Default);
        //stream.Save("geojuhu", _geoJuhu);

        stream.Save("minzoomscale", _minZoomScale);
        stream.Save("allowemptysearch", _allowEmptySearch);

        _gdi_properties.Save(stream);

        // MapTips
        stream.Save("minscale", _minScale);
        stream.Save("maxscale", _maxScale);

        stream.Save("ismapinfo", _isMapInfo);
        stream.Save("mapinfosymbol", _mapInfoSymbol);
        stream.Save("mapinfovisible", _mapInfoVisible);
        stream.Save("viswiththeme", _setVisWithTheme);
        ///////////////////////////////////////////////

        if (!String.IsNullOrEmpty(_networktracer))
        {
            stream.Save("networktracer", _networktracer);
        }

        if (this.ShowAttachments == true)
        {
            stream.Save("show_attachments", this.ShowAttachments);
        }

        if (this.Draggable == true)
        {
            stream.Save("draggable", this.Draggable);
        }

        if (this.Distict == true)
        {
            stream.Save("distinct", this.Distict);
        }

        if (this.Union == true)
        {
            stream.Save("union", this.Union);
        }

        if (this.ApplyZoomLimits == true)
        {
            stream.Save("applyzoomlimits", this.ApplyZoomLimits);
        }

        if (this.MaxFeatures > 0)
        {
            stream.Save("maxfeatures", this.MaxFeatures);
        }

        if (!String.IsNullOrWhiteSpace(this.PreviewTextTemplate))
        {
            stream.Save("preview_text_template", this.PreviewTextTemplate);
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Abfrage"; }
    }

    #region Helper Classes
    public class GdiProperties
    {
        private FeatureTableType _featureTableType = FeatureTableType.Default;
        private bool _geoJuhu = false;
        private string _geoJuhuSchema = String.Empty;
        private string _filterUrl = String.Empty;
        private readonly Query _parent;

        public GdiProperties(Query parent)
        {
            _parent = parent;
        }

        #region Properties
        [DisplayName("#feature_table_type")]
        public FeatureTableType FeatureTableType
        {
            get { return _featureTableType; }
            set { _featureTableType = value; }
        }

        [DisplayName("#geo_juhu")]
        [Category("#category_geo_juhu")]
        public bool GeoJuhu
        {
            get { return _geoJuhu; }
            set { _geoJuhu = value; }
        }

        [DisplayName("#geo_juhu_schema")]
        [Category("#category_geo_juhu_schema")]
        public string GeoJuhuSchema
        {
            get { return _geoJuhuSchema; }
            set { _geoJuhuSchema = value; }
        }

        [DisplayName("#filter_url")]
        [Category("#category_filter_url")]
        [Editor(typeof(TypeEditor.SelectServiceVisFilter), typeof(TypeEditor.ITypeEditor))]
        public string FilterUrl
        {
            get { return _filterUrl; }
            set { _filterUrl = value; }
        }

        [DisplayName("#query_group_name")]
        public string QueryGroupName { get; set; }

        [Browsable(false)]
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public Query Parent
        {
            get { return _parent; }
        }
        #endregion

        public void Load(IStreamDocument stream)
        {
            _featureTableType = (FeatureTableType)stream.Load("gdi_ftabtype", (int)FeatureTableType.Default);
            _geoJuhu = (bool)stream.Load("gdi_geojuhu", false);
            _geoJuhuSchema = (string)stream.Load("gdi_geojuhuschema", String.Empty);
            _filterUrl = (string)stream.Load("gdi_filter", String.Empty);
            this.QueryGroupName = (string)stream.Load("gdi_querygroupname", String.Empty);
        }

        public void Save(IStreamDocument stream)
        {
            stream.Remove("gdi_ftabtype");
            stream.Remove("gdi_geojuhu");
            stream.Remove("gdi_filter");

            if (_featureTableType != FeatureTableType.Default)
            {
                stream.Save("gdi_ftabtype", (int)_featureTableType);
            }

            if (_geoJuhu != false)
            {
                stream.Save("gdi_geojuhu", _geoJuhu);
            }

            if (!String.IsNullOrEmpty(_geoJuhuSchema))
            {
                stream.Save("gdi_geojuhuschema", _geoJuhuSchema);
            }

            if (!String.IsNullOrEmpty(_filterUrl))
            {
                stream.Save("gdi_filter", _filterUrl);
            }

            if (!String.IsNullOrWhiteSpace(this.QueryGroupName))
            {
                stream.Save("gdi_querygroupname", this.QueryGroupName);
            }
        }

        public override string ToString()
        {
            return "GdiProperties";
        }
    }
    #endregion
}

public class QueryTheme : Link
{
    string _name = String.Empty;

    public override void LoadParent(IStreamDocument stream)
    {
        base.LoadParent(stream);

        if (stream == null)
        {
            return;
        }

        if (String.IsNullOrEmpty(_name))
        {
            _name = (string)stream.Load("name", String.Empty);
        }
    }

    public override bool HasAdditionalLinks(string rootDirectory, string parentPath)
    {
        if (parentPath.ToLower().StartsWith("services/miscellaneous/servicecollection/"))
        {
            List<string> equalNameThemes = new List<string>();

            string[] p = parentPath.Split('/');
            string servicesPath = rootDirectory + @"\" + p[0] + @"\" + p[1] + @"\" + p[2] + @"\" + p[3] + @"\services";
            ItemOrder servicesOrder = new ItemOrder(servicesPath, false);
            foreach (string service in servicesOrder.Items)
            {
                IStreamDocument xmlStream = DocumentFactory.Open(servicesPath + @"\" + service);
                Link link = new Link();
                link.Load(xmlStream);
                if (String.IsNullOrEmpty(link.LinkUri))
                {
                    continue;
                }

                string themesPath = rootDirectory + @"\" + link.LinkUri + @"\themes";
                ItemOrder themesOrder = new ItemOrder(themesPath);
                foreach (string theme_ in themesOrder.Items)
                {
                    string theme = theme_;
                    if (theme.ToLower().EndsWith(".xml"))
                    {
                        theme = theme.Substring(0, theme.Length - 4);
                    }
                    else if (theme.ToLower().EndsWith(".link"))
                    {
                        theme = theme.Substring(0, theme.Length - 5);
                    }

                    xmlStream = DocumentFactory.Open(themesPath + @"\" + theme_);
                    string name = (string)xmlStream.Load("name", String.Empty);

                    if (name.ToLower() == _name.ToLower())
                    {
                        equalNameThemes.Add(link.LinkUri + "/themes/" + theme);
                    }
                }
            }

            _additionalLinks = new List<Link>();
            if (equalNameThemes.Count > 1)
            {
                throw new Exception("In den eingebunden Diensten existieren mehrere Themen mit gleichem Namen");
                //FormEqualItems dlg = new FormEqualItems(_name, equalNameThemes.ToArray());
                //dlg.HeaderText = "In den eingebunden Diensten existieren mehrere Themen mit gleichem Namen. Sollen diese Themen auch eingef�gt werden?";
                //dlg.ShowSetVisible = dlg.ShowGroup = false;
                //if (dlg.ShowDialog() == DialogResult.OK)
                //{
                //    foreach (string item in dlg.CheckedItems)
                //    {
                //        QueryTheme additionalTheme = new QueryTheme();
                //        additionalTheme.LinkUri = item;
                //        _additionalLinks.Add(additionalTheme);
                //    }
                //    return true;
                //}
            }
            return false;
        }
        return false;
    }

    List<Link> _additionalLinks = null;
    public override List<Link> AdditionalLinks(string rootDirectory, string parentPath)
    {
        return _additionalLinks;
    }
}
