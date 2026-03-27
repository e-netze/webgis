using Api.Core.AppCode.Extensions;
using E.DataLinq.Web.Models.TokenCache;
using E.DataLinq.Web.Services.TokenCache;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebApp.Abstraction;
using E.Standard.WebApp.Services;
using E.Standard.WebGIS.Api.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Api.Core.Endpoints.Rest;

public class ResolveUrlPayload : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
#if DEBUG
        app.MapGet("rest/resolve-url-payload-debug",
            async (IDataLinqCacheTokenService tokenService) =>
            {
                return new
                {
                    token = await tokenService.CreateTokenAsync(new TokenCreateRequest()
                    {
                        DataLinqRoute = "endpoint1@query1@table1",
                        Payload = "x=1,2,3,4"
                    })
                };
            });

#endif

        app.MapPost("rest/resolve-url-payload",
            (
                [FromForm] string payload,
                SecureEndpointHandlerService endpointHandler,
                IUrlHelperService urlHelper,
                ICryptoService crypto,
                IDataLinqCacheTokenService tokenService
            ) => endpointHandler.HandlerAsync(async (ui) =>
            {
                var payloadUrl = crypto.DecryptTextDefault(payload);

                var resultUrl = payloadUrl switch
                {
                    string url when urlHelper.IsLocalDataLinqReportUrl(url) => await DataLinqCacheTokenUrl(urlHelper, tokenService, payloadUrl),
                    _ => payloadUrl,
                };

                return new
                {
                    success = true,
                    url = resultUrl
                };
            }))
           .DisableAntiforgery();


    }

    async private Task<string> DataLinqCacheTokenUrl(
        IUrlHelperService urlHelper,
        IDataLinqCacheTokenService tokenService,
        string url)
    {
        var dataLinqUrlParts = urlHelper.ToDataLinqUrlParts(url);
        var tokenResponse = await tokenService.CreateTokenAsync(
                    new TokenCreateRequest()
                    {
                        DataLinqRoute = dataLinqUrlParts.dataLinqRoute,
                        Payload = dataLinqUrlParts.payload,
                        
                    });

        return $"{dataLinqUrlParts.dataLinqUrl}?datalinqCacheToken={tokenResponse.Token}";
    }
}
