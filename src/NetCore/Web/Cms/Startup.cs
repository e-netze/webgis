using Cms.AppCode.Extensions.DependencyInjection;
using E.DataLinq.Code.Extensions.DependencyInjection;
using E.Standard.ActiveDirectory.Services.ApplicationSecurity;
using E.Standard.Azure.Extensions.DependencyInjection;
using E.Standard.Cms.Configuration.Extensions;
using E.Standard.Cms.Configuration.Extensions.DependencyInjection;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core.IO;
using E.Standard.Configuration;
using E.Standard.Configuration.Extensions;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Custom.Core.Services;
using E.Standard.Extensions.Security;
using E.Standard.OpenIdConnect.Extensions.Services.ApplicationSecurity;
using E.Standard.Security.App.Extensions.DependencyInjection;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Extensions.DependencyInjection;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Extensions.DependencyInjection;
using E.Standard.Web.Services;
using E.Standard.WebGIS.SubscriberDatabase.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;

namespace Cms;

public class Startup
{
    public Startup(IConfiguration configuration,
                   IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
        CustomStartupServices = CustomStartupServiceFactory.LoadCustomStartupServices(WebGISAppliationTarget.Cms) ?? new ICustomStartupService[0];
        ConfigureCmsApplication();

        var settingsAppConfig = new JsonAppConfiguration("settings.config");
        this.Settings = settingsAppConfig.Exists ?
            settingsAppConfig.Deserialize<SettingsConfig>().AddDefaults() :
            SettingsConfig.Defaults;
    }

    private void ConfigureCmsApplication()
    {
        // Micosoft Bug mit .NET Core 2.1 -> Verbindungen mit Proxy erzeugen error
        // https://github.com/dotnet/corefx/issues/30166
        //AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
        System.Net.WebRequest.DefaultWebProxy = null;

#pragma warning disable SYSLIB0014
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        System.Net.ServicePointManager.SecurityProtocol |=
                    System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
#pragma warning restore SYSLIB0014

        DocumentFactory.DocumentTypes.Add("mongodb://", typeof(E.Standard.CMS.MongoDB.MongoStreamDocument));
        DocumentFactory.DocumentInfoTypes.Add("mongodb://", typeof(E.Standard.CMS.MongoDB.MongoDocumentInfo));
        DocumentFactory.PathInfoTypes.Add("mongodb://", typeof(E.Standard.CMS.MongoDB.MongoPathInfo));
        DocumentFactory.CanImportValues.Add("mongodb://", true);
        DocumentFactory.CanClearValues.Add("mongodb://", true);
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public SettingsConfig Settings { get; }

    public IEnumerable<ICustomStartupService> CustomStartupServices { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationSecurityConfiguration();

        var securityConfig = new ApplicationSecurityConfig().LoadFromJsonFile();

        if (securityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection)
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies", options =>
            {
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = securityConfig.OpenIdConnectConfiguration.Authority;
                options.RequireHttpsMetadata = false;

                options.ClientId = securityConfig.OpenIdConnectConfiguration.ClientId;
                options.ClientSecret = securityConfig.OpenIdConnectConfiguration.ClientSecret;
                options.ResponseType = "code";

                options.SaveTokens = true;

                options.Scope.Clear();
                foreach (var scope in securityConfig.OpenIdConnectConfiguration.Scopes ??
                        (
                            String.IsNullOrWhiteSpace(securityConfig.OpenIdConnectConfiguration?.RequiredRole)
                                ? new string[] { "openid", "profile" }
                                : new string[] { "openid", "profile", "role" })
                        )
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
                else
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                }

                options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");
            });
        }
        else if (securityConfig?.IdentityType == ApplicationSecurityIdentityTypes.AzureAD)
        {

        }
        else if (securityConfig?.IdentityType == ApplicationSecurityIdentityTypes.Windows)
        {
            services.AddAuthentication(IISDefaults.AuthenticationScheme);
        }
        else
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                //options.CheckConsentNeeded = context => true;
                //options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            if (!String.IsNullOrEmpty(securityConfig?.IdentityType) && securityConfig.IdentityType.StartsWith("subscriber-db:"))
            {
                services.AddSubscriberDatabaseService(options =>
                {
                    options.ConnectionStringConfigurationKey = securityConfig.IdentityType.Substring("subscriber-db:".Length);
                });
            }
        }

        services.AddAppliationSecurityUserManager()
            .AddApplicationSecurityProvider<OpenIdConnectionApplicationSecurityProvider>()
            .AddApplicationSecurityProvider<WindowsApplicationSeurityProvider>();

        services.AddMvc(o =>
        {
            o.EnableEndpointRouting = false;
        })
        .AddNewtonsoftJson();

        services.AddEssentialCmsServices();

        #region CmsConfigurationService (CCS)

        services.AddConfiguraionService();

        #endregion

        #region Cryptography

        var appConfig = new JsonAppConfiguration("cms.config");
        CmsConfig cmsConfig = appConfig.Exists
            ? appConfig.Deserialize<CmsConfig>()
            : new CmsConfig();

        if (!this.CustomStartupServices.ImplementsCryptographyService(this.Configuration))
        {
            services.AddCrytpographyService<CryptoService>(config =>
            {
                string keyPath = this.Environment.ContentRootPath;
                try
                {
                    if (!String.IsNullOrEmpty(cmsConfig.SharedCrptoKeysPath))
                    {
                        keyPath = cmsConfig.SharedCrptoKeysPath;
                    }
                }
                catch { }
                config.LoadOrCreate(keyPath.AppendToDefaultConfigPaths().ToArray(), typeof(CustomPasswords));
            });
        }

        foreach (var cmsItem in cmsConfig.CmsItems ?? []) 
        {
            foreach (var deployment in cmsItem.Deployments?
                                              .Where(d => d.Target.IsUrl()) ?? [])
            {
                services.AddKeyedSingleton($"cms-upload-{cmsItem.Id}-{deployment.Name}", JwtAccessTokenService.Create(deployment.Secret));
            }
        }

        #endregion

        #region HttpClient

        var webProxy = Settings.GetWebProxy();

