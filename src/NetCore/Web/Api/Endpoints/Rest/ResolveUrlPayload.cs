#nullable enable

using System.Threading.Tasks;

using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Extensions.Endpoints;
using Api.Core.AppCode.Services.Endpoints;

using E.DataLinq.Web.Models.TokenCache;
using E.DataLinq.Web.Services.TokenCache;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebApp.Abstraction;
using E.Standard.WebGIS.Api.Abstractions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Core.Endpoints.Rest;

public class ResolveUrlPayload : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
#if DEBUG
        app.MapGet("rest/resolve-url-payload-debug",
            async ([FromServices] IDataLinqCacheTokenService? tokenService = null) =>
            {
                return new
                {
                    token = await tokenService!.CreateTokenAsync(new TokenCreateRequest()
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
                // this is optional, because DataLinq can be excluded from api.config
                [FromServices] IDataLinqCacheTokenService? tokenService = null  // optional: set [FromServices] here because otherweise ASPNET.Core do not know its a Service...
            ) => endpointHandler.HandlerAsync(async (ui) =>
            {
                var payloadUrl = crypto.DecryptTextDefault(payload);

                var resultUrl = payloadUrl switch
                {
                    string url when urlHelper.IsLocalDataLinqReportUrl(url) => await DataLinqCacheTokenUrl(payloadUrl, urlHelper, tokenService),
                    _ => payloadUrl,
                };

                return new
                {
                    success = true,
                    url = resultUrl
                };
            }))
           .AddWebGISApiEndpointMetadata();
    }

    async private Task<string> DataLinqCacheTokenUrl(
        string url,
        IUrlHelperService urlHelper,
        IDataLinqCacheTokenService? tokenService = null
        )
    {
        if (tokenService is null) return string.Empty;

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
