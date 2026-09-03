using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Extensions;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

/// <summary>
/// Parameters shared between the ids-query and the objectIds-batch-queries of the
/// ids-first query workaround (see <see cref="QueryService"/>). <see cref="Geometry"/> is
/// only set for spatial queries; for plain attribute queries it stays <c>null</c> and no
/// geometry/spatialRel parameters are sent.
/// </summary>
internal sealed class IdsWorkaroundQueryParams
{
    public Shape Geometry { get; set; }
    public int GeometrySrefId { get; set; }
    public string Where { get; set; }
    public string OutFields { get; set; }
    public string OrderByFields { get; set; }
    public TimeEpochDefinition TimeEpoch { get; set; }
    public int InSrefId { get; set; }
    public int OutSrefId { get; set; }
    public int DatumTransformationId { get; set; }
    public bool ReturnZ { get; set; }
    public bool ReturnM { get; set; }
    public bool ReturnGeometry { get; set; }
}

/// <summary>
/// Encapsulates the ArcGIS Server request/response handling for the ids-first query
/// workaround (see <see cref="AgsQuerySettings"/>), primarily used to work around the
/// spatial-query bbox/TOP bug: resolve the full, correct set of matching object ids first
/// (not affected by the bug), then fetch the actual features in id-batches (a pure objectId
/// lookup, likewise not affected by the bug). Also home to the "returnCountOnly"-based
/// candidate-count helpers used to decide whether/how the workaround needs to be applied at
/// all (see <see cref="GetBoundingBoxCandidateCountAsync"/>, used by
/// <see cref="QueryStrategies.AgsQueryStrategyFactory"/>).
///
/// This class has no dependencies that require DI - it is instantiated directly by
/// <see cref="FeatureLayer"/>/<see cref="QueryStrategies.AgsQueryStrategyFactory"/> and only
/// exists to keep those readable by keeping all ArcGIS Server ids/count request building in one
/// place.
/// </summary>
internal sealed class QueryService
{
    /// <summary>
    /// Resolves the object ids matching the given (spatial or attribute) query, choosing between
    /// a single unbounded request and paged keyset resolution depending on how many candidates
    /// actually match (see <see cref="AgsQuerySettings.IdsPagingCandidateCountThreshold"/>): huge
    /// result sets (potentially millions of features for a large query geometry) benefit from
    /// paging so they don't have to be transferred/held in memory in one request, but for the
    /// common case of a comparatively small, genuinely matching result set, paging only adds
    /// unnecessary round trips. Not affected by the ArcGIS Server bbox/TOP bug either way.
    /// <para>
    /// Approach: keyset pagination via <paramref name="idFieldName"/> - each page orders by
    /// <c>{idFieldName} ASC</c> and requests at most one page worth of ids (page size = the
    /// service's <c>maxRecordCount</c>, see remarks below on why this is safe); the next page
    /// then adds <c>{idFieldName} &gt; {last id of previous page}</c> to the where clause.
    /// Paging stops only once a page comes back genuinely empty (0 ids), or once
    /// <paramref name="cap"/> ids have been collected (result flagged as truncated via
    /// <c>HasMore</c>). If <paramref name="idFieldName"/> is unknown (should not normally
    /// happen for AGS feature layers), falls back to a single unbounded request.
    /// </para>
    /// <para>
    /// Before paging is even considered, an upfront "returnCountOnly" request against the real
    /// query geometry/where-clause (not its bounding box - a plain count is not affected by the
    /// bbox/TOP bug, see <see cref="GetBoundingBoxCandidateCountAsync"/> remarks) determines the
    /// true, final match count. If it is below
    /// <see cref="AgsQuerySettings.IdsPagingCandidateCountThreshold"/>, a single unbounded
    /// "returnIdsOnly" request already returns the full, uncapped id list (see remarks below on
    /// why that reliably works), so the paging machinery is skipped entirely - saving the extra
    /// round trips paging would otherwise cost for what is, in the end, a small result set. This
    /// is exactly the same fallback path used when <paramref name="idFieldName"/> is unknown
    /// (<see cref="GetAllObjectIdsUnboundedAsync"/>). If the count itself could not be
    /// determined (unparsable response), pagination is used regardless, failing safe towards the
    /// more defensive (but more expensive) path.
    /// </para>
    /// <para>
    /// Note on why paging uses <c>resultRecordCount</c> despite the version-dependent behavior
    /// observed during testing: AGS 11.2 and 11.5 both reliably clamp the result of a
    /// <c>returnIdsOnly</c> request to (at most) the service's own <c>maxRecordCount</c> once
    /// <em>any</em> <c>resultRecordCount</c> is supplied - regardless of the exact value asked
    /// for. That clamping behavior is exactly what keyset pagination needs (a bounded page per
    /// request), so we deliberately request <c>maxRecordCount</c> as the page size instead of
    /// trying to get a larger/exact count in one call (which, per that same testing, does not
    /// work consistently - see also <see cref="GetAllObjectIdsUnboundedAsync"/>).
    /// </para>
    /// <para>
    /// Why a short page (fewer ids than <c>pageSize</c>) is deliberately NOT treated as "done":
    /// that would normally be a safe signal, backed up by <c>exceededTransferLimit=true</c> when
    /// the server clamped the page. However, older ArcGIS Server versions have been observed to
    /// never populate <c>exceededTransferLimit</c> at all (always <c>null</c>/<c>false</c>),
    /// even on a page that was silently clamped short - i.e. the very quirk this workaround
    /// exists for can resurface for <c>returnIdsOnly</c> once <c>resultRecordCount</c> is
    /// supplied. Since <c>exceededTransferLimit</c> can't be trusted to say "true" when it
    /// should, a short page can't be trusted to say "done" either. The only signal relied upon
    /// here is an actually EMPTY page (0 ids): because each page always advances the keyset by
    /// the last id actually seen, continuing regardless of a page's length can never loop
    /// forever or skip data - it just costs one extra (empty) request on the last page.
    /// </para>
    /// <para>
    /// The one remaining use of <c>exceededTransferLimit</c> is the inverse edge case: a page
    /// comes back empty (0 ids) while still flagged as <c>exceededTransferLimit=true</c>
    /// (observed as a rare AGS quirk). There is no id to derive the next <c>lastId</c> from in
    /// that case, so <c>lastId</c> is advanced by a full <paramref name="pageSize"/>-wide step
    /// instead, to skip past the current (apparently empty but "clamped") window and keep
    /// probing forward. That skip-forward path is bounded by <see cref="MaxConsecutiveEmptySkips"/>
    /// to guarantee termination even against a persistently misbehaving server.
    /// </para>
    /// <para>
    /// A second defensive guard protects against a page whose ids don't actually advance past
    /// the previous cursor (i.e. the server silently ignored the <c>{idFieldName} &gt; {lastId}</c>
    /// filter) - without it, such a server could cause the same page to be re-requested forever
    /// and/or duplicate ids to accumulate in the result. If a non-empty page's max id does not
    /// exceed the previous <c>lastId</c>, pagination stops immediately and the result is reported
    /// as truncated (<c>HasMore=true</c>) rather than risking either an infinite loop or
    /// duplicates.
    /// </para>
    /// <para>
    /// A third, time-based guard (<see cref="AgsQuerySettings.GetObjectIdsTimeoutSeconds"/>)
    /// bounds the total wall-clock time spent inside this method. The request-count-based guards
    /// above only limit how many "bad" pages can occur consecutively/in total - they do not limit
    /// how long the overall pagination may run if the server keeps responding (slowly) with many
    /// individually-valid-looking but near-empty/heavily-clamped pages. Once the timeout elapses,
    /// pagination stops immediately, returning whatever ids were collected so far with
    /// <c>HasMore=true</c>, so a misbehaving server can't stall the caller indefinitely.
    /// </para>
    /// </summary>
    private const int MaxConsecutiveEmptySkips = 50;

