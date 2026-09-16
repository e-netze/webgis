using System.Globalization;

using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.WebApp.Abstraction;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace E.Standard.WebApp.Extensions;

static public class WebApplicationExtensions
{
    static public IEndpointRouteBuilder RegisterApiEndpoints(
                this IEndpointRouteBuilder app,
                Type assemblyType)
    {
        var apiEndpointTypes = assemblyType.Assembly.GetTypes()
            .Where(t =>
                 typeof(IApiEndpoint).IsAssignableFrom(t) &&
                 t.IsClass);

        Console.WriteLine("Register ApiEndpoints");
        Console.WriteLine("=====================");

        foreach (var apiEndpointType in apiEndpointTypes)
        {
            try
            {
                Console.Write($"Register ApiEndpoint {apiEndpointType}");

                var apiEndpoint = Activator.CreateInstance(apiEndpointType) as IApiEndpoint;

                apiEndpoint?.Register(app);

                Console.WriteLine("...succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"...failed: {ex.Message}");
            }
        }

        Console.WriteLine("...done");

        return app;
    }

    static public TBuilder SetAppLocalization<TBuilder>(this TBuilder builder, bool setRequestLocalization = false)
        where TBuilder : IHostApplicationBuilder
    {
        try
        {
            var cultureName = builder.Configuration["Localization:DefaultCulture"];

            if (String.IsNullOrEmpty(cultureName))
            {
                Console.WriteLine($"INFO: Localization:DefaultCulture not set in configuraion. System default culture is used: {CultureInfo.CurrentCulture.Name}");
                return builder;
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);
            if (culture is null)
            {
                throw new Exception($"Unkown culture {cultureName}. Please set a correct culture in Localization:DefaultCulture");
            }

            Console.WriteLine($"INFO: Set CultureInfo.DefaultThreadCurrentCulture: {culture.Name}");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (setRequestLocalization)
            {
                Console.WriteLine($"INFO: Set RequestLocalization: {culture.Name}");

                builder.Services.Configure<RequestLocalizationOptions>(options =>
                {
                    options.DefaultRequestCulture = new RequestCulture(culture);

                    options.SupportedCultures = new[] { culture };
                    options.SupportedUICultures = new[] { culture };

                    // avoid override culture with Browser-Language
                    options.RequestCultureProviders.Clear();
                });

                // Wenn Request Localization sollte in der Program.cs 
                // eventuell auch 
                //
                // var app = builder.Build();
                // app.UseRequestLocalization();
                //
                // damit das greift. Hatten wir bisher aber nicht, drum ist es wahrscheinlich nicht notwendig

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("*****************************************************************");
            Console.WriteLine("EXCEPTION: @SetAppLocalization");
            Console.WriteLine(ex.Message);
            Console.WriteLine("*****************************************************************");
        }

        return builder;
    }

    /// <summary>
    /// Wires up WebGIS's application/host logging engine:
    /// <list type="bullet">
    /// <item>an optional "_config/logging.json" file, so administrators who only have access to
    /// the mounted "_config" directory (a common Kubernetes ConfigMap-volume setup) can
    /// configure Logging/Serilog/OpenTelemetry (OTEL_* keys) without touching environment
    /// variables or appsettings.json - see <c>AddConfigDirectoryJsonFile</c>;</item>
    /// <item>Serilog as an additional, config-driven <c>ILogger</c> provider (SQL Server/
    /// PostgreSQL sinks, etc. via a "Serilog" configuration section), running alongside
    /// whatever other providers are already registered (e.g. OpenTelemetry, via
    /// <c>ConfigureOpenTelemetry()</c>) rather than replacing them. Without a "Serilog" section,
    /// this is a no-op beyond the existing console logging.</item>
    /// </list>
    /// This is the single place that decides which logging framework WebGIS uses - if Serilog is
    /// ever replaced (or an additional engine is added), only this method needs to change;
    /// callers (Program.cs) don't need to know the implementation detail. Requires
    /// <see cref="WebApplicationBuilder"/> rather than the generic <see cref="IHostApplicationBuilder"/>
    /// used elsewhere in this class, since Serilog's deferred, DI-aware bootstrap
    /// (<c>ReadFrom.Services(...)</c>) needs <see cref="WebApplicationBuilder.Host"/>.
    /// <para>
    /// <c>UseSerilog()</c> replaces <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>
    /// with a Serilog-backed one, which means <c>IsEnabled()</c>/filtering for every log call is
    /// decided by Serilog's own minimum level - the standard ASP.NET Core "Logging:LogLevel"
    /// section (which callers, and <c>docs/logging.md</c>, document as the way to control log
    /// levels) is otherwise silently ignored. <see cref="ApplyMicrosoftLogLevels"/> bridges
    /// "Logging:LogLevel" into Serilog's <c>MinimumLevel</c> so that section keeps working as
    /// documented; an explicit "Serilog:MinimumLevel" entry for the same category still wins.
    /// </para>
    /// </summary>
    static public WebApplicationBuilder AddLoggingEngine(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddConfigDirectoryJsonFile("logging.json");

        // writeToProviders: true keeps already-registered ILoggerProviders (e.g. the
        // OpenTelemetry one from ConfigureOpenTelemetry()) working side-by-side, since
        // UseSerilog() would otherwise clear all other providers.
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            ApplyMicrosoftLogLevels(loggerConfiguration, context.Configuration);

            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        },
        writeToProviders: true);

        return builder;
    }

    /// <summary>
    /// Bridges the standard <c>Microsoft.Extensions.Logging</c> "Logging:LogLevel" configuration
    /// section (<c>Default</c> + per-category overrides, e.g.
    /// <c>Logging:LogLevel:My.Namespace.MyType = Trace</c>) into Serilog's <c>MinimumLevel</c>,
    /// so it keeps controlling log levels even though <see cref="AddLoggingEngine"/> makes
    /// Serilog - not the ASP.NET Core logging pipeline - responsible for level filtering.
    /// <c>Microsoft.Extensions.Logging.LogLevel</c> and <see cref="LogEventLevel"/> share the
    /// same 0-5 ordinal range (Trace/Verbose .. Critical/Fatal), so the cast is exact; the only
    /// mismatch is <c>LogLevel.None</c> (6), which has no Serilog equivalent and is skipped.
    /// Must run before <c>ReadFrom.Configuration()</c> so an explicit "Serilog:MinimumLevel"
    /// entry for the same category - applied right after - always wins; per-category overrides
    /// already present under "Serilog:MinimumLevel:Override" are skipped here so
    /// <c>ReadFrom.Configuration()</c> doesn't throw for setting the same override twice.
    /// </summary>
    static private void ApplyMicrosoftLogLevels(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var logLevelSection = configuration.GetSection("Logging:LogLevel");
        if (!logLevelSection.Exists())
        {
            return;
        }

        var serilogOverrides = configuration.GetSection("Serilog:MinimumLevel:Override");

        foreach (var entry in logLevelSection.GetChildren())
        {
            if (!Enum.TryParse<LogLevel>(entry.Value, ignoreCase: true, out var msLevel) || msLevel == LogLevel.None)
            {
                continue;
            }

            var serilogLevel = (LogEventLevel)(int)msLevel;

            if (string.Equals(entry.Key, "Default", StringComparison.OrdinalIgnoreCase))
            {
                loggerConfiguration.MinimumLevel.Is(serilogLevel);
            }
            else if (!serilogOverrides.GetSection(entry.Key).Exists())
            {
                loggerConfiguration.MinimumLevel.Override(entry.Key, serilogLevel);
            }
        }
    }
}
