using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
                             SubscriberDatabaseService subscriberDbService,
                             IEnumerable<ICustomApiSubscriberClientnameService> customSubscriberClientnameServices = null)
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

                string usernamePrefix = String.Empty, clientName = client.ClientName;

                var customScriberClientname = customSubscriberClientnameServices.GetCustomClientname(client);
                if (customScriberClientname != null)
                {
                    usernamePrefix = customScriberClientname.UsernamePrefix;
                    clientName = customScriberClientname.ClientName;
                }
                else
                {
                    var subscriber = subscriberDb.GetSubscriberById(client.Subscriber);
                    if (subscriber == null)
                    {
                        throw new Exception("Unknown Subscriber");
                    }

                    usernamePrefix = usernamePrefix = $"{subscriber.FullName}@";
                }

                var ui = new CmsDocument.UserIdentification($"{usernamePrefix}{clientName}", client.Roles?.ToArray(), null, null);
                httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.ClientIdAndSecret);
            }
        }

        await _next(httpContext);
    }
}
