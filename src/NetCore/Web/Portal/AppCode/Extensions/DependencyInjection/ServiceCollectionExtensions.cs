using E.Standard.Configuration.Extensions;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddEssentialWebgisPortalServices(this IServiceCollection services)
    {
        services.AddTransient<IAppEnvironment, AppEnvironment>();
        services.AddTransient<UrlHelperService>();
        services.AddTransient<WebgisApiService>();
        services.AddTransient<CustomContentService>();
        services.AddTransient<HmacService>();

        services.AddScoped<UploadFilesService>();
        services.AddSingleton<InMemoryPortalAppCache>();

        services.AddTransient<ProxyService>();

        services.AddTransient<ViewerLayoutService>();

        services.AddHttpContextAccessor();
        services.AddScoped<RoutingEndPointReflectionService>();  // Scoped => eine Instanz pro Http Request!!

        return services;
    }

    static public IServiceCollection AddWebgisPortalAuthenticationServices(this IServiceCollection services,
                                                                           IConfiguration configuration)
    {
        services.AddApplicationSecurityConfiguration();

        var securityConfig = new ApplicationSecurityConfig().LoadFromJsonFile();

        if (securityConfig?.UseOpenIdConnect() == true)
        {
            #region OpenIdConnect Services
            //Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", options =>
                {
                    options.Cookie.Name = "maps-auth-identity";
                    options.SlidingExpiration = true;

                    options.Events.OnSigningIn = (context) =>
                    {
                        // Make it IsPermanent
                        context.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(365);
                        context.CookieOptions.Secure = true;
                        context.CookieOptions.HttpOnly = true;
                        // iOS Bug => sonst gibts beim laden einen "infinite loop"
                        context.CookieOptions.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;

                        return Task.CompletedTask;
                    };

                    // ToDo:
                    //options.Cookie.Domain = "localhost";
                    //options.Cookie.Path = "/apf";
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = securityConfig.OpenIdConnectConfiguration.Authority.OrTake(null);
                    options.MetadataAddress = securityConfig.OpenIdConnectConfiguration.MetadataAddress.OrTake(null);
                    options.RequireHttpsMetadata = false;
                    //options.UsePkce = true;

                    options.ClientId = securityConfig.OpenIdConnectConfiguration.ClientId;
                    options.ClientSecret = securityConfig.OpenIdConnectConfiguration.ClientSecret.OrTake(null);
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.Events.OnRedirectToIdentityProvider = (context) =>
                    {
                        try
                        {
                            var queryString = System.Web.HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
                            if (!String.IsNullOrEmpty(queryString["id"]))
                            {
                                context.ProtocolMessage.SetParameter("map-portal-id", queryString["id"]);
                            }

                            if (!String.IsNullOrEmpty(queryString["name"]))
                            {
                                context.ProtocolMessage.SetParameter("map-portal-name", queryString["name"]);
                            }
                        }
                        catch { }
                        return Task.FromResult(0);
                    };

                    options.SaveTokens = true;

                    options.Scope.Clear();
                    foreach (var scope in securityConfig.OpenIdConnectConfiguration.Scopes ?? new string[] { "openid", "profile" })
                    {
                        options.Scope.Add(scope);
                    }

                    options.GetClaimsFromUserInfoEndpoint = securityConfig.OpenIdConnectConfiguration.ClaimsFromUserInfoEndpoint;

                    if (!String.IsNullOrEmpty(securityConfig.OpenIdConnectConfiguration.NameClaimType) ||
                       !String.IsNullOrEmpty(securityConfig.OpenIdConnectConfiguration.RoleClaimType))
                    {
                        var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters();
                        if (!String.IsNullOrEmpty(securityConfig.OpenIdConnectConfiguration.NameClaimType))
                        {
                            validationParameters.NameClaimType = securityConfig.OpenIdConnectConfiguration.NameClaimType;
                        }

                        if (!String.IsNullOrEmpty(securityConfig.OpenIdConnectConfiguration.RoleClaimType))
                        {
                            validationParameters.RoleClaimType = securityConfig.OpenIdConnectConfiguration.RoleClaimType;
                        }

                        options.TokenValidationParameters = validationParameters;
                    }

                    options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");
                });

            #endregion
        }
        else if (securityConfig?.UseAzureAD() == true)
        {
            services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.Instance = securityConfig.AzureADConfiguration?.Instance.OrTake(null);
                    options.Domain = securityConfig.AzureADConfiguration?.Domain.OrTake(null);
                    options.TenantId = securityConfig.AzureADConfiguration?.TenantId.OrTake(null);
                    options.ClientId = securityConfig.AzureADConfiguration.ClientId.OrTake(null);
                    options.ClientSecret = securityConfig.AzureADConfiguration.ClientSecret.OrTake(null);
                    options.CallbackPath = securityConfig.AzureADConfiguration.CallbackPath.OrTake("/signin-oidc");
                    options.SignedOutCallbackPath = securityConfig.AzureADConfiguration.CallbackPath.OrTake(null);
                });
        }
        else
        {
            //List<string> allowedSecurity = new List<string>(WebgisConfigSettings.AppSettings("security_allowed_methods", WebgisConfigSettings.AppConfigFileTitle).Split(','));
            //if (allowedSecurity.Contains("windows"))
            services.AddAuthentication(Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme);
        }

        #region Default Services

        services.AddTransient<WebgisCookieService>();
        services.AddTransient<IPortalAuthenticationService, WindowsAuthenticationService>();
        services.AddTransient<HeaderAuthenticationService>();

        services.AddTransient<ICustomPortalSecurityService, WindowsCustomPortalSecurityService>();

        if (configuration.HasExtendedRoleParametersHeaders())
        {
            services.AddTransient<ICustomPortalAuthenticationMiddlewareService, ExtendedRoleParametersFromHeaderCustomAuthenticationMiddlewareService>();
            Console.WriteLine("Added custom service: <ICustomPortalAuthenticationMiddlewareService, ExtendedRoleParametersFromHeaderCustomAuthenticationMiddlewareService>");
        }

        if (configuration.HasExtendedRoleParametersSourceAndStatement())
        {
            services.AddTransient<ICustomPortalAuthenticationMiddlewareService, ExtendedRoleParametersFromDatabaseCustomAuthenticationMiddlewareService>();
            Console.WriteLine("Added custom service: <ICustomPortalAuthenticationMiddlewareService, ExtendedRoleParametersFromDatabaseCustomAuthenticationMiddlewareService>");
        }

        #endregion

        return services;
    }
}
