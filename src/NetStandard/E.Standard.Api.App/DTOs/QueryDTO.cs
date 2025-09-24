using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Collections;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class QueryDTO : VersionDTO, IHtml, IAuthClone<QueryDTO>, IQueryBridge
{
    public string id { get; set; }
    public string name { get; set; }
    public ItemDTO[] items { get; set; }

    public ResultFieldDTO[] result_fields =>
        this.Fields?.Select(f => new ResultFieldDTO
        {
            ColumnName = f.ColumnName,
            Visible = f.Visible == false ? false : null,
            ColumnType = f switch
            {
                TableFieldHotlinkDTO => "hotlink",
                TableFieldImageDTO => "image",
                _ => "value"
            }
        }).ToArray();

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AllowEmptyQueries { get; set; }

    [JsonProperty("layerid")]
    [System.Text.Json.Serialization.JsonPropertyName("layerid")]
    public string LayerId { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public TableFieldDTO[] Fields { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] AutoSortFields { get; set; }

    public AssociatedLayer[] associatedlayers { get; set; }

    public bool geojuhu { get; set; }
    public string geojuhuschema { get; set; }

    public int zoomscale { get; set; }

    public bool draggable { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Distinct { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Union { get; set; }

    [JsonProperty(PropertyName = "apply_zoom_limits", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("apply_zoom_limits")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? apply_zoom_limits => ApplyZoomLimits == true ? true : null;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ApplyZoomLimits { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int MaxFeatures { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Filter { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string PreviewTextTemplate { get; set; }

    public ItemDTO GetItem(string id)
    {
        if (items == null)
        {
            return null;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].id == id)
            {
                return items[i];
            }
        }
        return null;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public AuthObject<ItemDTO>[] AuthItems { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public AuthObject<TableFieldDTO>[] AuthFields { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public AuthObject<TableExportFormat>[] AuthTableExportFormats { get; set; }

    [JsonProperty(PropertyName = "table_export_formats", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("table_export_formats")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public TableExportFormat[] TableExportFormats { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<AuthObject<FeatureTransfer>> AuthFeatureTransfers { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<FeatureTransfer> FeatureTransfers { get; set; }

    [JsonProperty("has_feature_transfers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("has_feature_transfers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasFeatureTransfers => this.FeatureTransfers != null && this.FeatureTransfers.Where(f => f.Targets != null && f.Targets.Count() > 0).FirstOrDefault() != null ?
                                                true : null;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]  // for GeoJuhu
    public WebGIS.CMS.QueryDefinitions.QueryDefinition QueryDef { get; set; }

    public void Init(IMapService service)
    {
        Map map = new Map("init");

        // Always Clone -> WMS Identify need map Extent and Size!!! 
        this.Service = service.Clone(map);
    }

    async public Task InitFieldRendering(IHttpService httpService)
    {
        if (this.Fields != null)
        {
            foreach (var field in this.Fields)
            {
                await field.InitRendering(httpService);
            }
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IMapService Service
    {
        get;
        set;
    }

    public LayerType GetLayerType()
    {
        var layer = this.Service?.Layers?.FindByLayerId(this.LayerId);
        if (layer == null)
        {
            return LayerType.unknown;
        }

        return layer.Type;
    }

    public string GetServiceId() => this.Service?.Url;
    public Guid? GetServiceGuid() => String.IsNullOrEmpty(this.Service?.ID) ? null : new Guid(this.Service.ID);

    public string GetLayerId() => this.LayerId;

    #region SubClasses

    public class ItemDTO : IHtml
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool visible { get; set; }
        public bool required { get; set; }
        public string examples { get; set; }
        public bool autocomplete { get; set; }
        public int autocomplete_minlength { get; set; }
        public IEnumerable<string> autocomplete_depends_on { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string[] Fields { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool UseUpper { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public WebGIS.CMS.QueryMethod QueryMethod { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Regex { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string FormatExpression { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Lookup_ConnectionString { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Lookup_SqlClause { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string SqlInjectionWhiteList { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IgnoreInPreviewText { get; set; }

        #region IHtml Member

        public string ToHtmlString()
        {
            return (HtmlHelper.ToTable(
                new string[] { "Id", "Name", "Visible", "Examples", "Autocomplete", "Autocomplete Minlength" },
                new object[] { this.id, this.name, this.visible, this.examples, this.autocomplete, this.autocomplete_minlength }
            ));
        }

        #endregion
    }

    public class ResultFieldDTO // For backward compatibility
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string ColumnName { get; set; }

        [JsonProperty("visible", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("visible")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? Visible { get; set; }

        [JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string ColumnType { get; set; }
    }

    public class AssociatedLayer
    {
        public string id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string serviceid { get; set; }
    }

    public class TableExportFormat
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string FormatString { get; set; }
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string FileExtension { get; set; }
    }

    public class FeatureTransfer : IQueryFeatureTransferBridge, IAuthClone<FeatureTransfer>
    {
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Id { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IEnumerable<AuthObject<IQueryFeatureTransferTargetBridge>> AuthTargets { get; set; }

        public IEnumerable<IQueryFeatureTransferTargetBridge> Targets { get; private set; }

        public IEnumerable<AuthObject<IFieldSetter>> AuthFieldSetters { get; set; }

        public IEnumerable<IFieldSetter> FieldSetters { get; private set; }


        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public FeatureTransferMethod Method { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool CopyAttributes { get; set; }

        public FeatureTransfer AuthClone(CmsDocument.UserIdentification cmsui)
        {
            return new FeatureTransfer()
            {
                Id = this.Id,
                Name = this.Name,
                Method = this.Method,
                CopyAttributes = this.CopyAttributes,
                Targets = AuthObject<IQueryFeatureTransferTargetBridge>.QueryObjectArray(this.AuthTargets, cmsui),
                FieldSetters = AuthObject<IFieldSetter>.QueryObjectArray(this.AuthFieldSetters, cmsui)
            };
        }

        #region Classes

        public class Target : IQueryFeatureTransferTargetBridge
        {
            public string ServiceId { get; set; }

            public string EditThemeId { get; set; }

            public bool PipelineSuppressAutovalues { get; set; }
            public bool PipelineSuppressValidation { get; set; }
        }

        public class FieldSetter : IFieldSetter
        {
            public string Field { get; set; }
            public string ValueExpression { get; set; }

            public bool IsDefaultValue { get; set; }
            public bool IsRequired { get; set; }
        }

        #endregion
    }

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToNextLevelLink(this.id, this.name));

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Name" },
            new object[] { this.id, this.name }
        ));

        sb.Append(HtmlHelper.ToList(this.items, "Items", HtmlHelper.HeaderType.h6));

        return sb.ToString();
    }

    #endregion

    #region IAuthClone<Query> Member

    public QueryDTO AuthClone(CmsDocument.UserIdentification cmsui)
    {
        QueryDTO ret = new QueryDTO()
        {
            id = this.id,
            name = this.name,
            items = AuthObject<QueryDTO.ItemDTO>.QueryObjectArray(this.AuthItems, cmsui),
            AllowEmptyQueries = this.AllowEmptyQueries,
            LayerId = this.LayerId,
            Fields = AuthObject<TableFieldDTO>.QueryObjectArray(this.AuthFields, cmsui),
            TableExportFormats = AuthObject<TableExportFormat>.QueryObjectArray(this.AuthTableExportFormats, cmsui),

            associatedlayers = this.associatedlayers,

            geojuhu = this.geojuhu,
            geojuhuschema = this.geojuhuschema,
            Filter = this.Filter,
            draggable = this.draggable,
            Distinct = this.Distinct,
            Union = this.Union,
            ApplyZoomLimits = this.ApplyZoomLimits,
            MaxFeatures = this.MaxFeatures,
            zoomscale = zoomscale,

            AuthItems = this.AuthItems,
            AuthFields = this.AuthFields,
            AuthTableExportFormats = this.AuthTableExportFormats,
            AuthFeatureTransfers = this.AuthFeatureTransfers,

            QueryDef = this.QueryDef,
            Service = null,

            PreviewTextTemplate = this.PreviewTextTemplate,

            FeatureTransfers = AuthObject<QueryDTO.FeatureTransfer>.QueryObjectArray(this.AuthFeatureTransfers, cmsui)
        };



        if (ret.Fields != null)
        {
            ret.AutoSortFields = ret.Fields
                .Where(f => f is TableFieldData && ((TableFieldData)f).AutoSort != FieldAutoSortMethod.None)
                .Select(f =>
                {
                    switch (((TableFieldData)f).AutoSort)
                    {
                        case FieldAutoSortMethod.Descending:
                            return $"-{f.ColumnName}";
                        default:
                            return f.ColumnName;
                    }
                })
                .ToArray()
                .EmptyArrayToNull();
        }

        return ret;
    }

    #endregion

    #region IBridgeQuery Member

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Name
    {
        get { return this.name; }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSelectable
    {
        get
        {
            var layer = this.Service?.Layers?.FindByLayerId(this.LayerId);
            if (layer != null)
            {
                return (String.IsNullOrEmpty(layer.IdFieldName) || layer is IRasterlayer) == false;
            }

            return true;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string QueryGlobalId
    {
        get
        {
            return this.Service != null ? this.Service.Url + ":" + this.id : this.id;
        }
    }

    async public Task<FeatureCollection> PerformAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", int limit = 0, double mapScale = 0D)
    {
        var engine = new QueryEngine();
        if (await engine.PerformAsync(requestContext, this, filter, appendFilterClause: appendFilterClause, limit: limit, mapScale: mapScale))
        {
            return engine.Features;
        }

        var emptyResult = new FeatureCollection();
        emptyResult.AddWarnings(engine.Features?.Warnings);
        emptyResult.AddInformations(engine.Features?.Informations);

        return emptyResult;
    }

    async public Task<int> HasFeaturesAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        var engine = new QueryEngine();
        if (await engine.PerformAsync(requestContext, this, filter, QueryEngine.AdvancedQueryMethod.HasFeatures, appendFilterClause: appendFilterClause, mapScale: mapScale))
        {
            return engine.Features != null ? Math.Max(engine.Features.Count, engine.FeatureCount) : 0;
        }

        return 0;
    }

    async public Task<WebMapping.Core.Feature> FirstFeatureAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        var engine = new QueryEngine();
        if (await engine.PerformAsync(requestContext, this, filter, QueryEngine.AdvancedQueryMethod.FirstFeature, appendFilterClause: appendFilterClause, mapScale: mapScale))
        {
            return engine.Features != null && engine.Features.Count > 0 ? engine.Features[0] : null;
        }
        return null;
    }

    async public Task<WebMapping.Core.Geometry.Shape> FirstFeatureGeometryAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        var engine = new QueryEngine();
        if (await engine.PerformAsync(requestContext, this, filter, QueryEngine.AdvancedQueryMethod.FirstFeatureGeometry, appendFilterClause: appendFilterClause, mapScale: mapScale))
        {
            return engine.Features != null && engine.Features.Count > 0 ? engine.Features[0].Shape : null;
        }
        return null;
    }

    public void SetMapProperties(WebMapping.Core.Geometry.SpatialReference sRef, WebMapping.Core.Geometry.Envelope mapExtent, int mapImageWidth, int mapImageHeight)
    {
        if (this.Service?.Map != null)
        {
            this.Service.Map.SpatialReference = sRef;

            if (mapExtent != null && mapImageWidth > 0 && mapImageHeight > 0)
            {
                //if (sRef != null && sRef.Id != 4326)
                //{
                //    var sRef4326 = ApiGlobals.MapApplication.SpatialReferences.ById(4326);
                //    using (E.WebMapping.Geometry.GeometricTransformerPro transformer = new E.WebMapping.Geometry.GeometricTransformerPro(sRef4326, sRef))
                //    {
                //        transformer.Transform(mapBox4326);
                //        mapBox4326 = mapBox4326.ShapeEnvelope;
                //    }
                //}

                this.Service.Map.ImageWidth = mapImageWidth;
                this.Service.Map.ImageHeight = mapImageHeight;

                this.Service.Map.ZoomTo(mapExtent);
            }
        }
    }

    async public Task<string> LegendItemImageUrlAsync(IRequestContext requestContext, ApiQueryFilter apiFilter)
    {
        if (!(this.Service is IMapServiceLegend2))
        {
            return null;
        }

        string firstLegendValue = String.Empty;
        var engine = new QueryEngine();
        if (await engine.PerformAsync(requestContext, this, apiFilter, QueryEngine.AdvancedQueryMethod.FirstLegendValue))
        {
            try
            {
                firstLegendValue = engine.Features[0].Attributes["FirstLegendValue"]?.Value;
            }
            catch { }
        }

        return "/rest/services/" + this.Service.Url + "/getlegendlayeritem?f=bin&layer=" + this.LayerId + "&value=" + System.Net.WebUtility.UrlEncode(firstLegendValue);
    }

    public Task<string> LegendItemImageUrlAsync(E.Standard.WebMapping.Core.Feature feature, out string legendValue)
    {
        legendValue = String.Empty;

        try
        {
            if (!(this.Service is IMapServiceLegend2))
            {
                return Task.FromResult<string>(null);
            }

            var layer = this.Service.Layers?.Where(l => l.ID == this.LayerId).FirstOrDefault();

            StringBuilder val = new StringBuilder();
            if (layer is ILegendRendererHelper)
            {
                var legendHelper = (ILegendRendererHelper)layer;
                if (!String.IsNullOrEmpty(legendHelper.UniqueValue_Field1) && !String.IsNullOrEmpty(feature[legendHelper.UniqueValue_Field1]?.ToString()))
                {
                    val.Append((feature[legendHelper.UniqueValue_Field1].ToString()));
                }
                if (!String.IsNullOrEmpty(legendHelper.UniqueValue_Field2) && !String.IsNullOrEmpty(feature[legendHelper.UniqueValue_Field2]?.ToString()))
                {
                    val.Append(legendHelper.UniqueValue_FieldDelimiter + (feature[legendHelper.UniqueValue_Field2].ToString()));
                }
                if (!String.IsNullOrEmpty(legendHelper.UniqueValue_Field3) && !String.IsNullOrEmpty(feature[legendHelper.UniqueValue_Field3]?.ToString()))
                {
                    val.Append(legendHelper.UniqueValue_FieldDelimiter + (feature[legendHelper.UniqueValue_Field3].ToString()));
                }

                legendValue = val.ToString();
                return Task.FromResult("/rest/services/" + this.Service.Url + "/getlegendlayeritem?f=bin&layer=" + this.LayerId + "&value=" + System.Net.WebUtility.UrlEncode(legendValue));
            }
        }
        catch
        {

        }

        return Task.FromResult<string>(null);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IBridge Bridge { get; set; }

    public Dictionary<string, string> GetSimpleTableFields()
    {
        var fields = new Dictionary<string, string>();

        foreach (var field in this.Fields)
        {
            if (field is TableFieldData && field.FeatureFieldNames != null && field.FeatureFieldNames.Count() == 1)
            {
                fields[field.FeatureFieldNames.First()] = field.ColumnName;
            }
        }

        return fields;
    }

    #endregion

    #region Helper

    //private string ApplyVisFilterClause(string appendFilterClause)
    //{
    //    string visFilterClause = null;

    //    if (this.Bridge is Bridge)
    //    {
    //        visFilterClause = ((Bridge)this.Bridge).GetFilterDefinitionQuery(this);
    //    }

    //    if (!String.IsNullOrWhiteSpace(appendFilterClause))
    //    {
    //        if (String.IsNullOrWhiteSpace(visFilterClause))
    //        {
    //            visFilterClause = appendFilterClause;
    //        }
    //        else
    //        {
    //            visFilterClause = $"({ visFilterClause }) and ({ appendFilterClause})";
    //        }
    //    }

    //    return visFilterClause;
    //}

    #endregion
}