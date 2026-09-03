using System.Threading.Tasks;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.QueryStrategies;

/// <summary>
/// Resolves the <see cref="IAgsQueryStrategy"/> to use for a given query, based on the
/// service's <see cref="MapService.QueryStrategy"/> flag. <see cref="DefaultAgsQueryStrategy"/>
/// is preferred whenever it is safe to use it: it needs no separate ids-resolution round trip
/// and is therefore always the cheaper/faster option (see
/// <see cref="BoundingBoxProblemAgsQueryStrategy"/> for background on the bug it works around).
/// </summary>
internal static class AgsQueryStrategyFactory
{
    private static readonly IAgsQueryStrategy _default = new DefaultAgsQueryStrategy();
    private static readonly IAgsQueryStrategy _boundingBoxProblem = new BoundingBoxProblemAgsQueryStrategy();

    /// <summary>
    /// Resolves the strategy to use. Services not opted into <see cref="AgsQueryStrategy.BoundingBoxProblem"/>
    /// always get <see cref="DefaultAgsQueryStrategy"/>. Services that are opted in still only
    /// get <see cref="BoundingBoxProblemAgsQueryStrategy"/> if the query can actually trigger the
    /// bug (see <see cref="CanTriggerBoundingBoxBug"/>) - otherwise <see cref="DefaultAgsQueryStrategy"/>
    /// is used too, since it is cheaper and gives the same, correct result. As a further
    /// refinement, even a query that *could* trigger the bug is answered by
    /// <see cref="DefaultAgsQueryStrategy"/> if a cheap upfront bounding-box-only count shows
    /// that the number of candidates ArcGIS Server would clip against stays below the transfer
    /// limit - in that case the bug cannot manifest either, because nothing gets clamped away
    /// before the clip happens.
    /// </summary>
    public static async Task<IAgsQueryStrategy> GetStrategyAsync(
        MapService service,
        SpatialFilter spatialFilter,
        string featuresReqUrl,
        string where,
        int resolvedInSrefId,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext)
    {
        if (service.QueryStrategy != AgsQueryStrategy.BoundingBoxProblem)
        {
            return _default;
        }

        if (!CanTriggerBoundingBoxBug(spatialFilter))
        {
            return _default;
        }

        int transferLimit = service.MaxRecordCount > 0
            ? service.MaxRecordCount
            : AgsQuerySettings.DefaultMaxRecordCountFallback;

        var queryService = new QueryService();

        int bboxCandidateCount = await queryService.GetBoundingBoxCandidateCountAsync(
            service, spatialFilter, featuresReqUrl, where, resolvedInSrefId, authHandler, requestContext);

        // Fewer candidates within the query geometry's bounding box than fit in a single page:
        // ArcGIS Server's internal bbox pre-filter can't have clamped anything away before the
        // clip against the real geometry happens, so a single plain (and cheaper) request
        // already returns the full, correct result - same as the ids-first workaround, just
        // without its extra round trips.
        return bboxCandidateCount < transferLimit ? _default : _boundingBoxProblem;
    }

    /// <summary>
    /// The ArcGIS Server bbox/TOP bug can only manifest when the database-side pre-filter
    /// (bounding box of the query geometry) can return a *different* candidate set than the
    /// real query geometry - i.e. when clipping after the fact can actually drop something.
    /// That is never the case for:
    /// <list type="bullet">
    /// <item>plain attribute queries (no spatial filter/geometry at all),</item>
    /// <item>a query geometry that is itself an <see cref="Envelope"/> (bbox == geometry), or</item>
    /// <item>a query geometry that is a single <see cref="Point"/> (its bounding box is the
    /// point itself, zero area/extent).</item>
    /// </list>
    /// Every other geometry type (line, polygon, multipoint, ...) can have a bounding box that
    /// is strictly larger than the shape itself, so the bug can, in principle, manifest there.
    /// </summary>
    private static bool CanTriggerBoundingBoxBug(SpatialFilter spatialFilter)
    {
        return spatialFilter?.QueryShape switch
        {
            null => false,
            Envelope => false,
            Point => false,
            _ => true,
        };
    }
}
