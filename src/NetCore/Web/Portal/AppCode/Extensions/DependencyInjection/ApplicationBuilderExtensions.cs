using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Json;
using Microsoft.AspNetCore.Builder;
using Portal.Core.AppCode.Middleware;
using Portal.Core.AppCode.Middleware.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Core.AppCode.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder UseWebgisCookieAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<WebgisCookieAuthenticationMiddleware>();

        return builder;
    }

    //static public IApplicationBuilder UseSubscriberUrlCredentialsAuthenticationMiddleware(this IApplicationBuilder builder)
    //{
    //    builder.UseMiddleware<SubscriberUrlCredentialsAuthenticationMiddleware>();

    //    return builder;
    //}

    [Obsolete("This schould be done with default AspNetCore Authentication Middleware and dan [Authorize] Method => AuthController.LoginAD")]
    static public IApplicationBuilder UseWindowsAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<WindowsAuthenticationMiddleware>();

        return builder;
    }

    static public IApplicationBuilder UseWebgisAuthorizationMiddleware(this IApplicationBuilder builder,
                                                                       ApplicationSecurityConfig applicationSecurityConfig,
                                                                       ConfigurationService config,
                                                                       IEnumerable<ICustomPortalAuthenticationMiddlewareService> customAuthentications)
    {
        if (applicationSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection ||
            applicationSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.AzureAD)
        {
            builder.UseAuthentication();
            builder.UseAuthorization();

            // Subscriber login auf Portal, MapBuilder, ... (deprecated)
            //builder.UseSubscriberUrlCredentialsAuthenticationMiddleware();

            builder.UseWebgisCookieAuthenticationMiddleware();
            builder.UseMiddleware<OidcPrefixAuthenticationMiddleware>();
        }
        else
        {
            var allowedMethods = config.AllowedSecurityMethods();

            if (allowedMethods.Contains(WindowsAuthenticationMiddleware.AuthMethodeName))
            {
                // AspNetCore Authentication für Windows Authentication notwendig!!
                // Eigentliche Authenfizierung erfolt über AuthCOntroller.LoginAD()

                builder.UseAuthentication();
                builder.UseAuthorization();
            }

            // Subscriber login auf Portal, MapBuilder, ...  (deprecated)
            //builder.UseSubscriberUrlCredentialsAuthenticationMiddleware();

            // run bevore Windowsauth => enc_username for old ENI Homepage
            //                        => enc_username for debugging authorization        
            if (/*config.HasCompatibilitiesConfig()*/allowedMethods.Contains(UrlEncodedUserAuthenticationMiddleware.AuthMethodeName))
            {
                builder.UseMiddleware<UrlEncodedUserAuthenticationMiddleware>();
            }

            builder.UseWebgisCookieAuthenticationMiddleware();

            // Nach Cookie sollte windows Auth greifen
            // Ansonsten geht die Authentifizierung nur über AuthController.LoginAD
            // Windowns Auth sollte aber bei jedem Aufruf überprüft werden (falls nicht schon cookie gesetzt wurde), weil sonst direkte Aufrufe von Karten/Portal nicht funktionieren.
            // Ohne Middleware geht das nür über AuthController.LoginAD (umleitung über Startseite..)
            if (allowedMethods.Contains(WindowsAuthenticationMiddleware.AuthMethodeName))
            {
                builder.UseMiddleware<WindowsAuthenticationMiddleware>();
            }

            if (config.UseHeaderAuthentication())
            {
                builder.UseMiddleware<HeaderAuthenicationMiddleware>();
            }
        }

        if (customAuthentications != null && customAuthentications.Count() > 0)
        {
            builder.UseMiddleware<CustomAuthenticationMiddleware>();
        }

        return builder;
    }

    static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

        return appBuilder;
    }
}