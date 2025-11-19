using Api.Core.AppCode.Extensions.Razor;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Api;
using Api.Core.AppCode.Services.Api.DataLinqEngines;
using Api.Core.AppCode.Services.Authentication;
using Api.Core.AppCode.Services.DataLinq;
using Api.Core.AppCode.Services.Logging;
using Api.Core.AppCode.Services.Ogc;
using Api.Core.AppCode.Services.Rest;
using Api.Core.Models.DataLinq;
using E.DataLinq.Core;
using E.DataLinq.Core.Engines.Abstraction;
using E.DataLinq.Core.Services.Abstraction;
using E.DataLinq.Core.Services.Persistance;
using E.DataLinq.Web;
using E.DataLinq.Web.Extensions.DependencyInjection;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Extensions.DependencyInjection;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Extensions.DependencyInjection;
using E.Standard.Configuration.Extensions;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.Security.App.Extensions.DependencyInjection;
using E.Standard.Security.Cryptography;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core.Extensions.DependencyInjection;
using E.Standard.WebMapping.Core.Logging;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.GeoServices.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    public static IServiceCollection IfServiceNotRegistered<TService>(this IServiceCollection services, Action action)
    {
        if (services.Any(s => s.ServiceType == typeof(TService)) == false)
        {
            action();
        }

        return services;
    }

    static public IServiceCollection AddEssentialWebgisApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAppEnvironment, AppEnvironment>();
        services.AddSingleton<CacheService>();
        services.AddSingleton<ApiConfigurationService>();
        services.AddSingleton<ApiGlobalsService>();

        services.AddTransient<UrlHelperService>();                    // UrlHelperInstance
        services.AddTransient<IUrlHelperService, UrlHelperService>(); // UrlHelperInstance as IUrlHelperService (used in MapServerInitializerServer)
        services.AddScoped<HttpRequestContextService>();

        services.AddTransient<ApiToolsService>();
        services.AddTransient<UploadFilesService>();
        services.AddTransient<WebgisPortalService>();
        services.AddTransient<EtagService>();
        services.AddTransient<OgcRequestService>();
        services.AddTransient<ApiCookieAuthenticationService>();
        services.AddTransient<HmacAuthenticationService>();
        services.AddTransient<ViewDataHelperService>();
        services.AddTransient<ContentResourceService>();
        //services.AddTransient<WebProxyService>();
        services.AddTransient<ApiLoggingService>();
        services.AddTransient<ApiJavaScriptService>();
        services.AddMapServiceIntitializerService();
        services.AddLookupService();
        services.AddTransient<ICustomApiInteractionService, CustomApiInteractionService>();
        services.AddTransient<IExtendedControllerService, ExtendedControllerService>();

        services.AddTransient<CacheClearService>();

        services.AddRestServiceFactory(configuration);

        // obsolote: https://github.com/aspnet/Announcements/issues/520 ... replaces by IHttpContextAccessor
        //services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
        services.AddHttpContextAccessor();

        services.AddRoutingEndPointReflectionService(options =>
        {
            options.AppRoles = E.Standard.Api.App.AppRoles.None;

            var roles = configuration[ApiConfigKeys.AppRoles];
            if (String.IsNullOrWhiteSpace(roles))
            {
                options.AppRoles = E.Standard.Api.App.AppRoles.All;
            }
            else
            {
                foreach (var role in roles.Split(','))
                {
                    if (Enum.TryParse(typeof(E.Standard.Api.App.AppRoles), role, true, out object appRole))
                    {
                        options.AppRoles |= (E.Standard.Api.App.AppRoles)appRole;
                    }
                }
            }
        });
        services.AddRequestContextService();

        services.AddGeoServiceDependencies();

        services.AddFileSystemTempDataCache(config =>
         {
             config.RootPath = configuration.TempFolderPath();
             config.SubFolder = "webgis-cache";
         });
        services.AddInAppTempDataObjectCache();

        services.AddCancellationTokenService(config =>
        {
            config.TimeoutMillisecnds = configuration.BufferResultsTimeoutSeconds() * 1000;
        });

        services.AddBotDetection();


        return services;
    }

    static public IServiceCollection AddRestServiceFactory(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddTransient<RestServiceFactory>()
            .AddTransient<RestHelperService>()
            .AddTransient<RestMappingHelperService>()
            .AddTransient<RestQueryHelperService>()
            .AddTransient<RestEditingHelperService>()
            .AddTransient<RestToolsHelperService>()
            .Configure<RestSearchHelperServiceOptions>((config) =>
            {
                config.DefaultQuerySrefId = configuration.DefaultQuerySrefId();
                config.AllowGeoCodesInput = configuration.AllowGeoCodesInput();
                config.MaxResultItems = configuration.QuickSearchMaxResultItems();
            })
            .AddTransient<RestSearchHelperService>()
            .AddTransient<RestPrintHelperService>()
            .AddTransient<RestRequestHmacHelperService>()
            .AddTransient<RestImagingService>()
            .AddTransient<RestSnappingService>()
            // Transient (depends on IHttpContextAccessor
            .AddTransient<BridgeService>();

        return services;
    }

    static public IServiceCollection AddRoutingEndPointReflectionService(this IServiceCollection services,
                                                      Action<RoutingEndPointReflectionServiceOptions> configureOptions)
    {
        services.Configure<RoutingEndPointReflectionServiceOptions>(configureOptions);
        services.AddScoped<RoutingEndPointReflectionService>();  // Scoped => eine Instanz pro Http Request!!
        return services;
    }

    static public IServiceCollection AddCancellationTokenService(this IServiceCollection services, Action<CancellationTokenServiceOptions> configAction)
    {
        services.Configure<CancellationTokenServiceOptions>(configAction);
        services.AddTransient<CancellationTokenService>();
        return services;
    }

    static public IServiceCollection AddWebGISLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var loggingType = configuration[ApiConfigKeys.LoggingType];

        if (loggingType == "files")
        {
            if (configuration[ApiConfigKeys.LoggingLogPerformance]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IGeoServicePerformanceLogger, CsvGeoServicePerformanceLogger>();
                services.AddSingleton<IOgcPerformanceLogger, CsvOgcPerformanceLogger>();
            }

            if (configuration[ApiConfigKeys.LoggingLogUsage]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IUsagePerformanceLogger, CsvUsagePerformaceLogger>();
                services.AddSingleton<IDatalinqPerformanceLogger, CsvDatalinqPerformanceLogger>();
            }

            services.AddSingleton<IExceptionLogger, FileExceptionLogger>();
            services.AddSingleton<IWarningsLogger, FileWarningsLogger>();
        }
        else if (loggingType == "microsoft")
        {
            if (configuration[ApiConfigKeys.LoggingLogPerformance]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IGeoServicePerformanceLogger, MicrosoftGeoServicePerformanceLogger>();
            }
            if (configuration[ApiConfigKeys.LoggingLogUsage]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IUsagePerformanceLogger, MicrosoftUsagePerformanceLogger>();
            }
            if (configuration[ApiConfigKeys.LoggingLogOgcPerformance]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IOgcPerformanceLogger, MicrosoftOgcPerformanceLogger>();
            }
            if (configuration[ApiConfigKeys.LoggingLogDataLinq]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                services.AddSingleton<IDatalinqPerformanceLogger, MicrosoftDatalinqPerformanceLogger>();
            }

            if (configuration[ApiConfigKeys.LoggingLogWarnings]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true)
            {
                services.AddSingleton<IWarningsLogger, MicrosoftWarningsLogger>();
            }
            if (configuration[ApiConfigKeys.LoggingLogExceptions]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true)
            {
                services.AddSingleton<IExceptionLogger, MicrosoftExceptionLogger>();
            }
        }

        if (configuration[ApiConfigKeys.LoggingLogServiceRequests]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
            && !String.IsNullOrEmpty(configuration[ApiConfigKeys.LogPath])
            && configuration[ApiConfigKeys.Trace] == "true")
        {
            services.AddSingleton<IGeoServiceRequestLogger, SimpleServiceRequestLogger>(
                _ => new SimpleServiceRequestLogger(configuration[ApiConfigKeys.LogPath], 50));
        }


        services.IfServiceNotRegistered<IGeoServicePerformanceLogger>(() => services.AddSingleton<IGeoServicePerformanceLogger, NullGeoServicePerformanceLogger>());
        services.IfServiceNotRegistered<IOgcPerformanceLogger>(() => services.AddSingleton<IOgcPerformanceLogger, NullGeoServicePerformanceLogger>());
        services.IfServiceNotRegistered<IUsagePerformanceLogger>(() => services.AddSingleton<IUsagePerformanceLogger, NullUsagePerformanceLogger>());
        services.IfServiceNotRegistered<IDatalinqPerformanceLogger>(() => services.AddSingleton<IDatalinqPerformanceLogger, NullUsagePerformanceLogger>());
        services.IfServiceNotRegistered<IWarningsLogger>(() => services.AddSingleton<IWarningsLogger, NullWarningsLogger>());
        services.IfServiceNotRegistered<IExceptionLogger>(() => services.AddSingleton<IExceptionLogger, NullExceptionLogger>());
        services.IfServiceNotRegistered<IGeoServiceRequestLogger>(() => services.AddSingleton<IGeoServiceRequestLogger, NullLogger>());

        return services;
    }


    #region DataLinq

    static public IServiceCollection AddDatalinqServices(this IServiceCollection services,
                                                         IConfiguration configuration,
                                                         IEnumerable<ICustomStartupService> customStartupServices)
    {
        var configService = configuration.ToServiceInstanceWithHostEnvironmentParser();

        services.AddDataLinqHostAuthenticatoinService<DatalinqHostAuthenticationService>();
        services.AddDataLinqServices<FileSystemPersistanceService,
                                     E.DataLinq.Core.Services.Crypto.CryptoService,
                                     UrlHelperService>(
            dataLinqOptions: config =>
            {
                List<string> customCssUrls = new List<string>(new[]
                {
                $"~/content/styles/default.css?{WebGISVersion.CssVersion}",
                $"~/content/Site.css?{WebGISVersion.CssVersion}"
            });
                customCssUrls.AddRange(configuration.DataLinqCustomCssUrls(WebGISVersion.CssVersion));

                var customReportJavascriptUrls = new List<string>(new[]
                {
                $"src=~/scripts/api/api.min.js?{WebGISVersion.JsVersion};id=webgis-api-script",
                $"~/scripts/api/api-ui.min.js?{WebGISVersion.JsVersion}",
                $"~/scripts/api/datalinq-overrides.js?{WebGISVersion.JsVersion}"
            });
                customReportJavascriptUrls.AddRange(configuration.DataLinqCustomJavaScriptUrls(WebGISVersion.JsVersion));

                config.CustomReportCssUrls = customCssUrls.ToArray();
                config.CustomReportJavascriptUrls = customReportJavascriptUrls.ToArray();

                if (Enum.TryParse<E.DataLinq.Core.DataLinqEnvironmentType>(configuration.DataLinqEnvrionment(), true, out var environmentType))
                {
                    config.EnvironmentType = environmentType;
                }
                else
                {
                    config.EnvironmentType = E.DataLinq.Core.DataLinqEnvironmentType.Default;
                }

                config.EngineId = configuration.DataLinqEngineId().ToLowerInvariant() switch
                {
                    "legacy" => RazorEngineIds.LegacyEngine,
                    _ => RazorEngineIds.DataLinqLanguageEngineRazor
                };

                if (config.EngineId == RazorEngineIds.DataLinqLanguageEngineRazor)
                {
                    // add assemblies to use DataLinqHelper Extensions
                    config.AddAssemblyReferene(typeof(IDataLinqHelper).Assembly);
                    config.AddAssemblyReferene(typeof(DataLinqHelperExtensions).Assembly);

                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "datalinq");
                    config.TempPath = tempPath;

                    Console.WriteLine($"Datalinq Cache TempPath: {tempPath}");
                }

                config.AddRazorNamespace("Api.Core.AppCode.Extensions.Razor");
                foreach (var razorNamespace in configuration.DataLinqAddRazorNamesapces())
                {
                    config.AddRazorNamespace(razorNamespace);
                }

                config.AddToRazorWhiteList(configuration.DataLinqRazorWhiteListItems());
                config.AddToRazorBlackList(configuration.DataLinqRazorBlackListItems());

                config.AddSupportedEndPointTypes<WebGISCustomEndPointTypes>();
            },
            persistanceOptions: config =>
            {
                config.ConnectionString = $"{configService[ApiConfigKeys.StorageRootPath]}/webgis.tools.datalinq.endpoints";
                if (Enum.TryParse<EncryptionLevel>(configuration.DataLinqApiEncryptionLevel(), true, out EncryptionLevel encryptionLevel))
                {
                    config.SecureStringEncryptionLevel = encryptionLevel;
                }
                else
                {
                    config.SecureStringEncryptionLevel = EncryptionLevel.DefaultStaticEncryption;
                }
            },
            cryptoOptions: config =>
            {
                if (!customStartupServices.ImplementsCryptographyService(configuration))
                {
                    var cryptoOptions = new E.Standard.Security.Cryptography.Services.CryptoServiceOptions();
                    cryptoOptions.LoadOrCreate(
                                configuration[ApiConfigKeys.SharedCryptoKeysPath].AppendToDefaultConfigPaths().ToArray(),
                                typeof(CustomPasswords),
                                customLegacySalt:
                                    !String.IsNullOrEmpty(configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                        ? Convert.FromBase64String(configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-salt"])
                                        : null,
                                legacyDataLinqDefaultPassword: configuration[$"{ApiConfigKeys.ConfigurationSectionName}:security:use-legacy-datalinq-crypto-key"]);

                    config.DefaultPassword = cryptoOptions.DefaultPasswordDataLinq.OrTake(cryptoOptions.DefaultPassword);
                    config.HashBytesSalt = cryptoOptions.HashBytesSalt;
                    config.Saltsize = cryptoOptions.Saltsize;
                }
                else
                {
                    var cryptoOptions = customStartupServices.GetCryproServiceOptions(configuration);
                    if (cryptoOptions != null)
                    {
                        config.DefaultPassword = cryptoOptions.DefaultPassword;
                        config.HashBytesSalt = cryptoOptions.HashBytesSalt;
                        config.Saltsize = cryptoOptions.Saltsize;
                    }
                }
            });

        services.AddDefaultDatalinqEngines(configuration.GetSection(ApiConfigKeys.ToKey("datalinq:SelectEngines")));
        services.AddDataLinqSelectEngine<ApiEngine>();
        services.AddDataLinqSelectEngine<GeoJsonEngine>();
        services.AddDataLinqSelectEngine<GeoRssEngine>();
        services.AddDataLinqDbFactoryProvider<E.DataLinq.Engine.SqlServer.SqlClientDbFactoryProvider>();
        services.AddDataLinqDbFactoryProvider<E.DataLinq.Engine.MsSqlServer.MsSqlClientDbFactoryProvider>();
        services.AddDataLinqDbFactoryProvider<E.DataLinq.Engine.Postgres.DbFactoryProvider>();
        services.AddDataLinqDbFactoryProvider<E.DataLinq.Engine.SQLite.DbFactoryProvider>();
        services.AddDataLinqDbFactoryProvider<E.DataLinq.Engine.OracleClient.DbFactoryProvider>();

        services.AddSingleton<IDbFactoryProviderConnectionStringModifyService, DataLinqDbFactoryProviderConnectionStringModifyService>();

        services.AddTransient<IEngineFieldParserService, DataLinqLocationFieldParserService>();
        services.AddTransient<ISelectResultProvider, DataLinqGeoJsonSelectResultProvider>();

        services.AddTransient<IExpectableUserRoleNamesProvider, DataLinqExpectableUserRoleNamesProvider>();
        services.AddTransient<E.DataLinq.Web.Services.Abstraction.IDataLinqLogger, DataLinqLogger>();

        services.AddTransient<IDataLinqCustomSelectArgumentsProvider, DataLinqRoleParameterSelectArgumentsProvider>();

        return services;
    }

    static public IServiceCollection AddDatalinqCodeApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddTransient<E.DataLinq.Web.Services.Abstraction.IRoutingEndPointReflectionProvider, DataLinqRoutingEndPointReflectionProvider>()
            .AddDataLinqCodeApiServices<DataLinqCodeIdentityProvider>(config =>
            {
                config.DataLinqCodeClients = configuration.DataLinqCodeApiClients()?.ToArray();
            });
    }

    #endregion
}
