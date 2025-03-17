using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Configuration.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebApp.Abstraction;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Api.Core.Endpoints.TileCache;

public class Redirect : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/tilecache/redirect/{serviceId}/{encryptedUrl}/{level}/{row}/{col}",
            async (
                string serviceId,
                string encryptedUrl,
                string level,
                string row,
                string col,
                HttpContext context,
                [FromServices] ICryptoService crypto,
                [FromServices] CacheService cache,
                [FromServices] IUrlHelperService urlHelper,
                [FromServices] ConfigurationService config,
                [FromServices] IRequestContext requestContext) =>
            {
                var referer = context.Request.Headers.Referer.ToString().ToLowerInvariant();
                // todo: check allowed referers
                var etag = GenerateETag(Encoding.UTF8.GetBytes(encryptedUrl));

                if (context.Request.Headers.ContainsKey("If-None-Match")
                    && context.Request.Headers["If-None-Match"] == etag)
                {
                    context.Response.StatusCode = 304;
                    return;
                }

                var allowedReferers = config.AllowedSecuredTilesRedirectReferers();
                if (allowedReferers is not null
                    && allowedReferers.Length > 0
                    && allowedReferers.Any(allowedReferer => referer.Contains(allowedReferer)) == false)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                var url = crypto.StaticDefaultDecrypt(encryptedUrl)
                                .Replace("[LEVEL]", level)
                                .Replace("[ROW]", row)
                                .Replace("[COL]", col);

                var serviceDownload = await cache.GetOriginalService(serviceId, null, urlHelper) as IServiceSecuredDownload;

                var fileBytes = serviceDownload is not null
                    ? await serviceDownload.GetSecuredData(requestContext, url)
                    : await requestContext.Http.GetDataAsync(url);

                var lastModified = DateTime.UtcNow;
                context.Response.Headers["Cache-Control"] = "public, max-age=604800"; // cache image for 7 days
                context.Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
                context.Response.Headers["ETag"] = etag;
                context.Response.ContentType = "image/png"; // Change the type based on your image format
                await context.Response.Body.WriteAsync(fileBytes);
            });
    }

    string GenerateETag(byte[] fileBytes)
    {
        // Erzeuge ein ETag, indem du den MD5-Hash des Dateiinhalts berechnest
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(fileBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