#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)

        services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        if (webProxy != null)
        {
            services.AddHttpClient("default-proxy").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                Proxy = webProxy
            });
        }

#pragma warning restore SYSLIB0039 // allow old protocols (tls, tls11)

        services.AddHttpService<HttpService>(config =>
        {
            config.DefaultClientName = "default";
            config.DefaultProxyClientName = webProxy != null ? "default-proxy" : null;
            config.IgnoreProxyServers = Settings.WebProxyIgnores();
            config.WebProxyInstance = webProxy;
        });

        #endregion

        #region ConfigValueParser Services

        services.AddHostingEnvironmentConfigValueParser();
        if (!String.IsNullOrWhiteSpace(Configuration["AZURE_AD_ClientId"]) &&
                !String.IsNullOrWhiteSpace(Configuration["AZURE_AD_ClientSecret"]) &&
                !String.IsNullOrWhiteSpace(Configuration["AZURE_AD_TenantId"]) &&
                !String.IsNullOrWhiteSpace(Configuration["AZURE_KV_Uri"]))
        {
            Console.WriteLine("Add AzureKeyVaultConfigValueParser");
            services.AddAzureKeyVaultConfigValueParser(options =>
            {
                options.KeyValueUri = Configuration["AZURE_KV_Uri"];
                options.TenantId = Configuration["AZURE_AD_TenantId"];
                options.ClientId = Configuration["AZURE_AD_ClientId"];
                options.ClientSecret = Configuration["AZURE_AD_ClientSecret"];
            });
        }

        #endregion

        #region CmsConfigurationService (CCS)

        services.AddCmsConfigurationService(config =>
        {
            config.ContentPath = Environment.ContentRootPath;
        });

        #endregion

        #region CustomStartupServices

        this.CustomStartupServices.ConfigureServices(services, this.Configuration);

        #endregion

        #region DataLinq Code

        services.AddDataLinqCodeServices(Configuration, Environment, CustomStartupServices);

        #endregion

        #region Logging

        services.AddCmsLogging(this.Settings?.LoggingConnectionString);

        #endregion

        #region Cms Tools

        services.AddTransient<DeployService>();
        services.AddTransient<SolveWaringsService>();
        services.AddTransient<ClearCmsService>();
        services.AddTransient<ReloadSchemeService>();
        services.AddTransient<ExportCmsService>();

        #endregion

        services.AddTransient<CmsItemInjectionPackService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(WebApplication app,
                          IWebHostEnvironment env,
                          IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity,
                          ICryptoService crypto,
                          ILogger<Startup> logger)
    {
        try
        {
            Console.WriteLine("Startup.Configure...");

            var securityConfig = applicationSecurity.CurrentValue;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseWebgisAppBasePath();
            app.AddXForwardedProtoMiddleware();

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            //app.UseCookiePolicy();

            switch (securityConfig?.IdentityType)
            {
                case ApplicationSecurityIdentityTypes.OpenIdConnection:
                case ApplicationSecurityIdentityTypes.AzureAD:
                    app.UseAuthentication();
                    app.UseAuthorization();
                    break;
            }

            app.UseDatalinqCodeAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "cms",
                    template: "{id}/cms",
                    defaults: new { controller = "Cms", action = "index" }
                    );
                routes.MapRoute(
                    name: "cms-action",
                    template: "{id}/cms/{action}",
                    defaults: new { controller = "Cms" }
                    );

                routes.MapRoute(
                    name: "deploy",
                    template: "{id}/deploy",
                    defaults: new { controller = "Deploy", action = "index" }
                    );
                routes.MapRoute(
                    name: "deploy-name",
                    template: "{id}/deploy/{name}",
                    defaults: new { controller = "Deploy", action = "Deploy" }
                    );
                routes.MapRoute(
                    name: "solvewarnings-name",
                    template: "{id}/solvewarnings/{name}",
                    defaults: new { controller = "Deploy", action = "SolveWarnings" }
                    );
                routes.MapRoute(
                    name: "export",
                    template: "{id}/export",
                    defaults: new { controller = "IO", action = "Export" }
                    );
                routes.MapRoute(
                    name: "import",
                    template: "{id}/import",
                    defaults: new { controller = "IO", action = "Import" }
                    );
                routes.MapRoute(
                    name: "Clear",
                    template: "{id}/Clear",
                    defaults: new { controller = "IO", action = "Clear" }
                    );
                routes.MapRoute(
                    name: "ReloadScheme",
                    template: "{id}/ReloadScheme",
                    defaults: new { controller = "IO", action = "ReloadScheme" }
                    );

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            DocumentFactory.Init(crypto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error on startup");

            System.Environment.Exit(-1);
        }
    }
}
