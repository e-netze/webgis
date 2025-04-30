using Microsoft.AspNetCore.Builder;
using System;

namespace E.Standard.Custom.Core.Extensions;
static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder UseWebgisAppBasePath(this IApplicationBuilder app)
    {
        var basePath = Environment.GetEnvironmentVariable("WEBGIS_APP_BASE_PATH");
        if (!String.IsNullOrEmpty(basePath))
        {
            app.UsePathBase(basePath);
            Console.WriteLine($"Info: Set Base Path: {basePath}"); ;
        }

        return app;
    }
}
