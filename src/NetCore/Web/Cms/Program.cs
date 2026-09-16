using Cms;
using Cms.AppCode.Extensions.DependencyInjection;

using E.Standard.Configuration;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebApp.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// _config/logging.env (optional): "KEY=VALUE" per line, loaded as real process environment
// variables before the host builder captures them - see EnvFileLoader for details. Must run
// before WebApplication.CreateBuilder(args), which is what actually snapshots environment
// variables into IConfiguration.
EnvFileLoader.LoadConfigDirectoryEnvFile("logging.env");

var builder = WebApplication
                    .CreateBuilder(args)
                    .SetAppLocalization(false)
                    .PerformWebgisCmsSetup(args)
                    .AddWebgisCmsConfiguration()
                    .AddLoggingEngine();

#if DEBUG // aspire
builder.AddServiceDefaults();
#else
// Service discovery / HTTP-client resilience (the rest of AddServiceDefaults) are Aspire-dev
// concerns, but OpenTelemetry (logging/metrics/tracing, incl. WebGIS.GeoServices) is a real
// production feature: it stays inert unless OTEL_EXPORTER_OTLP_ENDPOINT is configured, so
// enabling it here lets an admin point WebGIS at any OTLP-compatible backend (Grafana, Elastic,
// Seq, Jaeger, ...) without a rebuild.
builder.ConfigureOpenTelemetry();
builder.AddDefaultHealthChecks();
#endif

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

//#if DEBUG // aspire
// /health & /alive endpoints 
app.MapDefaultEndpoints();
//#endif

startup.Configure(app,
        app.Environment,
        app.Services.GetRequiredService<IOptionsMonitor<ApplicationSecurityConfig>>(),
        app.Services.GetRequiredService<ICryptoService>(),
        app.Services.GetRequiredService<ILogger<Startup>>());

app.Run();