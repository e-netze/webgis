namespace E.Standard.WebMapping.GeoServices.ArcServer;

/// <summary>
/// Global, process-wide configuration for the ArcGIS Server spatial-query workaround
/// (see FeatureLayer.GetFeaturesProAsync). ArcGIS Server evaluates spatial queries against
/// SQL Server by first filtering on the bounding box of the query geometry (with a row limit
/// applied at that stage) and only afterwards clips the candidates against the real geometry.
/// This can lead to fewer (or even zero) results than actually exist within the query shape.
/// As a workaround, a "return ids only" query (which is not affected by this bug) is issued
/// first, followed by chunked "query by objectIds" requests to fetch the actual features.
/// </summary>
/// <remarks>
/// All properties can be overridden via api.config, section "tool-identify":
/// <c>ags-spatial-query-max-result-cap</c> (<see cref="MaxSpatialQueryResultCap"/>),
/// <c>ags-spatial-query-default-max-record-count-fallback</c> (<see cref="DefaultMaxRecordCountFallback"/>),
/// <c>ags-spatial-query-max-parallel-batch-requests</c> (<see cref="MaxParallelBatchRequests"/>),
/// <c>ags-spatial-query-ids-timeout-seconds</c> (<see cref="GetObjectIdsTimeoutSeconds"/>),
/// <c>ags-spatial-query-ids-paging-threshold</c> (<see cref="IdsPagingCandidateCountThreshold"/>).
/// See ApiGlobalsService for where these are read.
/// </remarks>
public static class AgsQuerySettings
{
    /// <summary>
    /// Upper bound on the number of object ids used for a single query when using the
    /// ids-first workaround. The full (unbounded) id list is always fetched from ArcGIS
    /// Server first (see remarks on <see cref="QueryService.GetObjectIdsAsync"/> for why),
    /// then capped client-side to this many ids. If more ids than this cap exist, only the
    /// first <see cref="MaxSpatialQueryResultCap"/> are used and the result is flagged as
    /// truncated (<c>FeatureCollection.HasMore</c>).
    /// </summary>
    public static int MaxSpatialQueryResultCap { get; set; } = 2000;

    /// <summary>
    /// Batch size used to fetch features by objectIds when the ArcGIS Server service does not
    /// report a usable <c>maxRecordCount</c> (e.g. older AGS versions where the service info
    /// does not expose it).
    /// </summary>
    public static int DefaultMaxRecordCountFallback { get; set; } = 1000;

    /// <summary>
    /// Maximum number of concurrent "query by objectIds" batch requests issued against ArcGIS
    /// Server while resolving a single spatial query.
    /// </summary>
    public static int MaxParallelBatchRequests { get; set; } = 4;

    /// <summary>
    /// Wall-clock time budget (in seconds) for <see cref="QueryService.GetObjectIdsAsync"/> to
    /// resolve the full set of matching object ids. Guards against a persistently misbehaving
    /// ArcGIS Server that keeps responding with many near-empty/clamped pages (each individually
    /// cheap, but adding up to an excessive total duration) - once exceeded, pagination stops
    /// and the result is reported as truncated (<c>HasMore=true</c>), the same way the
    /// <c>consecutiveEmptySkips</c> guard does, just bounded by elapsed time instead of by
    /// request count.
    /// </summary>
    public static int GetObjectIdsTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Candidate-count threshold below which <see cref="QueryService.GetObjectIdsAsync"/> skips
    /// keyset pagination entirely and resolves all matching ids via a single unbounded
    /// "returnIdsOnly" request instead. Keyset pagination (one <c>maxRecordCount</c>-sized page
    /// per round trip) exists to protect against huge result sets (potentially hundreds of
    /// thousands or millions of ids for a large query geometry) - for a query that only matches
    /// a comparatively small number of features, the extra round trips just add latency without
    /// any benefit, since ArcGIS Server reliably returns the full, uncapped id list for a
    /// <c>returnIdsOnly</c> request with no <c>resultRecordCount</c> supplied (see remarks on
    /// <see cref="QueryService.GetObjectIdsAsync"/>). The decision is based on an upfront
    /// "returnCountOnly" request against the actual query geometry/where-clause (not the
    /// bounding box - a plain count is not affected by the bbox/TOP bug and reflects the true,
    /// final match count), which is itself cheap and fast in the common case.
    /// </summary>
    public static int IdsPagingCandidateCountThreshold { get; set; } = 50000;
}
