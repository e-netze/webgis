using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Json;
using Microsoft.Extensions.Hosting;
using System;

namespace Cms.AppCode.Extensions.DependencyInjection;

static public class HostBuilderExtensions
{
    static public TBuilder PerformWebgisCmsSetup<TBuilder>(this TBuilder builder, string[] args)
    where TBuilder : IHostApplicationBuilder
    {
        JSerializer.SetEngine("Newtonsoft".Equals(builder.Configuration["JsonSerializationEngine"], StringComparison.OrdinalIgnoreCase)
            ? JsonEngine.NewtonSoft
            : JsonEngine.SytemTextJson);

        new AppCode.SimpleSetup().TrySetup(args);

        return builder;
    }

    static public TBuilder AddWebgisCmsConfiguration<TBuilder>(this TBuilder builder)
       where TBuilder : IHostApplicationBuilder
    {
        builder.Configuration.AddHostingEnviromentJsonConfiguration();

        return builder;
    }
}
