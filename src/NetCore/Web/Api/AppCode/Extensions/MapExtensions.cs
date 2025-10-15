using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.GeoServices.Graphics;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Extensions;

internal static class MapExtensions
{
    async static public Task<bool> AddNecessaryServices(this IMap map, IRequestContext requestContext)
    {
        if(map?.GraphicsContainer?.Any() == true 
            && map.Services?.Any(s => s is IGraphicsService) != true)
        {
            var graphicsService = new GraphicsService();
            await graphicsService.InitAsync(map, requestContext);
            map.Services.Add(graphicsService);
        }

        return true;
    }
}
