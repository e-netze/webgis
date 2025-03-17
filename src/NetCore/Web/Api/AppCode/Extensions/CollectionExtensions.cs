#nullable enable

using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Extensions;

static internal class CollectionExtensions
{
    async static public Task<OrderedDictionary<string, IMapService>> ResolveCollections(
            this OrderedDictionary<string, IMapService> services,
            CacheService cache,
            IMap map,
            CmsDocument.UserIdentification ui,
            IUrlHelperService urlHelper)
    {
        var serviceCollectionUrl = services.FirstCollectionService();

        if (string.IsNullOrEmpty(serviceCollectionUrl))
        {
            return services;
        }

        int index = services.IndexOf(serviceCollectionUrl);
        var serviceCollection = (services[serviceCollectionUrl] as IMapServiceCollection)!;

        services.Remove(serviceCollectionUrl);

        foreach (var item in serviceCollection.Items.Reverse())
        { 
            if(item.Url == serviceCollectionUrl)
            {
                throw new System.Exception($"Self-Recursive collection detected. {serviceCollectionUrl} contains {item.Url}");
            }

            var service = await cache.GetService(item.Url, map, ui, urlHelper);

            if (service is not null)
            {
                if (item.LayerVisibility == E.Standard.WebMapping.Core.MapServiceLayerVisibility.AllVisible)
                {
                    foreach (var layer in service.Layers)
                    {
                        layer.Visible = true;
                    }
                }
                else if(item.LayerVisibility == E.Standard.WebMapping.Core.MapServiceLayerVisibility.AllInvisible)
                {
                    foreach (var layer in service.Layers)
                    {
                        layer.Visible = false;
                    }
                }
            }

            services.Insert(index++, item.Url, service!);  // also add service == null => unknown service or not authorized service...
        }

        return await services.ResolveCollections(cache, map, ui, urlHelper);
    }

    static private OrderedDictionary<string, IMapService> ResolveCollectionsFast(
            this OrderedDictionary<string, IMapService> services,
            CacheService cache,
            CmsDocument.UserIdentification ui)
    {
        var serviceCollectionUrl = services.FirstCollectionService();

        if (string.IsNullOrEmpty(serviceCollectionUrl))
        {
            return services;
        }

        int index = services.IndexOf(serviceCollectionUrl);
        var serviceCollection = (services[serviceCollectionUrl] as IMapServiceCollection)!;

        services.Remove(serviceCollectionUrl);

        foreach (var item in serviceCollection.Items.Reverse())
        {
            if (item.Url == serviceCollectionUrl)
            {
                throw new System.Exception($"Self-Recursive collection detected. {serviceCollectionUrl} contains {item.Url}");
            }

            var service = cache.GetOriginalServiceFast(item.Url, ui);

            if (service != null)
            {
                services.Insert(index++, item.Url, service!);
            }
        }

        return services.ResolveCollectionsFast(cache, ui);
    }

    static public string[] ResolveCollectionServiceUrls(
                this IMapService service, 
                CacheService cache,
                CmsDocument.UserIdentification ui)
    {
        var childServices = new OrderedDictionary<string, IMapService>()
                    { {service.Url, service } };

        childServices = childServices.ResolveCollectionsFast(cache, ui);

        return childServices.Keys.ToArray();
    }

    static public bool HasCollectionServices(this IDictionary<string, IMapService> services)
        => services?.Values?.Any(service => service is IMapServiceCollection) == true;

    static public string? FirstCollectionService(this OrderedDictionary<string, IMapService> services)
        => services?.FirstOrDefault(kv => kv.Value is IMapServiceCollection).Key;
}