    /// <summary>
    /// Outcome of a single ids-page fetch within <see cref="GetObjectIdsAsync"/>, decided purely
    /// from the page's id count and its (possibly unreliable) <c>exceededTransferLimit</c> flag.
    /// </summary>
    private enum PageOutcome
    {
        /// <summary>Page had ids - collect them and query the next page.</summary>
        Continue,

        /// <summary>Page was empty and not flagged as clamped (or retries exhausted) - done.</summary>
        Done,

        /// <summary>Page was empty but flagged as clamped - skip forward and retry.</summary>
        SkipForward
    }

    public async Task<(long[] Ids, bool HasMore)> GetObjectIdsAsync(
        MapService service,
        string featuresReqUrl,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext,
        IdsWorkaroundQueryParams queryParams,
        string idFieldName,
        int cap)
    {
        if (String.IsNullOrEmpty(idFieldName))
        {
            return await GetAllObjectIdsUnboundedAsync(service, featuresReqUrl, authHandler, requestContext, queryParams, cap);
        }

        int candidateCount = await GetCandidateCountAsync(
            service, featuresReqUrl, authHandler, requestContext,
            queryParams.Geometry, queryParams.GeometrySrefId, queryParams.Where, queryParams.InSrefId,
            "getfeaturecount");

        if (candidateCount < AgsQuerySettings.IdsPagingCandidateCountThreshold)
        {
            // Few enough true matches (not just bbox candidates) that a single unbounded
            // "returnIdsOnly" request already returns everything - no need to pay for the extra
            // round trips keyset pagination would otherwise cost.
            return await GetAllObjectIdsUnboundedAsync(service, featuresReqUrl, authHandler, requestContext, queryParams, cap);
        }

        int pageSize = service.MaxRecordCount > 0
            ? service.MaxRecordCount
            : AgsQuerySettings.DefaultMaxRecordCountFallback;

        var allIds = new List<long>();
        long? lastId = null;
        int consecutiveEmptySkips = 0;
        var startedAt = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(AgsQuerySettings.GetObjectIdsTimeoutSeconds);

        while (true)
        {
            if (DateTime.UtcNow - startedAt >= timeout)
            {
                // Guards against a persistently misbehaving server that keeps responding with
                // many near-empty/clamped pages - each individually cheap/fast, but adding up to
                // an excessive total duration (the consecutiveEmptySkips/no-progress guards above
                // bound the number of such pages, not the wall-clock time they can consume).
                // Report the result as truncated rather than blocking the caller indefinitely.
                return (allIds.ToArray(), true);
            }

            string pageWhere = lastId.HasValue
                ? $"({queryParams.Where}) AND {idFieldName}>{lastId.Value}"
                : queryParams.Where;

            var requestBuilder = new GetFeaturesRequestBuilder();

            if (queryParams.Geometry != null)
            {
                requestBuilder
                    .WithGeometry(queryParams.Geometry, queryParams.GeometrySrefId)
                    .WithSpatialRelationIntersects();
            }

            requestBuilder
                .WithWhereClause(pageWhere)
                .WithTimeEpoch(queryParams.TimeEpoch)
                .WithOrderByFields($"{idFieldName} ASC")
                .WithInSpatialReferenceId(queryParams.InSrefId)
                .WithOutSpatialReferenceId(queryParams.OutSrefId)
                .WithDatumTransformation(queryParams.DatumTransformationId)
                .WithResultRecordCount(pageSize)
                .WithReturnIdsOnly(true)
                .WithReturnCountOnly(false)
                .WithReturnGeometry(false)
                .WithFormat("json");

            string idsResponseString = await requestContext.LogRequest(
                service.Server,
                service.ServiceShortname,
                requestBuilder.Build(),
                "getfeatureids",
                (requestBody) => authHandler.TryPostAsync(
                    service,
                    featuresReqUrl,
                    requestBody));

            var idsResponse = JSerializer.Deserialize<JsonFeatureIdsResponse>(idsResponseString);
            long[] pageIds = idsResponse?.ObjectIds ?? Array.Empty<long>();
            bool exceededTransferLimit = idsResponse?.ExceededTransferLimit == true;

            // Deliberately NOT deciding "done" from "pageIds.Length < pageSize": some (older)
            // ArcGIS Server versions never populate exceededTransferLimit at all, even on a page
            // that was silently clamped short - so a short page isn't a trustworthy end-of-data
            // signal on its own. Only a genuinely EMPTY page is trusted to mean "no more matches".
            PageOutcome outcome = (pageIds.Length, exceededTransferLimit) switch
            {
                (0, true) when consecutiveEmptySkips < MaxConsecutiveEmptySkips => PageOutcome.SkipForward,
                (0, _) => PageOutcome.Done,
                _ => PageOutcome.Continue
            };

            switch (outcome)
            {
                case PageOutcome.SkipForward:
                    // Server says it clamped this page, yet returned no ids to derive the next
                    // lastId from - skip forward by a full page width and keep probing.
                    consecutiveEmptySkips++;
                    lastId = (lastId ?? 0) + pageSize;
                    continue;

                case PageOutcome.Done:
                    // If exceededTransferLimit is still true here, the skip-forward retries were
                    // exhausted (MaxConsecutiveEmptySkips) without the server ever giving us a
                    // usable id to continue from - report the result as truncated (HasMore=true)
                    // rather than falsely claiming completeness. Otherwise this is a genuinely
                    // empty page: no more matches remain.
                    return (allIds.ToArray(), exceededTransferLimit);
            }

            consecutiveEmptySkips = 0;

            long newLastId = pageIds.Max();

            if (lastId.HasValue && newLastId <= lastId.Value)
            {
                // Defensive guard: the server returned a page whose max id does not advance past
                // the previous cursor, i.e. it silently ignored the "{idFieldName} > {lastId}"
                // filter (a misbehaving/non-conformant server). Continuing would just re-request
                // (a variant of) the same page forever and, since the ids can't be told apart
                // from ones already collected, would inject duplicates into the result. Stop here
                // instead and report the result as truncated.
                return (allIds.ToArray(), true);
            }

            allIds.AddRange(pageIds);

            if (allIds.Count >= cap)
            {
                return (allIds.Take(cap).ToArray(), true);
            }

            lastId = newLastId;
        }
    }

