#nullable enable

using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Json;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

public class UrlEncodedUserAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public const string UrlParameterName = "enc-username";
    public const string AuthMethodeName = "debug-enc-username";

    public UrlEncodedUserAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
                             ITracerService tracer,
                             IOptions<ApplicationSecurityConfig> appSecurityConfigOptions,
                             IEnumerable<ICustomSecretUrlParameterDecoder>? decoders = null)
    {
        if (context.User.ApplyAuthenticationMiddleware())
        {
            string? username = context.Request.Query[UrlParameterName];
            bool validUser = false;

            if (!String.IsNullOrEmpty(username))
            {
                try
                {
                    string encType = !String.IsNullOrWhiteSpace(context.Request.Query[$"{UrlParameterName}-decoder"])
                                        ? context.Request.Query[$"{UrlParameterName}-decoder"].ToString()
                                        : "default";

                    var decoder = decoders?.Where(d => d.Name == encType).FirstOrDefault();

                    if (decoder != null)
                    {
                        username = decoder.Decode(username);
                        validUser = true;
                    }
                }
                catch { }

                if (validUser)
                {
                    bool stopAuthenicationMiddlewarePropagation = !username.Contains("\\"); // if username has domain => dont stop propagagion => windows auth shoud to the rest
                    context.User = new PortalUser(username).ToClaimsPricipal(false);

                    tracer.TracePortalUser(this, context, appSecurityConfigOptions.Value);
                }
            }
        }

        await _next(context);
    }
}
