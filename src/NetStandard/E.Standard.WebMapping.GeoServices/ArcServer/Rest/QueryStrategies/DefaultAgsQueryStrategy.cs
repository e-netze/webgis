using System;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.GeoServices.ArcServer;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Features;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Extensions;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.QueryStrategies;

/// <summary>
/// Plain, "textbook" ArcGIS Server pagination, used for services that are not known to be
/// affected by the spatial-query bbox/TOP bug (see <see cref="BoundingBoxProblemAgsQueryStrategy"/>
/// for background). Repeats the ordinary feature query (with geometry, if any) using
/// <c>resultRecordCount</c>/<c>resultOffset</c> paging until either a page comes back short
/// (and the server does not report <c>exceededTransferLimit</c> on it) or
/// <see cref="AgsQuerySettings.MaxSpatialQueryResultCap"/> is reached - whichever happens
/// first. Each page is appended directly (no separate ids-resolution round trip), so this is
/// noticeably cheaper than <see cref="BoundingBoxProblemAgsQueryStrategy"/> and is therefore
/// the default (see <see cref="AgsQueryStrategy.Default"/>).
/// <para>
/// Guard: the number of round trips is bounded to roughly <c>cap / pageSize</c> (see
/// <c>maxRequests</c> below). If that many requests weren't enough to either exhaust the
/// result or reach the cap, the pages must have been consistently short/near-empty - the same
/// symptom the bbox/TOP bug produces (many bbox candidates get clipped away, only a few
/// genuine matches per page). Rather than looping hundreds of times chasing the cap, this
/// aborts early and reports the result as truncated (<c>HasMore=true</c>); an administrator can
/// then opt this service into <see cref="AgsQueryStrategy.BoundingBoxProblem"/> if the service
/// is indeed affected.
/// </para>
/// </summary>
internal sealed class DefaultAgsQueryStrategy : IAgsQueryStrategy
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
        int pageSize = service.MaxRecordCount > 0
            ? service.MaxRecordCount
            : AgsQuerySettings.DefaultMaxRecordCountFallback;
        int cap = AgsQuerySettings.MaxSpatialQueryResultCap;

        string outFields = String.IsNullOrWhiteSpace(filter.SubFields)
            ? "*"
            : filter.SubFields.Replace(" ", ",");

        // resultOffset paging needs a stable order across requests - fall back to ordering by
        // the id field if the caller did not ask for a specific order.
        string orderByFields = String.IsNullOrWhiteSpace(filter.OrderBy)
            ? (String.IsNullOrEmpty(layer.IdFieldName) ? null : $"{layer.IdFieldName} ASC")
            : filter.OrderBy;

        int datumTransformationId = service.DatumTransformations?.FirstOrDefault() ?? 0;

        int resultOffset = 0;
        int totalCount = 0;
        bool hasMore = false;
        int requestCount = 0;

        // Upper bound on the number of round trips: if the pages were consistently full (the
        // "textbook" case this strategy assumes), this many requests would already be enough to
        // reach the cap. If they are not - i.e. we keep getting mostly short/near-empty pages
        // and still haven't reached the cap after this many requests - that is itself a strong
        // signal that this service is actually affected by the bbox/TOP bug (see
        // BoundingBoxProblemAgsQueryStrategy) despite being configured for the Default strategy.
        // Rather than looping potentially hundreds of times to eventually reach (or fall short
        // of) the cap, abort early and report the result as truncated; an administrator can then
        // opt this service into AgsQueryStrategy.BoundingBoxProblem in the CMS if desired.
        int maxRequests = (int)Math.Ceiling((double)cap / pageSize) + 1;

        while (true)
        {
            if (requestCount >= maxRequests)
            {
                hasMore = true;
                break;
            }

            requestCount++;

            var requestBuilder = new GetFeaturesRequestBuilder();

            if (spatialFilter != null)
            {
                requestBuilder
                    .WithGeometry(spatialFilter.QueryShape, spatialFilter.FilterSpatialReference?.Id ?? 0)
                    .WithSpatialRelationIntersects();
            }

            requestBuilder
                .WithOutFields(outFields)
                .WithWhereClause(where)
                .WithTimeEpoch(filter.TimeEpoch)
                .WithOrderByFields(orderByFields)
                .WithResultRecordCount(pageSize)
                .WithResultOffset(resultOffset)
                .WithInSpatialReferenceId(resolvedInSrefId)
                .WithOutSpatialReferenceId(resolvedOutSrefId)
                .WithDatumTransformation(datumTransformationId)
                .WithReturnZ(layer.HasZ, ignoreIfFalse: true)
                .WithReturnM(layer.HasM, ignoreIfFalse: true)
                .WithReturnGeometry(filter.QueryGeometry)
                .WithReturnCountOnly(false)
                .WithReturnIdsOnly(false)
                .WithFormat("json");

            string featuresResponse = await requestContext.LogRequest(
                service.Server,
                service.ServiceShortname,
                requestBuilder.Build(),
                "getfeatures",
                (requestBody) => authHandler.TryPostAsync(
                    service,
                    featuresReqUrl,
                    requestBody));

            var jsonFeatureResponse = JSerializer.Deserialize<JsonFeatureResponse>(featuresResponse);
            var pageFeatures = jsonFeatureResponse.Features ?? Array.Empty<JsonFeature>();
            int pageCount = pageFeatures.Length;
            bool exceededTransferLimit = jsonFeatureResponse.ExceededTransferLimit;

            if (pageCount == 0)
            {
                // A genuinely empty page - but if the server still reports exceededTransferLimit
                // (observed as a rare quirk, akin to the bbox/TOP bug), be conservative and flag
                // the result as possibly truncated rather than claiming completeness.
                hasMore = exceededTransferLimit;
                break;
            }

            if (totalCount + pageCount > cap)
            {
                // Cap reached mid-page: truncate this page's features before appending so the
                // total never exceeds the cap, and flag the result as truncated.
                jsonFeatureResponse.Features = pageFeatures.Take(cap - totalCount).ToArray();
                layer.AppendJsonFeaturesTo(jsonFeatureResponse, features, filter);
                totalCount = cap;
                hasMore = true;
                break;
            }

            layer.AppendJsonFeaturesTo(jsonFeatureResponse, features, filter);
            totalCount += pageCount;

            if (pageCount < pageSize && !exceededTransferLimit)
            {
                // Fewer features than the requested page size, and the server did not report
                // clamping: no further matches remain.
                break;
            }

            resultOffset += pageCount;
        }

        features.Query = filter;
        features.Layer = layer;
        features.HasMore = hasMore;

        return -1;
    }
}
