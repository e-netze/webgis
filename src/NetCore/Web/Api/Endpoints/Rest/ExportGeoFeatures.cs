using System;
using System.Threading.Tasks;

using Api.Core.AppCode;
using Api.Core.AppCode.Extensions.Endpoints;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Endpoints;

using E.Standard.Api.App;
using E.Standard.Api.App.DTOs.ApiResult;
using E.Standard.Api.App.DTOs.Print;
using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Services;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebApp.Abstraction;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Core.Endpoints.Rest;

public class ExportGeoFeatures : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("rest/exportfeatures",
            (
                [FromForm] string serviceId,
                [FromForm] string queryId,
                [FromForm] string featureIds,
                [FromForm] string queryFeatures,
                [FromForm] string format,
                [FromForm] bool? forceDownload,
                [FromServices] SecureEndpointHandlerService endpointHandler,
                [FromServices] ExportGeoFeaturesService exportService,
                [FromServices] ConfigurationService config,
                [FromServices] UrlHelperService urlHelper,
                [FromServices] ICryptoService crypto
            ) => endpointHandler.HandlerAsync(async (ui) =>
            {
                var export = !String.IsNullOrEmpty(queryFeatures)
                    ? exportService.ExportFeatures(JSerializer.Deserialize<QueryFeaturesDTO>(queryFeatures), format)
                    : await exportService.QueryFeaturesAndExport(ui, serviceId, queryId, featureIds, format);

                return await exportService.ExportType(ui, serviceId, queryId, format) switch
                {
                    GeoFeatureExportType.Clipboard when forceDownload != true 
                      => await endpointHandler.ApiJsonResult(ClipboardResult(export.name, export.data, export.descrtiption)),
                    _ => await endpointHandler.ApiJsonResult(await DownloadResult(export.name, export.data, urlHelper, config, crypto))
                };
            })
        ).AddWebGISApiEndpointMetadata();
    }

    #region Helper

    private object ClipboardResult(string name, string data, string description)
        => new DownloadDTO()
        {
            ClipboardData = data,
            Name = name,
            Description = description
        };

    async private Task<object> DownloadResult(string name, string data,
            UrlHelperService urlHelper,
            ConfigurationService config,
            ICryptoService crypto)
    {
        string fileTitle = $"{Guid.NewGuid():N}";
        string fileName = $"{fileTitle}{ApiGlobals.DownloadFileExtension}";
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(urlHelper.OutputPath(), fileName).ToPlatformPath(), data, config.DefaultTextDownloadEncoding());

        return new DownloadDTO()
        {
            EncryptedFilename = crypto.EncryptTextDefault(fileName, CryptoResultStringType.Hex),
            Name = name
        };
    }

    #endregion
}
