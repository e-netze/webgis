using System;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.GeoServices.ArcServer;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.QueryStrategies;

/// <summary>
/// Workaround for the ArcGIS Server spatial-query bbox/TOP bug: ArcGIS Server internally
/// pre-filters spatial queries against the bounding box of the query geometry (with a row
/// limit applied at that stage) and only clips against the real geometry afterwards. This can
/// silently drop matching features (in the worst case down to 0 results), unless the query
/// shape is itself an Envelope (where shape == bbox, so no clipping is needed and the bug
/// can't manifest).
/// <para>
/// This strategy resolves the full, correct set of matching object ids first (via
/// "returnIdsOnly", which is not affected by the bug, see <see cref="QueryService.GetObjectIdsAsync"/>),
/// then fetches the actual features in id-batches (a pure objectId lookup, likewise
/// unaffected, see <see cref="QueryService.GetFeaturesByObjectIdsAsync"/>). The result count is
/// capped at <see cref="AgsQuerySettings.MaxSpatialQueryResultCap"/> to bound worst-case
/// transfer size/traffic; if more matches exist, <c>FeatureCollection.HasMore</c> is set.
/// </para>
/// <para>
/// Also applied to plain attribute queries (<c>spatialFilter</c> is <c>null</c> in that case) -
/// the ids-first approach is correct there too, just not strictly necessary to work around the
/// bbox/TOP bug (which only affects spatial queries). More expensive than
/// <see cref="DefaultAgsQueryStrategy"/>, so only meant to be opted-in
/// (<see cref="MapService.QueryStrategy"/> = <see cref="AgsQueryStrategy.BoundingBoxProblem"/>)
/// for services/databases actually exhibiting the bug.
/// </para>
/// </summary>
internal sealed class BoundingBoxProblemAgsQueryStrategy : IAgsQueryStrategy
{
    public async Task<int> GetFeaturesAsync(
        MapService service,
        FeatureLayer layer,
        SpatialFilter spatialFilter,
        QueryFilter filter,
        FeatureCollection features,
        string featuresReqUrl,
        string where,
        int resolvedInSrefId,
        int resolvedOutSrefId,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext)
    {
        var queryService = new QueryService();

        var queryParams = new IdsWorkaroundQueryParams
        {
            Geometry = spatialFilter?.QueryShape,
            GeometrySrefId = spatialFilter?.FilterSpatialReference != null ? spatialFilter.FilterSpatialReference.Id : 0,
            Where = where,
            OutFields = String.IsNullOrWhiteSpace(filter.SubFields)
                ? "*"
                : filter.SubFields.Replace(" ", ","),
            OrderByFields = filter.OrderBy,
            TimeEpoch = filter.TimeEpoch,
            InSrefId = resolvedInSrefId,
            OutSrefId = resolvedOutSrefId,
            DatumTransformationId = service.DatumTransformations?.FirstOrDefault() ?? 0,
            ReturnZ = layer.HasZ,
            ReturnM = layer.HasM,
            ReturnGeometry = filter.QueryGeometry
        };

        var (ids, hasMore) = await queryService.GetObjectIdsAsync(
            service, featuresReqUrl, authHandler, requestContext, queryParams, layer.IdFieldName, AgsQuerySettings.MaxSpatialQueryResultCap);

        var jsonFeatureResponses = await queryService.GetFeaturesByObjectIdsAsync(
            service, featuresReqUrl, authHandler, requestContext, queryParams, ids);

        foreach (var jsonFeatureResponse in jsonFeatureResponses)
        {
            layer.AppendJsonFeaturesTo(jsonFeatureResponse, features, filter);
        }

        features.Query = filter;
        features.Layer = layer;

        features.HasMore = hasMore || jsonFeatureResponses.Any(r => r.ExceededTransferLimit);

        return -1;
    }
}
