using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class ClientIdAndSecretAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ClientIdAndSecretAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    async public Task Invoke(HttpContext httpContext,
                             RoutingEndPointReflectionService endpointReflection,
                             SubscriberDatabaseService subscriberDbService)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, ApiAuthenticationTypes.ClientIdAndSecret))
        {

            //
            // entsprichte dem früheren "allowClientIdAndSecret" Parameter beim GetHMACUser();
            //

            NameValueCollection nvc = httpContext.Request.FormAndQueryParameters();

            if (!String.IsNullOrWhiteSpace(nvc["client_id"]) && !String.IsNullOrWhiteSpace(nvc["client_sec"]))
            {
                var subscriberDb = subscriberDbService.CreateInstance();

                var client = subscriberDb.GetClientByClientId(nvc["client_id"]);
                if (client == null)
                {
                    throw new Exception("Unknown cliend_id");
                }

                if (client.IsClientSecret(nvc["client_sec"]) == false)
                {
                    throw new Exception("Wrong client secret");
                }

                var subscriber = subscriberDb.GetSubscriberById(client.Subscriber);
                if (subscriber == null)
                {
                    throw new Exception("Unknown Subscriber");
                }

                var ui = new CmsDocument.UserIdentification(subscriber.FullName + "@" + client.ClientName, null, null, null);
                httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.ClientIdAndSecret);
            }
        }

        await _next(httpContext);
    }
}
