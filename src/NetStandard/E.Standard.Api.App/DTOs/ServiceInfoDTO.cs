using E.Standard.Api.App.Models.Abstractions;
using E.Standard.CMS.Core;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public class ServiceInfoDTO : VersionDTO, IHtml2
{
    public string name { get; set; }
    public string id { get; set; }
    public string type { get; set; }

    public int[] supportedCrs { get; set; }

    public double opacity { get; set; }
    public float opacity_factor { get; set; } = 1f;

    public bool isbasemap { get; set; }
    public bool show_in_toc { get; set; }

    [JsonProperty("has_visible_locked_layers")]
    [System.Text.Json.Serialization.JsonPropertyName("has_visible_locked_layers")]
    public bool HasVisibleLockedLayers { get; set; }

    [JsonProperty("basemap_type", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("basemap_type")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string basemap_type { get; set; }

    public LayerInfo[] layers { get; set; }

    public PresentationDTO[] presentations { get; set; }
    public QueryDTO[] queries { get; set; }
    public EditThemeDTO[] editthemes { get; set; }
    public VisFilterDTO[] filters { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public VisFilterDTO[] LockedFilters { get; set; }

    //[JsonIgnore]
    //[System.Text.Json.Serialization.JsonIgnore]
    //public VisFilter[] InvisibleFilters { get; set; }

    [JsonProperty(PropertyName = "labeling")]
    [System.Text.Json.Serialization.JsonPropertyName("labeling")]
    public LabelingDTO[] Labeling { get; set; }

    [JsonProperty(PropertyName = "snapping", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("snapping")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<SnapSchemaDTO> Snapping { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ChainageThemeDTO[] chainagethemes { get; set; }

    public double[] extent { get; set; }

    [JsonProperty("metadata_link", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("metadata_link")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string MetadataLink { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string copyright { get; set; }

    [JsonProperty(PropertyName = "copyrighttext", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("copyrighttext")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string CopyrightText { get; set; }

    [JsonProperty(PropertyName = "servicedescription", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("servicedescription")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ServiceDescription { get; set; }

    [JsonProperty(PropertyName = "container_displayname", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("container_displayname")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ContainerDisplayname { get; set; }

    public ServiceInfoProperties properties { get; set; }

    [JsonProperty(PropertyName = "has_legend")]
    [System.Text.Json.Serialization.JsonPropertyName("has_legend")]
    public bool HasLegend { get; set; }

    [JsonProperty(PropertyName = "initial_exception", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("initial_exception")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string InitialException { get; set; }

    [JsonProperty(PropertyName = "image_service_type", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("image_service_type")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ImageServiceType { get; set; }

    [JsonProperty(PropertyName = "custom_request_parameters", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("custom_request_parameters")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object CustomRequestParameters { get; set; }

    [JsonProperty(PropertyName = "services", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("services")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ServiceInfoDTO[] ChildServiceInfos { get; set; }  // for service collections

    #region StaticOverlay Properties

    [JsonProperty(PropertyName = "overlay_url", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("overlay_url")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string OverlayUrl { get; set; }

    [JsonProperty(PropertyName = "affinePoints", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("affinePoints")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<double[]> AffinePoints { get; set; }

    [JsonProperty(PropertyName = "widthHeightRatio", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("widthHeightRatio")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? WidthHeightRatio { get; set; }

    #endregion

    #region Members

    public WebMapping.Core.Api.Bridge.IServiceBridge ToServiceBridge()
    {
        return new ServiceInfoBridge(this);
    }

    public void ApplyLockedFilters(WebMapping.Core.Abstraction.IMapService service,
                                   RequestParameters requestParameters,
                                   CmsDocument.UserIdentification ui)
    {
        if (this.LockedFilters == null || this.LockedFilters.Length == 0)
        {
            return;
        }

        foreach (var filter in this.LockedFilters)
        {
            foreach (string layerName in filter.LayerNamesString.Split(','))
            {
                var layer = (from l in service.Layers where l.Name == layerName select l).FirstOrDefault();
                if (layer == null)
                {
                    continue;
                }

                string filterClause = CmsHlp.ReplaceFilterKeys(requestParameters, ui, filter.Filter);

                //if (!String.IsNullOrWhiteSpace(layer.Filter))
                //    layer.Filter += " AND " + filterClause;
                //else
                //    layer.Filter = filterClause;
                layer.Filter = layer.Filter.AppendWhereClause(filterClause);
            }
        }
    }

    #endregion

    #region Sub Classes

    public sealed class LayerInfo : IHtml, WebMapping.Core.Api.Bridge.ILayerBridge
    {
        public string name { get; set; }
        public string id { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string idfieldname { get; set; }

        public bool selectable => !String.IsNullOrEmpty(idfieldname);

        public string alias { get; set; }
        public string metadata { get; set; }
        public bool visible { get; set; }
        public bool locked { get; set; }
        public bool legend { get; set; }

        public double minscale { get; set; }
        public double maxscale { get; set; }

        public string type { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string description { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string @abstract { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string metadataurl { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string metadataformat { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string tocname { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? tochidden { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string undropdownable_groupname { get; set; }

        private string _ogcId = null;
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string OgcId
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(OgcGroupId))
                {
                    return OgcGroupId;
                }

                return String.IsNullOrWhiteSpace(_ogcId) ? this.id : _ogcId;
            }
            set
            {
                _ogcId = value;
            }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string OgcTitle
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(OgcGroupTitle))
                {
                    return OgcGroupTitle;
                }

                if (!String.IsNullOrWhiteSpace(OgcGroupId))
                {
                    return OgcGroupId;
                }

                return this.Name;
            }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string OgcGroupId { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string OgcGroupTitle { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsTocLayer { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public BrowserWindowTarget2 MetadataTarget { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string MetadataTitle { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public MetadataButtonStyle MetadataButtonStyle { get; set; }

        #region IHtml Member

        public string ToHtmlString()
        {
            return HtmlHelper.ToTable(
                new string[] { "Name", "Id", "IdFieldName", "Aliasname", "GeometryType", "Metadata", "Visible", "Locked", "Legend", "MinScale", "MaxScale" },
                new object[] { this.name, this.id, this.idfieldname, this.alias, this.type, this.metadata, this.visible, this.locked, this.legend, this.minscale, this.maxscale }
                );
        }

        #endregion

        #region ILayer

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Id
        {
            get { return this.id; }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Name
        {
            get { return this.name; }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public WebMapping.Core.Api.Bridge.LayerGeometryType GeometryType
        {
            get
            {
                WebMapping.Core.Api.Bridge.LayerGeometryType geomType = WebMapping.Core.Api.Bridge.LayerGeometryType.unknown;
                if (Enum.TryParse<WebMapping.Core.Api.Bridge.LayerGeometryType>(this.type, out geomType))
                {
                    return geomType;
                }

                return WebMapping.Core.Api.Bridge.LayerGeometryType.unknown;
            }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double MinScale
        {
            get { return this.minscale; }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double MaxScale
        {
            get { return this.maxscale; }
        }

        #endregion

        #region ILayerBridge

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string IdFieldname { get { return idfieldname; } }

        #endregion
    }

    private sealed class ServiceInfoBridge : WebMapping.Core.Api.Bridge.IServiceBridge
    {
        private readonly ServiceInfoDTO _serviceInfo;
        public ServiceInfoBridge(ServiceInfoDTO serviceInfo)
        {
            if (serviceInfo == null)
            {
                throw new ArgumentNullException("serviceInfo");
            }

            _serviceInfo = serviceInfo;

            List<WebMapping.Core.Api.Bridge.ILayerBridge> layers = new List<WebMapping.Core.Api.Bridge.ILayerBridge>();
            if (serviceInfo.layers != null)
            {
                layers.AddRange(serviceInfo.layers);
            }

            this.Layers = layers.ToArray();
        }

        #region IService Member

        public string Name
        {
            get { return _serviceInfo.name; }
        }

        public string Id
        {
            get { return _serviceInfo.id; }
        }

        public WebMapping.Core.Api.Bridge.ILayerBridge[] Layers
        {
            get;
            private set;
        }

        public WebMapping.Core.Api.Bridge.ILayerBridge FindLayer(string id)
        {
            return (from l in this.Layers where l.Id == id select l).FirstOrDefault();
        }

        #endregion
    }

    #region ServiceInfoProperties

    [System.Text.Json.Serialization.JsonPolymorphic()]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(ServiceInfoProperties))]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(TileProperties))]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(VectorTileProperties))]
    public class ServiceInfoProperties : IHtml
    {
        [JsonProperty("basemap_type", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("basemap_type")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string BasemapType { get; set; }

        [JsonProperty("preview_url", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("preview_url")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string PreviewUrl { get; set; }

        [JsonProperty("capabilities", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("capabilities")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string[] Capabilities { get; set; }

        #region IHtml Member

        virtual public string ToHtmlString()
        {
            return HtmlHelper.ToTable(
                new string[] { "BasemapType" },
                new object[] { this.BasemapType }
            );
        }

        #endregion
    }

    public class TileProperties : ServiceInfoProperties, IHtml
    {
        public double[] extent { get; set; }
        public double[] resolutions { get; set; }
        public double[] origin { get; set; }
        public string tileurl { get; set; }
        public string[] domains { get; set; }
        public int tilesize { get; set; }
        public bool hide_beyond_maxlevel { get; set; }

        #region IHtml Member

        public override string ToHtmlString()
        {
            return HtmlHelper.ToTable(
                new string[] { "Origin", "Resolutions", "TileUrl", "TileSize", "BasemapType", "HideBeyondMaxLevel" },
                new object[] { this.origin, this.resolutions, this.tileurl, this.tilesize, this.BasemapType, this.hide_beyond_maxlevel }
            );
        }

        #endregion
    }

    public class VectorTileProperties : ServiceInfoProperties
    {
        public string vtc_styles_url { get; set; }
        public string fallback { get; set; }
    }

    #endregion

    public sealed class LngLat
    {
        static public double[] Create(double[] vec)
        {
            if (vec == null || vec.Length < 2)
            {
                return null;
            }

            return new double[] { vec[0], vec[1] };
        }
    }

    #endregion

    #region IHtml Member

    virtual public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToHeader(this.name, HtmlHelper.HeaderType.h1));

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Name", "Type", "SupportedCrs", "IsBasemap" },
            new object[] { this.id, this.name, this.type, this.supportedCrs, this.isbasemap }
            ));

        if (this.properties != null)
        {
            sb.Append(HtmlHelper.ToHeader("Properties", HtmlHelper.HeaderType.h2));
            sb.Append(this.properties.ToHtmlString());
        }

        sb.Append(HtmlHelper.ToList(this.layers, "Layers"));

        return sb.ToString();
    }

    #endregion

    #region IHtml2 Member

    public string[] PropertyLinks
    {
        get
        {
            return new string[]{
                "Presentations",
                "Queries"
            };
        }
    }

    #endregion
}