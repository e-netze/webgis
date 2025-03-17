using Cms;
using Cms.AppCode.Extensions.DependencyInjection;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = WebApplication
                    .CreateBuilder(args)
                    .PerformWebgisCmsSetup(args)
                    .AddWebgisCmsConfiguration();

#if DEBUG // aspire
builder.AddServiceDefaults();
#endif

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

#if DEBUG // aspire
app.MapDefaultEndpoints();
#endif

startup.Configure(app,
        app.Environment,
        app.Services.GetRequiredService<IOptionsMonitor<ApplicationSecurityConfig>>(),
        app.Services.GetRequiredService<ICryptoService>(),
        app.Services.GetRequiredService<ILogger<Startup>>());

app.Run();