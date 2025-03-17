using Cms.AppCode.Services;
using E.DataLinq.Code.Extensions.DependencyInjection;
using E.DataLinq.Code.Services;
using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Services.Logging;
using E.Standard.Configuration;
using E.Standard.Configuration.Extensions;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cms.AppCode.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddEssentialCmsServices(this IServiceCollection services)
    {
        return services
            .AddHttpContextAccessor()
            .AddTransient<UrlHelperService>();
    }

    static public IServiceCollection AddDataLinqCodeServices(this IServiceCollection services,
                                                             IConfiguration configuration,
                                                             IWebHostEnvironment environment,
                                                             IEnumerable<ICustomStartupService> customStartupServices)
    {
        services
            .AddTransient<IHostUrlHelper, UrlHelperService>()
            .AddDataLinqCodeService(
            configAction: config =>
            {
                config.ProjectWebSite = "https://docs.webgiscloud.com/de/datalinq/operation.html";

                var dataLinqInstances = new List<DataLinqCodeOptions.DataLinqInstance>();

                var appConfig = new JsonAppConfiguration("datalinq.config");
                if (appConfig.Exists)
                {
                    var dataLinqConfig = appConfig.Deserialize<DataLinqConfig>();
                    if (dataLinqConfig.Instances != null)
                    {
                        foreach (var instance in dataLinqConfig.Instances)
                        {
                            dataLinqInstances.Add(new DataLinqCodeOptions.DataLinqInstance()
                            {
                                Name = instance.Name,
                                Description = instance.Description,
                                LoginUrl = $"{instance.Url}/DataLinqAuth?redirect={{0}}",
                                LogoutUrl = $"{instance.Url}/DataLinqAuth/Logout?redirect={{0}}",
                                CodeApiClientUrl = instance.Url
                            });
                        }
                    }

                    config.UseAppPrefixFilters = dataLinqConfig.UseAppPrefixFilters;
                }

                config.DatalinqInstances = dataLinqInstances.ToArray();
            },
            cryptoOptions: config =>
            {
                if (!customStartupServices.ImplementsCryptographyService(configuration))
                {
                    string keyPath = environment.ContentRootPath;
                    try
                    {
                        var appConfig = new JsonAppConfiguration("cms.config");

                        if (appConfig.Exists)
                        {
                            var cmsConfig = appConfig.Deserialize<CmsConfig>();
                            if (!String.IsNullOrEmpty(cmsConfig.SharedCrptoKeysPath))
                            {
                                keyPath = cmsConfig.SharedCrptoKeysPath;
                            }
                        }
                    }
                    catch { }
                    var cryptoOptions = new E.Standard.Security.Cryptography.Services.CryptoServiceOptions();
                    cryptoOptions.LoadOrCreate(keyPath.AppendToDefaultConfigPaths().ToArray(), typeof(CustomPasswords));

                    config.DefaultPassword = cryptoOptions.DefaultPassword;
                    config.HashBytesSalt = cryptoOptions.HashBytesSalt;  // todo: legacy salt from config?
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

        return services;
    }

    static public IServiceCollection AddCmsLogging(this IServiceCollection services, string loggingConnectionString)
    {
        if (Path.IsPathRooted(loggingConnectionString))
        {
            services.Configure<CmsLoggerOptions>(config =>
            {
                config.ConnectionString = loggingConnectionString;
                config.MaxFileSizeBytes = 1024 * 1024 * 10; // 10MB
            });

            switch (Path.GetExtension(loggingConnectionString).ToLower())
            {
                case ".txt":
                case ".log":
                    return services.AddTransient<ICmsLogger, CmsFileLogger>();
                case ".csv":
                    return services.AddTransient<ICmsLogger, CmsCsvLogger>();
            }
        }

        services.AddTransient<ICmsLogger, CmsNullLogger>();

        return services;
    }
}
