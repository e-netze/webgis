using E.Standard.WebApp.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

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
            if(culture is null)
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
}
