using System.Threading.Tasks;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.QueryStrategies;

/// <summary>
/// Implements one concrete way of querying features from an ArcGIS Server feature layer (see
/// <see cref="AgsQueryStrategy"/> for the available strategies and why they exist).
/// Implementations are stateless and selected per <see cref="MapService"/> via
/// <see cref="AgsQueryStrategyFactory"/>.
/// </summary>
internal interface IAgsQueryStrategy
{
    /// <summary>
    /// Resolves and appends the features matching <paramref name="filter"/> (and, for spatial
    /// queries, <paramref name="spatialFilter"/>) to <paramref name="features"/>. Mirrors the
    /// parameters <see cref="FeatureLayer.GetFeaturesProAsync"/> already resolved (where clause,
    /// resolved in/out spatial reference ids, ...) so implementations don't have to repeat that
    /// logic. <paramref name="layer"/> is passed to reach layer-specific state needed for the
    /// JSON-to-<see cref="Feature"/> conversion (id field name, geometry type, hasZ/hasM, ...)
    /// via <see cref="FeatureLayer.AppendJsonFeaturesTo"/>.
    /// </summary>
    /// <returns>
    /// Always <c>-1</c> (feature count is not tracked here - the count-only path in
    /// <see cref="FeatureLayer.GetFeaturesProAsync"/> is unaffected by the query strategy and
    /// handles its own return value). Kept as <c>Task&lt;int&gt;</c> to match the call site.
    /// </returns>
    Task<int> GetFeaturesAsync(
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
        IRequestContext requestContext);
}
