using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class PortalProxyRequestAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public PortalProxyRequestAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    async public Task Invoke(HttpContext httpContext,
                             RoutingEndPointReflectionService endpointReflection,
                             ICryptoService crypto)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, ApiAuthenticationTypes.PortalProxyRequest))
        {
            //
            // entsprichte dem früheren "allowEncryptedUiParameter" Parameter beim GetHMACUser();
            //

            NameValueCollection nvc = httpContext.Request.FormAndQueryParameters();

            if (!String.IsNullOrWhiteSpace(nvc["__ui"]))  // for webgis5 proxy requests
            {
                string userName = nvc["__ui"];
                userName = crypto.DecryptText(userName,
                    (int)CustomPasswords.PortalProxyRequests,
                    CryptoStrength.AES256);

                string[] keyInfos = userName.Split('|');
                string privateKey = keyInfos[0];
                string username = keyInfos.Length > 0 ? keyInfos[0] : String.Empty;
                string[] userroles = null;
                string task = nvc["__ft"];

                if (keyInfos.Length > 1)  // Groupnames
                {
                    userroles = new string[keyInfos.Length - 1];
                    Array.Copy(keyInfos, 1, userroles, 0, keyInfos.Length - 1);
                }

                if (!String.IsNullOrWhiteSpace(username))
                {
                    var ui = new CmsDocument.UserIdentification(username, userroles, null, null, task: task);
                    httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.PortalProxyRequest);
                }
            }
        }

        await _next(httpContext);
    }
}
