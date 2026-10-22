using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IQueryBridge : IApiObjectBridge
{
    string Name { get; }
    string QueryGlobalId { get; }

    bool IsSelectable { get; }

    Task<FeatureCollection> PerformAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", int limit = 0, double mapScale = 0D);

    Task<int> HasFeaturesAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D);

    Task<Feature> FirstFeatureAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D);

    Task<Shape> FirstFeatureGeometryAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D);

    /// <summary>
    /// Server-side statistics over the whole query result (all matching features, not only the loaded ones).
    /// Only supported if the layer implements <see cref="ILayerStatisticsQuery"/> with SupportsStatistics.
    /// </summary>
    Task<LayerStatistics> QueryStatisticsAsync(IRequestContext requestContext, ApiQueryFilter filter, LayerStatisticsRequest statisticsRequest, string appendFilterClause = "", double mapScale = 0D);

    void SetMapProperties(SpatialReference sRef, Envelope mapBox4326, int mapImageWidth, int mapImageHeight);

    Task<string> LegendItemImageUrlAsync(IRequestContext requestContext, ApiQueryFilter filter);

    Task<string> LegendItemImageUrlAsync(Feature feature, out string legendValue);

    string GetServiceId();
    System.Guid? GetServiceGuid();

    string GetLayerId();

    LayerType GetLayerType();

    Dictionary<string, string> GetSimpleTableFields();

    bool Distinct { get; }
    bool Union { get; }

    bool ApplyZoomLimits { get; }
    int MaxFeatures { get; }

    IBridge Bridge { get; }
}
