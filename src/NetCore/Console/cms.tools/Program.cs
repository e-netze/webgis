using cms.tools;
using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Extensions.DependencyInjection;
using E.Standard.Cms.Services;
using E.Standard.Cms.Services.Logging;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Extensions.Compare;
using E.Standard.Web.Extensions.DependencyInjection;
using E.Standard.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Authentication;

try
{
    var commandLine = new CommandLine(args);

    if (String.IsNullOrEmpty(commandLine.Command))
    {
        commandLine.Usage();

        return 0;
    }

    var contentPath = Path.GetDirectoryName(Environment.ProcessPath)!;
    var builder = Host.CreateDefaultBuilder();

    builder.ConfigureAppConfiguration(configBuilder =>
    {
        configBuilder.AddHostingEnviromentJsonConfiguration();
    });

    builder.ConfigureServices(services =>
    {
        #region HttpClient

        services.AddHttpContextAccessor();
        services.AddHttpClient("default")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)
                    SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
#pragma warning restore SYSLIB0039 // allow old protocols (tls, tls11)
                });


        services.AddHttpService<HttpService>(config =>
        {
            config.DefaultClientName = "default";
            //config.DefaultProxyClientName = webProxy != null ? "default-proxy" : null;
            //config.IgnoreProxyServers = Settings.WebProxyIgnores();
            //config.WebProxyInstance = webProxy;
        });

        #endregion

        services.AddHostingEnvironmentConfigValueParser();
        services.AddCmsConfigurationService(config =>
        {
            config.ContentPath = contentPath;
        });
        services.AddTransient<ICmsLogger, CmsNullLogger>();
        services.AddTransient<CmsItemInjectionPackService>();

        services.AddTransient<DeployService>();
        services.AddTransient<SolveWaringsService>();
        services.AddTransient<ClearCmsService>();
        services.AddTransient<ReloadSchemeService>();
        services.AddTransient<ExportCmsService>();
    });

    var app = builder.Build();

    var toolContext = new CmsToolContext()
    {
        CmsId = commandLine.CmsId,  // "webgis-release-default", // "webgis-custom",
        Deployment = commandLine.Deployment, // "default",
        ContentRootPath = contentPath,
        Username = commandLine.UserName.OrTake(Environment.UserName),
    };

    ICmsTool cmsTool = commandLine.Command.ToLowerInvariant() switch
    {
        "deploy" => app.Services.GetRequiredService<DeployService>(),
        "solve-warnings" => app.Services.GetRequiredService<SolveWaringsService>(),
        "clear" => app.Services.GetRequiredService<ClearCmsService>(),
        "reload-scheme" => app.Services.GetRequiredService<ReloadSchemeService>(),
        "export" => app.Services.GetRequiredService<ExportCmsService>(),
        _ => throw new ArgumentException($"Unknown cms tool '{commandLine.Command}'")
    };
    var console = new ConsoleOutput();

    if (!cmsTool.Run(toolContext, console))
    {
        throw new Exception("Cms Tool finished with errors");
    }

    if (console.HasFile)
    {
        foreach (var file in console.GetFiles())
        {
            var outputPath = commandLine.OutputPath.OrTake(Path.Combine(contentPath, "output"));
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            await File.WriteAllBytesAsync(Path.Combine(outputPath, file.FileName), file.Data);
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Exception:");
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);

    return 1;
}