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
using E.Standard.Localization.Abstractions;
using E.Standard.MessageQueues.Extensions.DependencyInjection;
using E.Standard.Security.App;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.KeyVault;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Extensions.DependencyInjection;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Extensions.DependencyInjection;
using E.Standard.Web.Services;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebGIS.Core.Extensions.DependencyInjection;
using E.Standard.WebGIS.SubscriberDatabase.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Extensions.DependencyInjection;
using Portal.Core.AppCode.Middleware;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Worker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;

namespace Portal;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
        CustomStartupServices = CustomStartupServiceFactory.LoadCustomStartupServices(WebGISAppliationTarget.Portal) ?? Array.Empty<ICustomStartupService>();

        #region Create Folders

        Configuration.TryCreateDirectoryIfNotExistes(PortalConfigKeys.ToKey("cache-connectionstring"));
        Configuration.TryCreateDirectoryIfNotExistes(PortalConfigKeys.ToKey("subscriber-db-connectionstring"));

        #endregion
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public IEnumerable<ICustomStartupService> CustomStartupServices { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddEssentialWebgisPortalServices()
            .AddWebgisPortalAuthenticationServices(Configuration)
            .AddMvc(o =>
            {
                o.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson();

        #region Http Client Service

        services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls | SslProtocols.Tls11,
#pragma warning restore SYSLIB0039 // allow old protocols (tls, tls11)
            //ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        services.AddHttpService<HttpService>(config =>
        {
            config.DefaultClientName = "default";
        });

        #endregion

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

        #region Cryptography

        if (!this.CustomStartupServices.ImplementsCryptographyService(this.Configuration))
        {
            services.AddCrytpographyService<CryptoService>(cryptoOptions =>
            {
                Dictionary<int, string> legacyKeys = new();
                if (!string.IsNullOrEmpty(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-storage-crypto-key"]))
                {
                    legacyKeys.Add((int)CustomPasswords.ApiStoragePassword, Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-storage-crypto-key"]);
                }
                if (!string.IsNullOrEmpty(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-user-crypto-key"]))
                {
                    legacyKeys.Add((int)CustomPasswords.ApiBridgeUserCryptoPassword, Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-user-crypto-key"]);
                }

                cryptoOptions.LoadOrCreate(
                        this.Configuration[PortalConfigKeys.SharedCryptoKeysPath].AppendToDefaultConfigPaths().ToArray(),
                        typeof(CustomPasswords),
                        legacyKeys,
                        !String.IsNullOrEmpty(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                ? Convert.FromBase64String(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                : null
                        );
            });
        }

        #endregion

        #region KeyValueCache

        services.AddKeyValueCacheService(options =>
        {
            options.CacheProviderConfigValue = PortalConfigKeys.KeyValueCacheProvider;
            options.CacheConnectionStringConfigValue = PortalConfigKeys.KeyValueCacheConnectionString;
            options.CacheExpireSecondsDefault = TimeSpan.FromDays(7).TotalSeconds;

            options.CacheAsideProviderConfigValue = PortalConfigKeys.KeyValueAsideCacheProvider;
            options.CacheAsideConnectionStringConfigValue = PortalConfigKeys.KeyValueAsideCacheConnectionString;
            options.CacheAsideExpireSecondsDefault = 3600D;

            options.ImpersonateUserConfigValue = PortalConfigKeys.ImpersonateUser;
        });

        #endregion

        #region Databases

        services.AddSubscriberDatabaseService(options =>
        {
            options.ConnectionStringConfigurationKey = PortalConfigKeys.SubscriberDbConnectionString;
        });

        services.AddSpatialReferenceService(options =>
        {
            options.AppRootPath = Environment.ContentRootPath;
            options.Proj4DatabaseConnectionStringConfigKey = PortalConfigKeys.Proj4DatabaseConnectionString;
        });

        #endregion

        #region Tracer

        if (Configuration[PortalConfigKeys.ToKey("trace")] == "true")
        {
            services.AddFileTracerService(options =>
            {
                options.TraceConfigKey = PortalConfigKeys.ToKey("trace");
                options.OutputPathConfigKey = PortalConfigKeys.ToKey("tracePath");
            });
        }
        else
        {
            services.AddNullTracerService();
        }

        #endregion

        #region Message Queue

        if (Configuration.UseMessageQueue() && !String.IsNullOrEmpty(Configuration.MessageQueueConnection()))
        {
            services.AddMessageQueueNetService(config =>
            {
                config.QueueName = E.Standard.Portal.App.PortalGlobals.MessageQueueName;
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

        services.AddHostedService<TimedHostedBackgroundService>();

        #endregion

        #region Data Protection

        if (!String.IsNullOrEmpty(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:data-protection-azure-blob"]))
        {
            Console.WriteLine("Use blob stoate data protection");

            string connectionString = configSecuritryHandler.ParseConfigurationValue(Configuration[$"{PortalConfigKeys.ConfigurationSectionName}:data-protection-azure-blob"]);
            var container = new BlobStorage(connectionString).GetContainer("webgis-portal-app").Result;

            services.AddDataProtection()
                 .PersistKeysToAzureBlobStorage(container.GetBlockBlobReference("keys.xml"));

        }

        #endregion

        #region CustomStartupServices

        this.CustomStartupServices.ConfigureServices(services, this.Configuration);

        #endregion

        #region Global Replements

        services.AddSingleton<GlobalReplacementsService>();

        #endregion

        #region Localization

        services.AddMarkdownLocalizerFactory<CultureProvider>(config =>
        {
            config.SupportedLanguages = Configuration.SupportedLanguages();
            config.DefaultLanguage = config.SupportedLanguages.First();
        });

        #endregion

        services.AddAntiforgery(o =>
        {
            o.SuppressXFrameOptionsHeader = Configuration[PortalConfigKeys.SecuritySuppressXFrameOptionsHeader]?.ToString()?.ToLower() == "true";
        });
    }

    public void Configure(WebApplication app,
                          ConfigurationService configService,
                          KeyValueCacheService keyValueCache,                              // Initialize Key Value Cache
                          ICryptoService cryptoService,                                    // Init CryptoService
                          IOptionsMonitor<ApplicationSecurityConfig> applicationSecurityMonitor,
                          ILogger<Startup> logger,
                          IMarkdownLocationInitializer markdownLocationInitializer,
                          IEnumerable<ICustomPortalAuthenticationMiddlewareService> customAuthentications = null,
                          IEnumerable<ICustomPortalSecurityService> customPortalSecurity = null)
    {
        logger.LogInformation("Configure Application");

        //if (Environment.IsDevelopment())
        //{
        //    //app.UseBrowserLink();
        //    app.UseDeveloperExceptionPage();
        //}
        //else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseWebgisAppBasePath();

        var applicationSecurity = applicationSecurityMonitor.CurrentValue;
        if (applicationSecurity.UseOpenIdConnect())
        {
            app.AddXForwardedProtoMiddleware();
        }

        app.UseStaticFiles();

        #region CustomStartupServices

        foreach (var customStartupService in this.CustomStartupServices)
        {
            customStartupService.Configure(app, configService);
        }

        #endregion

        #region DeChunkerMiddleware (nur im PVP Umfeld, wenn Reverse Proxies keine Chunks verstehen)

        if (configService.UseDeChunkerMiddleware())
        {
            app.UseMiddleware<DeChunkerMiddleware>();
        }

        #endregion

        app.UseRouting();

        // Must be between UseRouting & UseEndpoints
        app.UseWebgisAuthorizationMiddleware(applicationSecurity, configService, customAuthentications);

        #region Map Controller Endpoints


        app.MapControllerRoute(
            "hmac",
            "hmac",
            new { controller = "Hmac", action = "Index" }
        );

        app.MapControllerRoute(
            "cache",
            "cache/{action}",
            new { controller = "Cache" }
        );

        app.MapControllerRoute(
            "proxy-toolmethod",
            "proxy/toolmethod/{id}/{method}",
            new { controller = "Proxy", action = "ToolMethod" }
        );

        app.MapControllerRoute(
            "mapbuilder-action",
            "{id}/mapbuilder/{action}",
            new { controller = "MapBuilder" }
        );
        app.MapControllerRoute(
            "mapbuilder",
            "{id}/mapbuilder",
            new { controller = "MapBuilder", action = "Index" }
        );

        app.MapControllerRoute(
            "portal_map",
            "{id}/map/{category}/{map}",
            new { controller = "Map", action = "Index" }
        );

        app.MapControllerRoute(
            "portal_app",
            "{id}/app/{template}/{project}",
            new { controller = "App", action = "Index" }
        );

        app.MapControllerRoute(
            "portal-ar-pois",
            "{id}/ar/pois",
            new { controller = "Ar", action = "Pois" }
        );

        app.MapControllerRoute(
            "map_layout",
            "{id}/maplayout",
            new { controller = "Map", action = "Layout" }
        );

        app.MapControllerRoute(
            "map_layout",
            "{id}/maplayout-templates",
            new { controller = "Map", action = "LayoutTemplates" }
        );

        app.MapControllerRoute(
            "appBuilder",
            "{id}/appbuilder",
            new { controller = "AppBuilder", action = "Index" }
        );
        app.MapControllerRoute(
            "appBuilder-action",
            "{id}/appbuilder/{action}",
            new { controller = "AppBuilder" }
        );

        app.MapControllerRoute(
            "portal_map2",
            "{id}/map",
            new { controller = "Map", action = "Index" }
        );

        app.MapControllerRoute(
            "page-editcontent",
            "{id}/editcontent",
            new { controller = "Home", action = "EditContent" }
        );

        app.MapControllerRoute(
            "page-removecontent",
            "{id}/removecontent",
            new { controller = "Home", action = "RemoveContent" }
        );

        app.MapControllerRoute(
            "page-sortcontent",
            "{id}/sortcontent",
            new { controller = "Home", action = "SortContent" }
        );

        app.MapControllerRoute(
           "page-uploadcontentimage",
           "{id}/UploadContentImage",
           new { controller = "Home", action = "UploadContentImage" }
       );

        app.MapControllerRoute(
            "page-contentimage",
            "{id}/ContentImage",
            new { controller = "Home", action = "ContentImage" }
        );

        app.MapControllerRoute(
            "page-sortitems",
            "{id}/sortItems",
            new { controller = "Home", action = "SortItems" }
        );

        app.MapControllerRoute(
            "page-mapimage",
            "{id}/MapImage",
            new { controller = "Map", action = "MapImage" }
        );

        app.MapControllerRoute(
           "page-uploadmapimage",
           "{id}/UploadMapImage",
           new { controller = "Map", action = "UploadMapImage" }
        );

        app.MapControllerRoute(
            "page-updatedescription",
            "{id}/UpdateMapDescription",
            new { controller = "Map", action = "UpdateMapDescription" }
            );
        app.MapControllerRoute(
            "page-deletemap",
            "{id}/DeleteMap",
            new { controller = "Map", action = "DeleteMap" }
            );

        app.MapControllerRoute(
            "rss",
            "{id}/rss",
            new { controller = "Home", action = "Rss" }
            );

        app.MapControllerRoute(
            "page",
            "{id}",
            new { controller = "Home", action = "Index" }
        );

        app.MapControllerRoute(
            "page-changestyles",
            "{id}/ChangeStyles",
            new { controller = "Home", action = "ChangeStyles" }
        );

        app.MapControllerRoute(
            "page-serviceworker",
            "{id}/ServiceWorker",
            new { controller = "Home", action = "ServiceWorker" }
        );

        app.MapControllerRoute(
            "customcontent-load",
            "customcontent/{id}/load",
            new { controller = "CustomContent", action = "Load" }
        );

        app.MapControllerRoute(
             "customcontent-save",
            "customcontent/{id}/save",
            new { controller = "CustomContent", action = "Save" }
        );

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        #endregion

        logger.LogInformation("Init PortalAppApp");

        E.Standard.WebGIS.Core.UserManagement.AllowWildcards = customPortalSecurity.AllowUsernamesAndRolesWithWildcard();

        logger.LogInformation("Finished Configuration");
    }
}
