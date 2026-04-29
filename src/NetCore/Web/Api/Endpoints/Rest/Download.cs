using System;
using System.IO;
using System.Threading.Tasks;

using Api.Core.AppCode.Extensions.Endpoints;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Endpoints;

using E.Standard.Api.App;
using E.Standard.Extensions.Text;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebApp.Abstraction;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Core.Endpoints.Rest;

public class Download : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("rest/download/{id}",
            (
                [FromServices] SecureEndpointHandlerService endpointHandler,
                [FromServices] ICryptoService crypto,
                [FromServices] UrlHelperService urlHelper,
                [FromServices] IHttpService http,
                string id,
                string n = "",
                string contentType = "application/octet-stream"
           ) => PerformDownload(endpointHandler, crypto, urlHelper, http, id, n, contentType)
        ).AddWebGISApiEndpointMetadata();

        app.MapGet("rest/download",
            (
                [FromServices] SecureEndpointHandlerService endpointHandler,
                [FromServices] ICryptoService crypto,
                [FromServices] UrlHelperService urlHelper,
                [FromServices] IHttpService http,
                string id,
                string n = "",
                string contentType = "application/octet-stream"
             ) => PerformDownload(endpointHandler, crypto, urlHelper, http, id, n, contentType)
        ).AddWebGISApiEndpointMetadata();
    }

    private Task<IResult> PerformDownload(
                [FromServices] SecureEndpointHandlerService endpointHandler,
                [FromServices] ICryptoService crypto,
                [FromServices] UrlHelperService urlHelper,
                [FromServices] IHttpService http,
                string id,
                string n,
                string contentType
        ) => endpointHandler.HandlerAsync(async (ui) =>
    {
        string fileName = crypto.DecryptTextDefault(id);

        if (fileName.Contains("/") || fileName.Contains(@"\"))
        {
            // no folders allowed... also ./../ is stricktly forbidden
            throw new IOException("Not allowed");
        }

        string filePath = urlHelper.OutputPath().AddUriPath(fileName);

        string clientFileName = System.IO.Path.GetExtension(fileName).ToLower() switch
        {
            ".pdf" when n.StartsWith(ApiGlobals.PrintOutputPrefix) 
                   => $"webgis-map_{DateTime.Now.ToShortDateString()}_{DateTime.Now.ToLongTimeString()}.pdf",
            ".zip" when n.StartsWith(ApiGlobals.PrintOutputPrefix) 
                   => $"webgis-map_{DateTime.Now.ToShortDateString()}_{DateTime.Now.ToLongTimeString()}.zip",
            //".zip" => n,
            ApiGlobals.DownloadFileExtension => n,
            _ => throw new Exception("Forbiden file extension")
        };

        //if (n?.StartsWith("print_", StringComparison.OrdinalIgnoreCase) == false)  // Ausdruck kann über die Druckvorschau öfter gedruckt werden.
        //{
        //    filePath.TryDelete();
        //}

        var data = await filePath.BytesFromUri(http); 
        return endpointHandler.ApiRawResponse(data.ToArray(), contentType, clientFileName);
    });
}
