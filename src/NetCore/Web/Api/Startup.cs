using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Extensions.DependencyInjection;
using Api.Core.AppCode.Middleware;
using Api.Core.AppCode.Middleware.Authentication;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Worker;
using E.DataLinq.Web.Extensions.DependencyInjection;
using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Extensions.DependencyInjection;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Azure.Extensions.DependencyInjection;
using E.Standard.Azure.Storage;
using E.Standard.Caching.Extensions.DependencyInjection;
using E.Standard.Caching.Services;
using E.Standard.Configuration.Extensions;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Custom.Core.Services;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.MessageQueues.Extensions.DependencyInjection;
using E.Standard.Security.App;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.KeyVault;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Extensions.DependencyInjection;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Extensions.DependencyInjection;
using E.Standard.Web.Services;
using E.Standard.WebApp.Extensions;
using E.Standard.WebGIS.Core.Extensions.DependencyInjection;
using E.Standard.WebGIS.SDK.Extensions.DependencyInjection;
using E.Standard.WebGIS.SubscriberDatabase.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

namespace Api;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        Environment = env;
        CustomStartupServices = CustomStartupServiceFactory.LoadCustomStartupServices(WebGISAppliationTarget.Api) ?? new ICustomStartupService[0];

        JsonOptions.SerializerOptions.AddServerDefaults();
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public IEnumerable<ICustomStartupService> CustomStartupServices { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEssentialWebgisApiServices(Configuration);

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

        services.AddMvc(o =>
        {
            o.EnableEndpointRouting = false;
        })
        .AddNewtonsoftJson();

        #region ConfigValueParser Services

        IKeyVault keyVault = null;

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

            keyVault = new E.Standard.Azure.KeyVault.KeyVault(
                Configuration["AZURE_KV_Uri"],
                Configuration["AZURE_AD_TenantId"],
                Configuration["AZURE_AD_ClientId"],
                Configuration["AZURE_AD_ClientSecret"]);
        }
        else
        {
            keyVault = new EnvironmentKeyVault();
        }

        var configSecuritryHandler = new ConfigSecurityHandler(keyVault);

        #endregion

        #region CmsConfigurationService (CCS)

        services.AddConfiguraionService();

        #endregion

        #region KeyValueCache

        services.AddKeyValueCacheService(options =>
        {
            options.CacheProviderConfigValue = ApiConfigKeys.KeyValueCacheProvider;
            options.CacheConnectionStringConfigValue = ApiConfigKeys.KeyValueCacheConnectionString;
            options.CacheExpireSecondsDefault = TimeSpan.FromDays(7).TotalSeconds;

            options.CacheAsideProviderConfigValue = ApiConfigKeys.KeyValueAsideCacheProvider;
            options.CacheAsideConnectionStringConfigValue = ApiConfigKeys.KeyValueAsideCacheConnectionString;
            options.CacheAsideExpireSecondsDefault = 3600D;

            options.ImpersonateUserConfigValue = ApiConfigKeys.ImpersonateUser;
        });

        #endregion

        #region Databases

        services.AddSubscriberDatabaseService(options =>
        {
            options.ConnectionStringConfigurationKey = ApiConfigKeys.SubscriberDbConnectionString;
        });

        services.AddSpatialReferenceService(options =>
        {
            options.AppRootPath = Environment.ContentRootPath;
            options.Proj4DatabaseConnectionStringConfigKey = ApiConfigKeys.Proj4DatabaseConnectionString;
            options.ServerSideConfigurationPathConfigKey = ApiConfigKeys.ServerSideConfigurationPath;
        });

        #endregion

        #region SDK

        services.AddSDKPluginManagerService(config =>
        {
            config.RootPath = $"{Environment.ContentRootPath}/bin/plugins";
        });

        #endregion

        #region CMS

        services.AddCmsDocumentsService(options =>
        {
            options.AppRootPath = Environment.ContentRootPath;
        });

        #endregion

        #region DataLinq

        if (Configuration.IncludeDataLinqServices())
        {
            services.AddDatalinqServices(Configuration, this.CustomStartupServices);
            if (Configuration.AllowDataLingCodeEditing())
            {
                services.AddDatalinqCodeApiServices(Configuration);
            }
        }

        #endregion

        #region HttpClient

        var webProxy = Configuration.GetWebProxy();

