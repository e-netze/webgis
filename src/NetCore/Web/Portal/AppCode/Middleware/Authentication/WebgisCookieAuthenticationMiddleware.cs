using E.Standard.Configuration.Services;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using System;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

public class WebgisCookieAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _useWindowsAuthentication;
    private readonly bool _allowEncUsername;
    private readonly ApplicationSecurityConfig _appSecurityConfig;

    public WebgisCookieAuthenticationMiddleware(RequestDelegate next,
                                                ConfigurationService config,
                                                IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig)
    {
        _next = next;
        _useWindowsAuthentication = config.AllowedSecurityMethods().Contains("windows");
        _allowEncUsername = config.AllowedSecurityMethods().Contains(UrlEncodedUserAuthenticationMiddleware.AuthMethodeName);
        _appSecurityConfig = appSecurityConfig.CurrentValue;
    }

    public async Task Invoke(HttpContext context,
                             WebgisCookieService webgisCookieService,
                             UrlHelperService urlHelper,
                             IOptions<ApplicationSecurityConfig> appSecurityConfigOptions,
                             ITracerService tracer)
    {
        if (context.User.ApplyAuthenticationMiddleware())
        {
            var portalUser = await webgisCookieService.TryGetCookieUser(context);
            if (portalUser != null)
            {
                context.User = portalUser.ToClaimsPricipal();

                tracer.TracePortalUser(this, context, appSecurityConfigOptions.Value);
            }
            else if (_useWindowsAuthentication && context.User.Identity is WindowsIdentity)
            {
                // do nothing => WindowsAuthentictionMiddleware will do the authentication and sets cookie
            }
            else if (_appSecurityConfig.UseOpenIdConnect())
            {
                // do nothing => AuthorizationMiddleware will do the authentication and sets cookie
            }
            else if (_appSecurityConfig.UseAzureAD())
            {
                // do nothing => AuthorizationMiddleware will do the authentication and sets cookie
            }
            else if (_allowEncUsername && !String.IsNullOrEmpty(context.Request.Query[UrlEncodedUserAuthenticationMiddleware.UrlParameterName]))
            {
                // to nothing 
            }
            else if (/*!String.IsNullOrEmpty(context.User.GetUsername()) &&*/ !urlHelper.TargetsAuthorizedEndPoint(context))
            {
                // 
                // Falls das Zeil kein Authorization Einpunkt ist, wird der User für das Portal auf "anonym" gesezt
                // => Bis zur Target Action soll eine Überprüfung mehr gemacht werden. 
                //    Die Authenfizierung erledigt die UseAuthentication() Middleware von AspNet Core.
                //    In der Target Action muss dann auch das Cookie gesetzt werden.
                //
                // Sollte eigentlich nur bei Windows Authentication auftreten wo das Cookie über AuthController.LoginAD erfolgt.
                // Wenn AuthController.LoginAD => sollte hier nicht aufgerufen werden wenn AuthorizedEnpoint => Windows User wird weiter übergeben.
                // Sonst sollte der User aus der Windowsauthentication ignorert und auf Anonym gesetzt werden.
                //

                context.User = PortalUser.Anonymous.ToClaimsPricipal(false);  // false => Sollte weitere Middleware noch anspringen auf den Request?
            }
        }

        await _next.Invoke(context);
    }
}
