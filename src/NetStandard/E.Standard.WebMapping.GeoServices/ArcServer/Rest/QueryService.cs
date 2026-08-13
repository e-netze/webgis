using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
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
/// lookup, likewise not affected by the bug).
///
/// This class has no dependencies that require DI - it is instantiated directly by
/// <see cref="FeatureLayer"/> and only exists to keep <see cref="FeatureLayer"/> readable.
/// </summary>
internal sealed class QueryService
{
    /// <summary>
    /// Resolves the object ids matching the given (spatial or attribute) query in pages, so
    /// that huge result sets (potentially millions of features for a large query geometry)
    /// don't have to be transferred/held in memory in one unbounded request. Not affected by
    /// the ArcGIS Server bbox/TOP bug.
    /// <para>
    /// Approach: keyset pagination via <paramref name="idFieldName"/> - each page orders by
    /// <c>{idFieldName} ASC</c> and requests at most one page worth of ids (page size = the
    /// service's <c>maxRecordCount</c>, see remarks below on why this is safe); the next page
    /// then adds <c>{idFieldName} &gt; {last id of previous page}</c> to the where clause.
    /// Paging stops once a page returns fewer ids than the page size (no more matches) or once
    /// <paramref name="cap"/> ids have been collected (result flagged as truncated via
    /// <c>HasMore</c>). If <paramref name="idFieldName"/> is unknown (should not normally
    /// happen for AGS feature layers), falls back to a single unbounded request.
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
    /// </summary>
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

        int pageSize = service.MaxRecordCount > 0
            ? service.MaxRecordCount
            : AgsQuerySettings.DefaultMaxRecordCountFallback;

        var allIds = new List<long>();
        long? lastId = null;

        while (true)
        {
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

            if (pageIds.Length == 0)
            {
                return (allIds.ToArray(), false);
            }

            allIds.AddRange(pageIds);

            if (allIds.Count >= cap)
            {
                return (allIds.Take(cap).ToArray(), true);
            }

            if (pageIds.Length < pageSize)
            {
                // Fewer ids than the requested page size means no further matches remain.
                return (allIds.ToArray(), false);
            }

            lastId = pageIds.Max();
        }
    }

    /// <summary>
    /// Fallback for layers without a known id field: fetches all matching ids in a single,
    /// unbounded request (no <c>resultRecordCount</c> - see remarks on <see cref="GetObjectIdsAsync"/>
    /// for why that reliably returns the full, uncapped id list on the ArcGIS Server versions
    /// tested). The cap is then applied client-side. Note this does not protect against very
    /// large id lists being transferred/held in memory in one go - kept only as a safety net
    /// for the rare case that a feature layer has no id field.
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