#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)

        services.AddHttpClient("default", client =>
        {
            if(ApiGlobals.HttpClientDefaultTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(ApiGlobals.HttpClientDefaultTimeoutSeconds);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls | SslProtocols.Tls11,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        if (webProxy != null)
        {
            services.AddHttpClient("default-proxy", client =>
        {
            if(ApiGlobals.HttpClientDefaultTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(ApiGlobals.HttpClientDefaultTimeoutSeconds);
            }
        })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls | SslProtocols.Tls11,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                Proxy = webProxy
            });
        }

#pragma warning restore SYSLIB0039

        services.AddHttpService<HttpService>(config =>
        {
            config.DefaultClientName = "default";
            config.DefaultProxyClientName = webProxy != null ? "default-proxy" : null;
            config.IgnoreProxyServers = Configuration.WebProxyIgnoresOrNull();
            config.WebProxyInstance = webProxy;

            config.UrlOutputRedirections = Configuration.UrlOutputRedirectionsOrNull();
            config.UrlInputRedirections = Configuration.UrlInputRedirectionsOrNull();

            config.Legacy_AlwaysDownloadFrom = Configuration.LegacyAlwaysDownloadFromOrNull();
        });

        #endregion

        #region Message Queue

        if (Configuration.UseMessageQueue() && !String.IsNullOrEmpty(Configuration.MessageQueueConnection()))
        {
            services.AddMessageQueueNetService(config =>
            {
                config.QueueName = E.Standard.Api.App.ApiGlobals.MessageQueueName;
                config.QueueServiceUrl = Configuration.MessageQueueConnection();
                config.Namespace = Configuration.MessageQueueNamespace();
                config.ApiClient = Configuration.MessageQueueClient();
                config.ApiClientSecret = Configuration.MessageQueueClientSecret();
            });

            services.AddTransient<IWorkerService, MessageQueueWorkerService>();
        }
        else
        {
            services.AddDummyMessageQueueService();
        }

        #endregion

        #region Background Task

        services.AddTransient<IWorkerService, ClearOutputWorkerService>();
        services.AddTransient<IWorkerService, ClearSharedMapsWorkerService>();
        services.AddHostedService<TimedHostedBackgroundService>();

        #endregion

        #region Cryptography

        if (!this.CustomStartupServices.ImplementsCryptographyService(this.Configuration))
        {
            services.AddCrytpographyService<CryptoService>(config =>
            {
                Dictionary<int, string> legacyKeys = new();
                if (!string.IsNullOrEmpty(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-storage-crypto-key"]))
                {
                    legacyKeys.Add((int)CustomPasswords.ApiStoragePassword, Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-storage-crypto-key"]);
                }
                if (!string.IsNullOrEmpty(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-user-crypto-key"]))
                {
                    legacyKeys.Add((int)CustomPasswords.ApiBridgeUserCryptoPassword, Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-user-crypto-key"]);
                }

                config.LoadOrCreate(
                        this.Configuration[ApiConfigKeys.SharedCryptoKeysPath].AppendToDefaultConfigPaths().ToArray(),
                        typeof(CustomPasswords),
                        customLegacyPasswords: legacyKeys,
                        customLegacySalt:
                            !String.IsNullOrEmpty(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                ? Convert.FromBase64String(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                : null,
                        legacyDataLinqDefaultPassword: Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-datalinq-crypto-key"]
                        );
            });
        }

        var cmsNames = Configuration.AllCmsNames().ToArray();

        foreach (string cmsName in cmsNames)
        {
            if (Configuration.IsCmsUploadAllowed(cmsName)
                && !string.IsNullOrEmpty(Configuration.CmsUploadClient(cmsName))
                && !string.IsNullOrEmpty(Configuration.CmsUploadSecret(cmsName)))
            {
                services.AddKeyedSingleton(
                    $"cms-upload-{cmsName}",
                    JwtAccessTokenService.Create(Configuration.CmsUploadSecret(cmsName))
                    );
            }
        }

        #endregion

        #region Data Protection

        if (!String.IsNullOrEmpty(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:data-protection-azure-blob"]))
        {
            Console.WriteLine("Use blob stoate data protection");

            string connectionString = configSecuritryHandler.ParseConfigurationValue(Configuration[$"{ApiConfigKeys.ConfigurationSectionName}:data-protection-azure-blob"]);
            var container = new BlobStorage(connectionString).GetContainer("webgis-api-app").Result;

            services.AddDataProtection()
                 .PersistKeysToAzureBlobStorage(container.GetBlockBlobReference("keys.xml"));

        }

        #endregion

        #region CustomStartupServices

        this.CustomStartupServices.ConfigureServices(services, this.Configuration);

        #endregion

        #region Logging

        services.AddWebGISLogging(Configuration);

        #endregion

        #region Localization

        services.AddMarkdownLocalizerFactory<CultureProvider>(config =>
        {
            config.SupportedLanguages = Configuration.SupportedLanguages();
            config.DefaultLanguage = config.SupportedLanguages.First();
        });

        #endregion
    }

    public void Configure(WebApplication app,
                          ConfigurationService config,
                          KeyValueCacheService keyValueCache,                               // Initialize Key Value Cache
                          CacheService cacheService,
                          ApiGlobalsService apiGlobals,                                     // Init Globals
                          ICryptoService cryptoService,                                     // Init CryptoService
                          IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity,
                          IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                          ILogger<Startup> logger,
                          IMarkdownLocationInitializer markdownLocationInitializer,
                          IEnumerable<ICustomApiAuthenticationMiddlewareService> customAuthentications = null,
                          IEnumerable<ICustomRouteService> customRouteServices = null)
    {
        Console.WriteLine("Startup.Configure...");

        if (Environment.IsDevelopment())
        {
            //app.UseDeveloperExceptionPage();
            app.UseExceptionHandler("/Home/Error");
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseWebgisAppBasePath();
        app.UseStaticFiles(new StaticFileOptions()
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
            }
        });

        #region CustomStartupServices

        foreach (var customStartupService in this.CustomStartupServices)
        {
            customStartupService.Configure(app, config);
        }

        #endregion

        #region DeChunkerMiddleware (nur im PVP Umfeld, wenn Reverse Proxies keine Chunks verstehen)

        if (config.UseDeChunkerMiddleware())
        {
            app.UseMiddleware<DeChunkerMiddleware>();
        }

        #endregion

        app.UseRouting();

        app.AddApiExceptionMiddleware();
        app.AddEtagHandling();
        app.AddOriginalUrlParametersMiddleware();

        #region Authentication Middleware

        switch (applicationSecurity.CurrentValue?.IdentityType)
        {
            case "oidc":
                app.UseAuthentication();
                app.UseAuthorization();
                break;
        }

        if (Configuration.AllowDataLingCodeEditing())
        {
            app.UseDatalinqTokenAuthorization();
        }
        app.UseMiddleware<HmacAuthenticationMiddleware>();
        app.UseMiddleware<ClientIdAndSecretAuthenticationMiddleware>();
        app.UseMiddleware<PortalProxyRequestAuthenticationMiddleware>();
        app.UseMiddleware<BasicAuthenticationMiddleware>();
        if (customAuthentications != null && customAuthentications.Count() > 0)
        {
            app.UseMiddleware<CustomAuthenticationMiddleware>();
        }
        app.UseMiddleware<ApiCookieAuthenticationMiddleware>();

        #endregion

        app.RegisterApiEndpoints(typeof(Startup));

        #region Map Controller Routes

        app.MapControllerRoute(
           "rest/services/service/request/objectId",
           "rest/services/{serviceId}/{request}/{objectId}",
           new { controller = "Rest", action = "ServiceObjectRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request/objectId/command",
           "rest/services/{serviceId}/{request}/{objectId}/{command}",
           new { controller = "Rest", action = "ServiceObjectRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request/objectId_post",
           "rest/services/{serviceId}/{request}/{objectId}",
           new { controller = "Rest", action = "ServiceObjectPostRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request/objectId/command_post",
           "rest/services/{serviceId}/{request}/{objectId}/{command}",
           new { controller = "Rest", action = "ServiceObjectPostRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request/objectId_options",
           "rest/services/{serviceId}/{request}/{objectId}",
           new { controller = "Rest", action = "ServiceObjectOptionsRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request/objectId/command_options",
           "rest/services/{serviceId}/{request}/{objectId}/{command}",
           new { controller = "Rest", action = "ServiceObjectOptionsRequest" }
           );
        app.MapControllerRoute(
           "rest/services/service/request",
           "rest/services/{id}/{request}",
           new { controller = "Rest", action = "ServiceRequest" }
           );
        app.MapControllerRoute(
           "rest/toolmethod/toolid/method",
           "rest/toolmethod/{toolId}/{method}",
           new { controller = "Rest", action = "ToolMethod" }
           );
        app.MapControllerRoute(
           "rest/tooldata/toolid/method",
           "rest/tooldata/{toolId}/{method}",
           new { controller = "Rest", action = "ToolData" }
           );

        app.MapControllerRoute(
           "rest/serviceinfo/service",
           "rest/serviceinfo/{ids}",
           new { controller = "Rest", action = "ServiceInfo" }
           );
        app.MapControllerRoute(
           "rest/services/service",
           "rest/Services/{ids}",
           new { controller = "Rest", action = "ServiceInfo" }
           );
        app.MapControllerRoute(
           "rest/extent/extent",
           "rest/extent/{id}",
           new { controller = "Rest", action = "Extent" }
           );
        app.MapControllerRoute(
           "rest/extents/extent",
           "rest/extents/{id}",
           new { controller = "Rest", action = "Extent" }
           );
        app.MapControllerRoute(
           "rest/sref/sref",
           "rest/sref/{id}",
           new { controller = "Rest", action = "SRef" }
           );
        app.MapControllerRoute(
           "rest/srefs/sref",
           "rest/srefs/{id}",
           new { controller = "Rest", action = "SRef" }
           );

        app.MapControllerRoute(
           "rest/searchservice/searvice",
           "rest/searchservice/{serviceId}",
           new { controller = "Rest", action = "SearchService" }
           );
        app.MapControllerRoute(
            "rest/search/searvice",
            "rest/search/{serviceId}",
            new { controller = "Rest", action = "SearchService" }
            );

        app.MapControllerRoute(
            "ogc/service/wmts",
            "ogc/{id}/WMTS/1.0.0/WMTSCapabilities",
            new { controller = "OGC", action = "Index" }
            );

        app.MapControllerRoute(
            "ogc/logout",
            "ogc/logout",
            new { controller = "OGC", action = "Logout" }
            );

        app.MapControllerRoute(
            "ogc/cacheclear",
            "ogc/cacheclear",
            new { controller = "OGC", action = "CacheClear" }
            );

        app.MapControllerRoute(
           "ogc",
           "ogc/{id}",
           new { controller = "OGC", action = "Index" }
           );

        app.AddDataLinqEndpoints();

        app.MapControllerRoute(
            "output",
            "output/{id}",
            new { controller = "Output", Action = "Index" });

        if (customRouteServices != null)
        {
            foreach (var customRouteService in customRouteServices.Where(c => c.Routes != null))
            {
                foreach (var customRoute in customRouteService.Routes)
                {
                    app.MapControllerRoute(
                        Guid.NewGuid().ToString(),
                        customRoute.Pattern,
                        new { controller = customRoute.Controller, Action = customRoute.Action });
                }
            }
        }

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
        );

        #endregion

        #region Init Cache

        logger.LogInformation("Initialize Cache");

        cacheService.Init(expectableUserRolesNamesProviders);

        E.Standard.WebGIS.Core.UserManagement.AllowWildcards = config.SecurtyUsermanagementAllowWildcards();

        logger.LogInformation("Cache Initialized");

        #endregion
    }
}