    /// <summary>
    /// Fetches all matching ids in a single, unbounded request (no <c>resultRecordCount</c> -
    /// see remarks on <see cref="GetObjectIdsAsync"/> for why that reliably returns the full,
    /// uncapped id list on the ArcGIS Server versions tested). The cap is then applied
    /// client-side. Used both as the fallback for layers without a known id field (paging is not
    /// possible without one) and, more commonly, whenever <see cref="GetObjectIdsAsync"/>'s
    /// upfront candidate count is small enough that paging isn't worth the extra round trips.
    /// Note this does not protect against very large id lists being transferred/held in memory
    /// in one go - only safe to reach when the caller has reason to believe the result is
    /// actually small (either no id field, or the candidate count says so).
    /// </summary>
    private async Task<(long[] Ids, bool HasMore)> GetAllObjectIdsUnboundedAsync(
        MapService service,
        string featuresReqUrl,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext,
        IdsWorkaroundQueryParams queryParams,
        int cap)
    {
        var requestBuilder = new GetFeaturesRequestBuilder();

        if (queryParams.Geometry != null)
        {
            requestBuilder
                .WithGeometry(queryParams.Geometry, queryParams.GeometrySrefId)
                .WithSpatialRelationIntersects();
        }

        requestBuilder
            .WithWhereClause(queryParams.Where)
            .WithTimeEpoch(queryParams.TimeEpoch)
            .WithInSpatialReferenceId(queryParams.InSrefId)
            .WithOutSpatialReferenceId(queryParams.OutSrefId)
            .WithDatumTransformation(queryParams.DatumTransformationId)
            .WithResultRecordCount(null)  // deliberately omitted - see remarks on GetObjectIdsAsync
            .WithReturnIdsOnly(true)
            .WithReturnCountOnly(false)
            .WithReturnGeometry(false)
            .WithFormat("json");

        string idsResponseString = await requestContext.LogRequest(
            service.Server,
            service.ServiceShortname,
            requestBuilder.Build(),
            "getfeatureids",
            (requestBody) => authHandler.TryPostAsync(
                service,
                featuresReqUrl,
                requestBody));

        var idsResponse = JSerializer.Deserialize<JsonFeatureIdsResponse>(idsResponseString);
        long[] ids = idsResponse?.ObjectIds ?? Array.Empty<long>();

        if (ids.Length > cap)
        {
            return (ids.Take(cap).ToArray(), true);
        }

        return (ids, false);
    }

