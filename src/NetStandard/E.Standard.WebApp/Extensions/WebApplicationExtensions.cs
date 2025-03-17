using E.Standard.WebApp.Abstraction;
using Microsoft.AspNetCore.Routing;

namespace E.Standard.WebApp.Extensions;

static public class WebApplicationExtensions
{
    static public IEndpointRouteBuilder RegisterApiEndpoints(
                this IEndpointRouteBuilder app,
                Type assemblyType)
    {
        var apiEndpointTypes = assemblyType.Assembly.GetTypes()
            .Where(t => typeof(IApiEndpoint).IsAssignableFrom(t));

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
}
