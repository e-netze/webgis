using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using System;
using System.Threading.Tasks;
using System.Web;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class ImageServerLayer : RestLayer, ILayer2
{
    private new readonly ImageServerService _service;
    private readonly string _id = String.Empty;

    public ImageServerLayer(string name, string id, ImageServerService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
        _service = service;
        _id = id;
    }

    public ImageServerLayer(string name, string id, LayerType type, ImageServerService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
        _service = service;
        _id = id;
    }

    public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext)
    {
        return GetFeaturesAsync(filter, result, requestContext, false);
    }

    async public Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext, bool countOnly)
    {
        Point clickPoint = null;

        if (!(filter is SpatialFilter) && ((SpatialFilter)filter).QueryShape != null)
        {
            return false;
        }

        clickPoint = ((SpatialFilter)filter).QueryShape.ShapeEnvelope.CenterPoint;

        using (var transormer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, (filter as SpatialFilter)?.FilterSpatialReference?.Id ?? 0, _service.SpatialReferenceId))
        {
            transormer.Transform(clickPoint);
        }

        string requestParameters =
            $"geometry={clickPoint.X.ToPlatformNumberString()},{clickPoint.Y.ToPlatformNumberString()}&geometryType=esriGeometryPoint&f=pjson&returnGeometry=true&renderingRule={HttpUtility.UrlEncode(_service.RenderingRuleIdentify)}";
        string featuresReqUrl = $"{_service.ServiceUrl}/identify?{requestParameters}";

        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string featuresResponse = await authHandler.TryGetAsync(_service, featuresReqUrl);
        var jsonRasterResponse = JSerializer.Deserialize<JsonImageServerIdentifyResponse>(featuresResponse);

        JsonRasterAttributeTable rasterAttributeTable = null;

        if (!String.IsNullOrWhiteSpace(_service.RenderingRuleIdentify) && countOnly == false)
        {
            try
            {
                string rasterAttributeTableUrl = $"{_service.ServiceUrl}/rasterAttributeTable?f=json&renderingRule={HttpUtility.UrlEncode(_service.RenderingRuleIdentify)}";
                string rasterAttributeTableResponse = await authHandler.TryGetAsync(_service, rasterAttributeTableUrl);

                rasterAttributeTable = JSerializer.Deserialize<JsonRasterAttributeTable>(rasterAttributeTableResponse);
            }
            catch { }
        }

        if (jsonRasterResponse.Location != null && jsonRasterResponse.Location.X.HasValue && jsonRasterResponse.Location.Y.HasValue)
        {
            Feature feature = new Feature();
            var attributeName = _service.PixelAliasname.OrTake(jsonRasterResponse.Name).OrTake("Value");
            feature.Attributes.Add(new Core.Attribute(attributeName, rasterAttributeTable.TranslateValue(jsonRasterResponse.Value)));

            var shape = new Point(jsonRasterResponse.Location.X.Value, jsonRasterResponse.Location.Y.Value);
            using (var transormer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, jsonRasterResponse.Location.SpatialReference?.EpsgCode ?? 0, filter.FeatureSpatialReference?.Id ?? 0))
            {
                transormer.Transform(shape);
            }
            feature.Shape = shape;
            result.Add(feature);
        }

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is ImageServerService imageServerService)
        {
            ImageServerLayer clone = new ImageServerLayer(this.Name, this.ID, this.Type, imageServerService, this.Queryable);
            clone.ClonePropertiesFrom(this);
            base.CloneParentLayerIdsTo(clone);
            return clone;
        }

        return null;
    }

    #region ILayer2 Member

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        await GetFeaturesAsync(filter, features, requestContext, true);
        int count = features.Count;

        return count;
    }

    public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        return Task.FromResult<Shape>(null);
    }

    async public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        if (await GetFeaturesAsync(filter, features, requestContext) && features.Count > 0)
        {
            return features[0];
        }

        return null;
    }

    #endregion
}
