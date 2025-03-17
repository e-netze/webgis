using E.Standard.Custom.Core.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

//[Obsolete("This schould be done with default AspNetCore Authentication Middleware and dan [Authorize] Method => AuthController.LoginAD")]
public class WindowsAuthenticationMiddleware
{
    public const string AuthMethodeName = "windows";

    private readonly RequestDelegate _next;

    public WindowsAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
                             WebgisCookieService webgisCookieService,
                             IEnumerable<IPortalAuthenticationService> _authenticationServices,
                             ITracerService tracer)
    {
        if (context.User.ApplyAuthenticationMiddleware() /*&& context.Request.HttpContext?.User?.Identity is WindowsIdentity*/)
        {
            string loginUser = context.Request.HttpContext.User.Identity.Name;

            if (!String.IsNullOrEmpty(loginUser))
            {
                var windowsUser = await _authenticationServices.GetService(UserType.WindowsUser)?
                                                               .TryAuthenticationServiceUser(context, loginUser, true);

                Console.WriteLine($"{String.Join(", ", windowsUser.UserRoles)}");

                if (windowsUser != null)
                {
                    webgisCookieService.SetAuthCookie(context, true, windowsUser.Username, E.Standard.WebGIS.Core.UserType.WindowsUser);

                    context.User = new PortalUser(windowsUser.Username,
                                                  windowsUser.UserRoles,
                                                  null).ToClaimsPricipal();

                    tracer.TracePortalUser(this, context);
                }
            }
        }

        await _next.Invoke(context);
    }
}
