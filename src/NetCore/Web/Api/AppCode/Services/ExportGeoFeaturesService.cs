#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services.Rest;

using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.DTOs.Print;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.CMS.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;

namespace Api.Core.AppCode.Services;

internal class ExportGeoFeaturesService
{
    private readonly IRequestContext _requestContext;
    private readonly CacheService _cache;
    private readonly UrlHelperService _urlHelper;
    private readonly RestServiceFactory _restService;

    

    public ExportGeoFeaturesService(IRequestContext requestContext, CacheService cache, UrlHelperService urlHelper, RestServiceFactory restService)
        => (_requestContext, _cache, _urlHelper, _restService) = (requestContext, cache, urlHelper, restService);

    async public Task<(string name, string data, string descrtiption)> QueryFeaturesAndExport(
            CmsDocument.UserIdentification ui, 
            string serviceId, 
            string queryId, 
            string featureIds, 
            string format)
    {
        var query = (await _cache.GetQuery(serviceId, queryId, ui, urlHelper: _urlHelper))
                        .ThrowIfNull(() => "Query not foound");

        var oids = featureIds.Split(',').Select(id => long.Parse(id)).ToArray();
        var filter = new E.Standard.WebMapping.Core.Api.Bridge.ApiOidsFilter(oids);
        filter.QueryGeometry = false;

        var engine = new QueryEngine();
        (await engine.PerformAsync(_requestContext, query, filter, advancedQueryMethod: QueryEngine.AdvancedQueryMethod.Normal))
            .ThrowIfFalse(() => $"Can't query service {serviceId}/{queryId}");

        var tableExportFormat = query.TableExportFormats?
                                                 .Where(f => f.Id == format)
                                                 .FirstOrDefault();

        FeatureCollection features = await _restService.Helper.PrepareFeatureCollection(engine.Features, query, null, ui, null, renderFields: tableExportFormat == null);
        features.OrderByIds(oids);

        return format switch
        {
            "_csv" => ($"{query.Name}.csv", features.ToCsv(excel: false), ""),
            "_csv_excel" => ($"{query.Name}.csv", features.ToCsv(excel: true), ""),
            _ when tableExportFormat is not null =>
                ($"{tableExportFormat.Name}.{tableExportFormat.FileExtension}", features.ToPattern(tableExportFormat.FormatString), tableExportFormat.Description),
            _ => throw new Exception($"Unknown feature export format: {format}")
        };
    }

    public (string name, string data, string descrtiption) ExportFeatures(
        QueryFeaturesDTO features,
        string format)
    {
        return format switch
        {
            "_csv" => ($"table.csv", features.ToCsv(excel: false), ""),
            "_csv_excel" => ($"table.csv", features.ToCsv(excel: true), ""),
            _ => throw new Exception($"Unknown feature export format: {format}")
        };
    }

    async public ValueTask<GeoFeatureExportType> ExportType(
        CmsDocument.UserIdentification ui, 
        string serviceId, 
        string queryId, 
        string format)
    {
        if (String.IsNullOrEmpty(serviceId) || String.IsNullOrEmpty(queryId))
        {
            return GeoFeatureExportType.Download;
        }

        var query = (await _cache.GetQuery(serviceId, queryId, ui, urlHelper: _urlHelper))
                        .ThrowIfNull(() => "Query not foound");

        var tableExportFormat = query.TableExportFormats?
                                                .Where(f => f.Id == format)
                                                .FirstOrDefault();

        if (tableExportFormat == null)
        {
            return GeoFeatureExportType.Download;
        }

        return GeoFeatureExportType.Clipboard;
    }
}
