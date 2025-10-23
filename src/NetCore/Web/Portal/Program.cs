using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal;
using Portal.Core.AppCode.Extensions.DependencyInjection;
using System.Collections.Generic;


var builder = WebApplication
                    .CreateBuilder(args)
                    .PerformWebgisPortalSetup(args)
                    .AddWebgisPortalConfiguration();

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
                  app.Services.GetRequiredService<ConfigurationService>(),
                  app.Services.GetRequiredService<KeyValueCacheService>(),
                  app.Services.GetRequiredService<ICryptoService>(),
                  app.Services.GetRequiredService<IOptionsMonitor<ApplicationSecurityConfig>>(),
                  app.Services.GetRequiredService<ILogger<Startup>>(),
                  app.Services.GetRequiredService<IMarkdownLocationInitializer>(),
                  app.Services.GetService<IEnumerable<ICustomPortalAuthenticationMiddlewareService>>(),
                  app.Services.GetService<IEnumerable<ICustomPortalSecurityService>>());

app.Run();