    /// <summary>
    /// Cheap upfront check: counts how many features fall within the bounding box of the query
    /// geometry (not the geometry itself), the same query ArcGIS Server would run internally as
    /// its first, unclipped step. "returnCountOnly" is not affected by the bbox/TOP bug (see
    /// remarks on <see cref="BoundingBoxProblemAgsQueryStrategy"/>), so this always returns the
    /// true bbox-candidate count. Used by
    /// <see cref="QueryStrategies.AgsQueryStrategyFactory"/> to decide whether the
    /// <see cref="QueryStrategies.BoundingBoxProblemAgsQueryStrategy"/> is even necessary for a
    /// given query, or whether the cheaper <see cref="QueryStrategies.DefaultAgsQueryStrategy"/>
    /// already gives the same, correct result.
    /// </summary>
    public async Task<int> GetBoundingBoxCandidateCountAsync(
        MapService service,
        SpatialFilter spatialFilter,
        string featuresReqUrl,
        string where,
        int resolvedInSrefId,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext)
    {
        return await GetCandidateCountAsync(
            service, featuresReqUrl, authHandler, requestContext,
            spatialFilter.QueryShape.ShapeEnvelope, resolvedInSrefId, where, inSrefId: 0,
            logKey: "getfeaturecount_bbox");
    }

