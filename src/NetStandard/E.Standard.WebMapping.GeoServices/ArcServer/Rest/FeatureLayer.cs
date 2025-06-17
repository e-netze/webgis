using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Renderer;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class FeatureLayer : RestLayer,
                     ILayer2,
                     ILayer3,
                     ILayerDistinctQuery,
                     ILabelableLayer,
                     ILegendRendererHelper,
                     ILayerFieldDomains,
                     ILayerGeometryDefintion,
                     IFeatureAttachmentProvider
{
    private string _definitionExpression = String.Empty;
    private new readonly MapService _service;
    private readonly string _id = String.Empty;
    private ThreadSafe.ThreadSafeDictionary<string, JsonDomain> _domains = null;

    public FeatureLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
        _service = (MapService)service;
        _id = id;

        HasM = HasZ = false;
    }
    public FeatureLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
        _service = (MapService)service;
        _id = id;

        HasM = HasZ = false;
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection features, IRequestContext requestContext)
    {
        await GetFeaturesProAsync(filter, features, false, requestContext);
        return true;
    }

    async public Task<int> GetFeaturesProAsync(QueryFilter filter, FeatureCollection features, bool countOnly, IRequestContext requestContext)
    {
        string featuresReqUrl = $"{_service.Service}/{this._id}/query";
        var jsonFeatureResponse = new JsonFeatureResponse();
        int outSrefId =
                (filter.FeatureSpatialReference != null)
                    ? filter.FeatureSpatialReference.Id
                    : 0,
            inSrefId = 0;
        string where = UrlEncodeWhere(filter.Where)
                           .AppendWhereClause(this.Filter)
                           .OrTake(String.IsNullOrEmpty(this.IdFieldName)
                               ? "1=1"
                               : $"{this.IdFieldName}>0");
        var requestBuilder = new GetFeaturesRequestBuilder();

        if (filter is SpatialFilter spatialFilter)
        {
            inSrefId = spatialFilter.FilterSpatialReference != null
                ? spatialFilter.FilterSpatialReference.Id
                : 0;

            requestBuilder
                .WithGeometry(spatialFilter.QueryShape, inSrefId)
                .WithSpatialRelationIntersects();
        }

        requestBuilder
            .WithOutFields(
                String.IsNullOrWhiteSpace(filter.SubFields)
                    ? "*"
                    : filter.SubFields.Replace(" ", ","))
            .WithWhereClause(where)
            .WithOrderByFields(filter.OrderBy)
            .WithResultRecordCount(null)  // always set null with AGS: otherwise Pagination Errors and bad performance
            .WithInSpatialReferenceId(
                _service.ProjectionMethode switch
                {
                    ServiceProjectionMethode.Map => inSrefId.OrTake(_service.Map.SpatialReference?.Id ?? 0),
                    ServiceProjectionMethode.Userdefined => inSrefId.OrTake(_service.ProjectionId),
                    _ => inSrefId
                })
            .WithOutSpatialReferenceId(
                _service.ProjectionMethode switch
                {
                    ServiceProjectionMethode.Map => outSrefId.OrTake(_service.Map.SpatialReference?.Id ?? 0),
                    ServiceProjectionMethode.Userdefined => outSrefId.OrTake(_service.ProjectionId),
                    _ => outSrefId
                })
            .WithDatumTransformation(_service.DatumTransformations?.FirstOrDefault() ?? 0)
            .WithReturnZ(this.HasZ, ignoreIfFalse: true)
            .WithReturnM(this.HasM, ignoreIfFalse: true)
            .WithFormat("json");

        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        if (countOnly)
        {
            requestBuilder
                .WithReturnCountOnly(true)
                .WithReturnIdsOnly(true)
                .WithReturnGeometry(false);

            string featuresResponse = await requestContext.LogRequest(
                _service.Server,
                _service.ServiceShortname,
                requestBuilder.Build(),
                "getfeatures",
                (requestBody) => authHandler.TryPostAsync(
                    _service,
                    featuresReqUrl,
                    requestBody));

            var jsonFeatureCountResponse = JSerializer.Deserialize<JsonFeatureCountResponse>(featuresResponse);

            return jsonFeatureCountResponse.Count;
        }
        else
        {
            requestBuilder
                .WithReturnCountOnly(false)
                .WithReturnIdsOnly(false)
                .WithReturnGeometry(filter.QueryGeometry);

            string featuresResponse = await requestContext.LogRequest(
                _service.Server,
                _service.ServiceShortname,
                requestBuilder.Build(),
                "getfeatures",
                (requestBody) => authHandler.TryPostAsync(
                    _service,
                    featuresReqUrl,
                    requestBody));

            jsonFeatureResponse = JSerializer.Deserialize<JsonFeatureResponse>(featuresResponse);
        }

        List<string> dateColumns = new List<string>();
        Dictionary<string, string> fieldAliases = new();

        foreach (var field in jsonFeatureResponse.Fields)
        {
            if (field.Name != null && field.Type != null && field.Type == "esriFieldTypeDate")
            {
                dateColumns.Add(field.Name);
            }
            fieldAliases[field.Name] = field.Alias;
        }

        foreach (var jsonFeature in jsonFeatureResponse.Features)
        {
            Feature feature = new Feature();

            if (this.Type == LayerType.line && jsonFeature.Geometry?.Paths != null)
            {
                Polyline polyline = new Polyline();
                for (int p = 0, to = jsonFeature.Geometry.Paths.Length; p < to; p++)
                {
                    Path path = new Path();

                    var pathsPointsArray = jsonFeature.Geometry.Paths[p];
                    var dimension = pathsPointsArray.GetLength(1); // 2D 3D 3D+M ?
                    int pathsPointsArrayLength = (pathsPointsArray.Length / dimension);

                    for (int multiArrayIndex = 0; multiArrayIndex < pathsPointsArrayLength; multiArrayIndex++)
                    {
                        path.AddPoint(ArrayToPoint(pathsPointsArray, multiArrayIndex, dimension));
                    }
                    polyline.AddPath(path);
                }

                feature.Shape = polyline;
            }
            else if (this.Type == LayerType.polygon && jsonFeature.Geometry?.Rings != null)
            {
                Polygon polygon = new Polygon();
                for (int r = 0, to = jsonFeature.Geometry.Rings.Length; r < to; r++)
                {
                    Ring ring = new Ring();

                    var ringsPointsArray = jsonFeature.Geometry.Rings[r];
                    var dimension = ringsPointsArray.GetLength(1); // 2D 3D 3D+M ?
                    int ringsPointsArrayLength = (ringsPointsArray.Length / dimension);

                    for (int multiArrayIndex = 0; multiArrayIndex < ringsPointsArrayLength; multiArrayIndex++)
                    {
                        //Point point = new Point();
                        //point.X = ringsPointsArray[multiArrayIndex, 0];
                        //point.Y = ringsPointsArray[multiArrayIndex, 1];

                        ring.AddPoint(ArrayToPoint(ringsPointsArray, multiArrayIndex, dimension));
                    }
                    polygon.AddRing(ring);
                }

                feature.Shape = polygon;
            }
            else if (this.Type == LayerType.point &&
                        (jsonFeature.Geometry?.X != null) &&
                        (jsonFeature.Geometry?.Y != null)
                    )
            {
                Point shape = this.HasM ? new PointM() : new Point();
                shape.X = jsonFeature.Geometry.X.Value;
                shape.Y = jsonFeature.Geometry.Y.Value;

                if (this.HasZ && jsonFeature.Geometry.Z.HasValue)
                {
                    shape.Z = jsonFeature.Geometry.Z.Value;
                }

                if (this.HasM && jsonFeature.Geometry.M.HasValue)
                {
                    ((PointM)shape).M = jsonFeature.Geometry.M.Value;
                }

                feature.Shape = shape;
            }
            else if (this.Type == LayerType.point &&
                    jsonFeature.Geometry?.Points != null &&
                    jsonFeature.Geometry.Points.Length > 0)
            {
                MultiPoint multiPoint = new MultiPoint();

                for (int p = 0, pointCount = jsonFeature.Geometry.Points.Length; p < pointCount; p++)
                {
                    var doubleArray = jsonFeature.Geometry.Points[p];
                    if (doubleArray.Length >= 2)
                    {
                        var point = new Point(doubleArray[0], doubleArray[1]);

                        multiPoint.AddPoint(point);
                    }
                }

                feature.Shape = multiPoint;
            }
            else { }

            var featureAttributes = (IDictionary<string, object>)jsonFeature.Attributes ?? new Dictionary<string, object>();

            foreach (var featureProperty in featureAttributes)
            {
                string name = featureProperty.Key;
                var value = featureProperty.Value;

                if (dateColumns.Contains(name))
                {
                    feature.Attributes.Add(new Core.Attribute(
                                name,
                                fieldAliases.ContainsKey(name) ? fieldAliases[name] : name,
                                value.EsriDateToString()));
                }
                else
                {
                    if (!filter.SuppressResolveAttributeDomains && _domains?.ContainsKey(name) == true)
                    {
                        value = DomainValue(name, value?.ToString());
                    }
                    feature.Attributes.Add(new Core.Attribute(
                        name,
                        fieldAliases.ContainsKey(name) ? fieldAliases[name] : name,
                        value?.ToString()));
                }

                if (name == this.IdFieldName)
                {
                    feature.Oid = int.Parse(value.ToString());
                }
            }

            if (feature.Shape != null)
            {
                if (this.HasZ) { feature.Shape.HasZ = true; }
                if (this.HasM) { feature.Shape.HasM = true; }
            }

            features.Add(feature);
        }

        features.Query = filter;
        features.Layer = this;

        features.HasMore = jsonFeatureResponse.ExceededTransferLimit;

        return -1;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        FeatureLayer clone = new FeatureLayer(this.Name, this.ID, this.Type, parent, this.Queryable);
        clone.ClonePropertiesFrom(this);

        clone._definitionExpression = _definitionExpression;
        clone._domains = _domains;
        clone.UseLabelRenderer = this.UseLabelRenderer;
        clone.LabelRenderer = this.LabelRenderer;

        clone.LengendRendererType = this.LengendRendererType;
        clone.UniqueValue_Field1 = this.UniqueValue_Field1;
        clone.UniqueValue_Field2 = this.UniqueValue_Field2;
        clone.UniqueValue_Field3 = this.UniqueValue_Field3;
        clone.UniqueValue_FieldDelimiter = this.UniqueValue_FieldDelimiter;

        clone.HasM = this.HasM;
        clone.HasZ = this.HasZ;
        clone.HasAttachments = this.HasAttachments;

        base.CloneParentLayerIdsTo(clone);
        return clone;
    }

    public string DefinitionExpression
    {
        get;
        set;
    }

    #region Domains

    public void AddDomains(string fieldName, JsonDomain domains)
    {
        if (_domains == null)
        {
            _domains = new ThreadSafe.ThreadSafeDictionary<string, JsonDomain>();
        }

        _domains.Add(fieldName, domains);
    }

    private string DomainValue(string fieldName, string value)
    {
        if (!_domains.ContainsKey(fieldName))
        {
            return value;
        }

        JsonDomain domain = _domains[fieldName];
        if (domain.Type == "codedValue" && domain.CodedValues != null)
        {
            string newValue = domain.CodedValues.Where(m => m.Code == value).Select(m => m.Name).FirstOrDefault();
            if (newValue != null)
            {
                value = newValue;
            }
        }

        return value;
    }

    #endregion

    #region IAttachmentContainer

    public bool HasAttachments
    {
        get; set;
    }

    async public Task<IEnumerable<string>> HasAttachmentsFor(IRequestContext requestContext, IEnumerable<string> ids)
    {
        if (!this.HasAttachments) return [];

        string attachmentReqUrl = $"{_service.Service}/{this._id}/queryAttachments?returnUrl=true&f=pjson&objectIds={String.Join(",", ids)}";
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string attachmentResponseString = await requestContext.LogRequest(
            _service.Server,
            _service.ServiceShortname,
            attachmentReqUrl,
            "gethasattachmentsfor",
            (requestBody) => authHandler.TryGetAsync(
                _service,
                attachmentReqUrl));

        var attachmentResponse = JSerializer.Deserialize<JsonAttachmentResponse>(attachmentResponseString);

        return attachmentResponse.AttachmentGroups
            ?.Select(a => a.ParentObjectId.ToString())
            .Distinct()
            .ToArray() ?? [];
    }

    async public Task<IFeatureAttachments> GetAttachmentsFor(IRequestContext requestContext, string id)
    {
        if (!this.HasAttachments) return null;

        var result = new FeatureAttachements();

        string attachmentReqUrl = $"{_service.Service}/{this._id}/queryAttachments?returnUrl=true&f=pjson&objectIds={id}";
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string attachmentResponseString = await requestContext.LogRequest(
            _service.Server,
            _service.ServiceShortname,
            attachmentReqUrl,
            "getattachmentsfor",
            (requestBody) => authHandler.TryGetAsync(
                _service,
                attachmentReqUrl));

        var attachmentResponse = JSerializer.Deserialize<JsonAttachmentResponse>(attachmentResponseString);

        foreach (var attachment in attachmentResponse
                                    ?.AttachmentGroups
                                    ?.SelectMany(g => g.AttachmentInfos)
                                    .Where(i => (i.ContentType == "image/jpeg"
                                             || i.ContentType == "image/jpg"
                                             || i.ContentType == "image/gif"
                                             || i.ContentType == "image/png")
                                             && !String.IsNullOrEmpty(i.Url)) ?? [])
        {
            var data = await authHandler.TryGetRawAsync(_service, attachment.Url);

            if (data is not null && data.Length > 0)
            {
                result.Add(new FeatureAttachment()
                {
                    Name = attachment.Name,
                    Type = attachment.ContentType,
                    Data = data
                });
            }
        }

        return result;
    }

    #endregion

    #region ILayerFieldDomains

    public IEnumerable<KeyValuePair<string, string>> CodedValues(string fieldName)
    {
        List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

        if (_domains.ContainsKey(fieldName))
        {
            JsonDomain domain = _domains[fieldName];
            if (domain.Type == "codedValue" && domain.CodedValues != null)
            {
                foreach (var codedValue in domain.CodedValues)
                {
                    result.Add(new KeyValuePair<string, string>(codedValue.Code, codedValue.Name));
                }
            }
        }

        return result;
    }

    #endregion

    #region ILayer2 Members

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        var count = await this.GetFeaturesProAsync(filter, features, true, requestContext);
        return count;
    }

    async public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        if (await GetFeaturesProAsync(filter, features, true, requestContext) > 10)  // Nicht eindeutig! Es sollten hier auch nicht zu viele Abgefragt werden->kann zu hohen Trafic führen (Deep Identify->Geometrie holen für sketch->alle Layer werden abgefragt...)
        {
            return null;
        }

        filter = filter.Clone();
        filter.FeatureLimit = 1;
        filter.QueryGeometry = true;
        filter.SubFields = $"{this.IdFieldName},{this.ShapeFieldName}";

        await GetFeaturesAsync(filter, features, requestContext);
        if (features.Count > 0)
        {
            return features[0].Shape;
        }

        return null;
    }

    async public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        if (await GetFeaturesProAsync(filter, features, true, requestContext) > 10)  // Nicht eindeutig! Es sollten hier auch nicht zu viele Abgefragt werden->kann zu hohen Trafic führen (Deep Identify->Geometrie holen für sketch->alle Layer werden abgefragt...)
        {
            return null;
        }

        filter = filter.Clone();
        filter.FeatureLimit = 1;
        filter.QueryGeometry = true;
        filter.SubFields = String.IsNullOrEmpty(filter.SubFields) ? $"{this.IdFieldName},{this.ShapeFieldName}" : filter.SubFields;

        await GetFeaturesAsync(filter, features, requestContext);
        if (features.Count > 0)
        {
            return features[0];
        }

        return null;
    }

    #endregion

    #region ILayer3

    async public Task<int> FeaturesCountOnly(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        var count = await this.GetFeaturesProAsync(filter, features, true, requestContext);

        return count;
    }

    #endregion

    #region ILayerDistinctQuery

    async public Task<IEnumerable<string>> QueryDistinctValues(IRequestContext requestContext, string field, string where = "", string orderBy = "", int featureLimit = 0)
    {
        string featuresReqUrl = $"{_service.Service}/{this._id}/query";
        var requestBuilder = new GetFeaturesRequestBuilder()
            .WithOutFields(field)
            .WithWhereClause(UrlEncodeWhere(where).OrTake("1=1"))
            .WithOrderByFields(orderBy)
            .WithResultRecordCount(featureLimit > 0 ? featureLimit : null)
            .WithReturnGeometry(false)
            .WithReturnDistinctValues(true)
            .WithFormat("json");

        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string featuresResponse = await authHandler.TryPostAsync(_service, featuresReqUrl, requestBuilder.Build());
        var jsonFeatureResponse = JSerializer.Deserialize<JsonFeatureResponse>(featuresResponse);

        if (jsonFeatureResponse?.Features == null)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();

        foreach (var jsonFeature in jsonFeatureResponse.Features)
        {
            var featureAttributes = (IDictionary<string, object>)jsonFeature.Attributes ?? new Dictionary<string, object>();

            foreach (var featureProperty in featureAttributes)
            {
                string name = featureProperty.Key;
                var value = featureProperty.Value;

                if (value != null && field.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(value?.ToString());
                }
            }
        }

        return result;
    }

    #endregion

    #region ILabelableLayer Member

    public LabelRenderer LabelRenderer
    {
        set;
        get;
    }

    public bool UseLabelRenderer
    {
        get;
        set;
    }

    #endregion

    #region Helper

    static internal string UrlEncodeWhere(string str)
    {
        if (String.IsNullOrWhiteSpace(str))
        {
            return String.Empty;
        }

        return str.Replace("%", "%25")
                  .Replace("+", "%2B")
                  .Replace("/", "%2F")
                  //.Replace(@"\", "%5C")   // Darf man nicht ersetzen!! Sonst geht beim Kunden der Filter für Usernamen nicht mehr!!!!!!
                  .Replace("&", "%26");
    }

    private Point ArrayToPoint(double?[,] pointArray, int index, int dimension)
    {
        var point = this.HasM ? new PointM() : new Point();

        int dimensionIndex = 0;

        point.X = pointArray[index, dimensionIndex++] ?? 0D;
        point.Y = pointArray[index, dimensionIndex++] ?? 0D;

        if (this.HasZ && dimensionIndex < dimension)
        {
            point.Z = pointArray[index, dimensionIndex++] ?? 0D;
        }

        if (this.HasM && dimensionIndex < dimension)
        {
            if (pointArray[index, dimensionIndex].HasValue)
            {
                ((PointM)point).M = pointArray[index, dimensionIndex++];
            }
        }

        return point;
    }

    #endregion

    #region ILegendRendererHelper

    public LayerRendererType LengendRendererType { get; set; }
    public string UniqueValue_Field1 { get; set; }
    public string UniqueValue_Field2 { get; set; }
    public string UniqueValue_Field3 { get; set; }
    public string UniqueValue_FieldDelimiter { get; set; }

    async public Task<string> FirstLegendValueAsync(QueryFilter filter, IRequestContext requestContext)
    {
        try
        {
            if (LengendRendererType == LayerRendererType.UniqueValue)
            {
                FeatureCollection features = new FeatureCollection();

                if (await GetFeaturesProAsync(filter, features, true, requestContext) > 10)  // Nicht eindeutig! Es sollten hier auch nicht zu viele Abgefragt werden->kann zu hohen Trafic führen (Deep Identify->Geometrie holen für sketch->alle Layer werden abgefragt...)
                {
                    return String.Empty;
                }

                filter = filter.Clone();
                filter.FeatureLimit = 1;
                filter.QueryGeometry = true;

                var subFields = new StringBuilder();
                subFields.Append(this.IdFieldName);
                subFields.Append(!String.IsNullOrWhiteSpace(UniqueValue_Field1) ? $" {UniqueValue_Field1}" : "");
                subFields.Append(!String.IsNullOrWhiteSpace(UniqueValue_Field2) ? $" {UniqueValue_Field2}" : "");
                subFields.Append(!String.IsNullOrWhiteSpace(UniqueValue_Field3) ? $" {UniqueValue_Field3}" : "");

                filter.SubFields = subFields.ToString();

                await GetFeaturesAsync(filter, features, requestContext);
                if (features.Count > 0)
                {
                    var feature = features[0];

                    StringBuilder sb = new StringBuilder();
                    if (!String.IsNullOrWhiteSpace(UniqueValue_Field1))
                    {
                        sb.Append(feature[UniqueValue_Field1]);
                    }
                    if (!String.IsNullOrWhiteSpace(UniqueValue_Field2))
                    {
                        sb.Append(UniqueValue_FieldDelimiter.Trim());
                        sb.Append(feature[UniqueValue_Field2]);
                    }
                    if (!String.IsNullOrWhiteSpace(UniqueValue_Field3))
                    {
                        sb.Append(UniqueValue_FieldDelimiter.Trim());
                        sb.Append(feature[UniqueValue_Field3]);
                    }
                    return sb.ToString();
                }
            }
        }
        catch { }
        return String.Empty;
    }

    #endregion

    #region ILayerGeometryDefintion

    public bool HasZ { get; set; }

    public bool HasM { get; set; }

    #endregion
}
