using Api.AppCode.Mvc.Wrapper;
using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Drawing;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Api.Core.AppCode.Services;

public class ApiGlobalsService
{
    public ApiGlobalsService(ConfigurationService config,
                             ApiConfigurationService apiConfig,
                             IWebHostEnvironment environment,
                             IEnumerable<ICustomApiService> customServices = null)
    {
        var contextWrapper = new CurrentHttpContextWrapper(environment);

        string appServerSideConfigurationsPath = apiConfig.ServiceSideConfigurationPath;
        if (string.IsNullOrWhiteSpace(appServerSideConfigurationsPath))
        {
            appServerSideConfigurationsPath = new DirectoryInfo(environment.ContentRootPath).Parent.FullName;
        }

        //E.Standard.CMS.Core.CmsDocument.UseAuthExclusives = config.UseAuthExclusives();

        ApiGlobals.IsDevelopmentEnvironment = environment.IsDevelopment();

        ApiGlobals.AppConfigPath = System.IO.Path.Combine(appServerSideConfigurationsPath, "config");
        ApiGlobals.AppEtcPath = System.IO.Path.Combine(appServerSideConfigurationsPath, "etc");

        ApiGlobals.SRefStore = new SpatialReferenceStore(ApiGlobals.AppEtcPath);
        CoreApiGlobals.SRefStore = ApiGlobals.SRefStore;
        E.Standard.WebGIS.CMS.Globals.SpatialReferences = ApiGlobals.SRefStore.SpatialReferences;

        ApiGlobals.AppRootPath = environment.ContentRootPath;
        ApiGlobals.AppPluginsPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);    //  
        ApiGlobals.AppAssemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);   // ist in development umgebung nicht gleich dem AppRootPath (produktiv sollte es das gleiche sein)
        ApiGlobals.WWWRootPath = environment.WebRootPath;

        SpatialReferenceCollection.p4DatabaseConnection = config.Pro4DatabaseConnectionString();

        WebApp.ConsoleLogging = config.UseConsoleLogging();

        foreach (string storageTranslation in config.GetPathsStartWith(ApiConfigKeys.ToKey("storage-translation-")))
        {
            string val = config[storageTranslation];
            if (!String.IsNullOrWhiteSpace(val))
            {
                CoreApiGlobals.StorageToolIdTranslation.Add(
                    storageTranslation.Substring(ApiConfigKeys.ToKey("storage-translation-").Length),
                    val);
            }
        }

        customServices.InitGlobals(config);

        ApiGlobals.LogPath = config.LogPath();
        ApiGlobals.LogPerformanceColumns = config.LogPerformanceColumns();

        GraphicsEngines.Init(
                config.GraphicsEngine()?.ToLower() switch
                {
                    "skia" => GraphicsEngines.Engines.Skia,
                    "gdiplus" => GraphicsEngines.Engines.GdiPlus,
                    _ => GraphicsEngines.Engines.SystemDefault
                }
            );
    }
}
