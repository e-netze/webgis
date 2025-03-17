using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Security.App.Services;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class BasicAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public BasicAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext,
                                  RoutingEndPointReflectionService endpointReflection,
                                  BotDetectionService botDetection,
                                  SubscriberDatabaseService subscriberDb)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, ApiAuthenticationTypes.BasicAuthentication))
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();

            if (authHeader.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
            {
                var authCode =
                    System.Text.Encoding.ASCII.GetString(
                        Convert.FromBase64String(authHeader.Substring("basic ".Length)));

                var pos = authCode.IndexOf(":");

                if (pos > 0)
                {
                    var name = authCode.Substring(0, pos);
                    var password = authCode.Substring(pos + 1);

                    var db = subscriberDb.CreateInstance();
                    var subscriber = db.GetSubscriberByName(name);

                    if (subscriber != null)
                    {
                        if (botDetection.IsSuspiciousUser(subscriber.FullName))
                        {
                            await botDetection.BlockSuspicousUserAsync(subscriber.FullName);
                        }
                        if (subscriber.VerifyPassword(password))
                        {
                            botDetection.RemoveSuspiciousUser(subscriber.FullName);

                            var ui = new CmsDocument.UserIdentification(subscriber.FullName, null, null, null);
                            httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.BasicAuthentication);
                        }
                        else
                        {
                            botDetection.AddSuspiciousUser(subscriber.FullName);
                        }
                    }
                }
            }
        }

        await _next(httpContext);
    }
}
