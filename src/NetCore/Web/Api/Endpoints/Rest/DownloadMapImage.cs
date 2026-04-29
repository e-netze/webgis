//using Api.Core.AppCode.Services;
//using Api.Core.AppCode.Services.Endpoints;
//using Api.Core.AppCode.Services.Rest;

//using E.Standard.WebApp.Abstraction;

//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Routing;

//namespace Api.Core.Endpoints.Rest;

//public class DownloadMapImage : IApiEndpoint
//{
//    public void Register(IEndpointRouteBuilder app) => 
//        app.MapPost("rest/downloadmapimage", (
//            [FromServices] SecureEndpointHandlerService endpointHandler,
//            [FromServices] RestServiceFactory restService,
//            [FromServices] ApiLoggingService pLog
//            ) => endpointHandler.HandlerAsync(ui) =>
//        {
//            using (var pLog = _apiLogging.UsagePerformaceLogger(this, $"downloadmapimage", null, ui))
//            {
//                var actionResult = await restService.Print.PerformDownloadMapImageAsync(this, ui);
//            }
//        });
//}