    /// <summary>
    /// Shared "returnCountOnly" request builder/issuer behind
    /// <see cref="GetBoundingBoxCandidateCountAsync"/> (counts against the query geometry's
    /// bounding box) and the upfront candidate count in <see cref="GetObjectIdsAsync"/> (counts
    /// against the real query geometry). Not affected by the bbox/TOP bug regardless of which
    /// geometry is passed in, since "returnCountOnly" always reflects the real, final match count
    /// for whatever geometry/where-clause is supplied - the bug only affects requests that return
    /// actual rows (features or ids) subject to the server's row-limit-then-clip behavior.
    /// </summary>
    private static async Task<int> GetCandidateCountAsync(
        MapService service,
        string featuresReqUrl,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext,
        Shape geometry,
        int geometrySrefId,
        string where,
        int inSrefId,
        string logKey)
    {
        var requestBuilder = new GetFeaturesRequestBuilder();

        if (geometry != null)
        {
            requestBuilder
                .WithGeometry(geometry, geometrySrefId)
                .WithSpatialRelationIntersects();
        }

        requestBuilder
            .WithWhereClause(where)
            .WithInSpatialReferenceId(inSrefId)
            .WithResultRecordCount(null)
            .WithReturnCountOnly(true)
            .WithReturnIdsOnly(true)
            .WithReturnGeometry(false)
            .WithFormat("json");

        string countResponseString = await requestContext.LogRequest(
            service.Server,
            service.ServiceShortname,
            requestBuilder.Build(),
            logKey,
            (requestBody) => authHandler.TryPostAsync(
                service,
                featuresReqUrl,
                requestBody));

        var countResponse = JSerializer.Deserialize<JsonFeatureCountResponse>(countResponseString);

        // If the response could not be parsed for whatever reason, fail safe towards the more
        // expensive but always-correct path (BoundingBoxProblem strategy / keyset pagination)
        // rather than risking the bug or a silently truncated result.
        return countResponse?.Count ?? int.MaxValue;
    }

    /// <summary>
    /// Fetches the features for the given object ids in chunks (batch size = the service's
    /// maxRecordCount, falling back to <see cref="AgsQuerySettings.DefaultMaxRecordCountFallback"/>
    /// for services that don't report one), with a bounded degree of parallelism.
    /// </summary>
    public async Task<List<JsonFeatureResponse>> GetFeaturesByObjectIdsAsync(
        MapService service,
        string featuresReqUrl,
        AgsAuthenticationHandler authHandler,
        IRequestContext requestContext,
        IdsWorkaroundQueryParams queryParams,
        IReadOnlyList<long> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        int batchSize = service.MaxRecordCount > 0
            ? service.MaxRecordCount
            : AgsQuerySettings.DefaultMaxRecordCountFallback;

        var batches = Chunk(ids, batchSize);

        using var semaphore = new SemaphoreSlim(Math.Max(1, AgsQuerySettings.MaxParallelBatchRequests));

        var batchTasks = batches.Select(async batch =>
        {
            await semaphore.WaitAsync();
            try
            {
                var requestBuilder = new GetFeaturesRequestBuilder()
                    .WithObjectIds(batch)
                    .WithOutFields(queryParams.OutFields)
                    .WithOrderByFields(queryParams.OrderByFields)
                    .WithInSpatialReferenceId(queryParams.InSrefId)
                    .WithOutSpatialReferenceId(queryParams.OutSrefId)
                    .WithDatumTransformation(queryParams.DatumTransformationId)
                    .WithReturnZ(queryParams.ReturnZ, ignoreIfFalse: true)
                    .WithReturnM(queryParams.ReturnM, ignoreIfFalse: true)
                    .WithReturnGeometry(queryParams.ReturnGeometry)
                    .WithReturnCountOnly(false)
                    .WithReturnIdsOnly(false)
                    .WithFormat("json");

                string featuresResponse = await requestContext.LogRequest(
                    service.Server,
                    service.ServiceShortname,
                    requestBuilder.Build(),
                    "getfeaturesbyids",
                    (requestBody) => authHandler.TryPostAsync(
                        service,
                        featuresReqUrl,
                        requestBody));

                return JSerializer.Deserialize<JsonFeatureResponse>(featuresResponse);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var responses = await Task.WhenAll(batchTasks);

        return responses.ToList();
    }

    private static List<long[]> Chunk(IReadOnlyList<long> ids, int size)
    {
        var chunks = new List<long[]>();

        for (int i = 0; i < ids.Count; i += size)
        {
            chunks.Add(ids.Skip(i).Take(size).ToArray());
        }

        return chunks;
    }
}
