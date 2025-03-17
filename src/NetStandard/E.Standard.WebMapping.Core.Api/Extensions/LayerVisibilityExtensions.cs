using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class LayerVisibilityExtensions
{
    static public LayerVisibility TryAddService(this LayerVisibility layerVisibility, string serviceId)
    {
        if (layerVisibility != null && !layerVisibility.ContainsKey(serviceId))
        {
            layerVisibility.Add(serviceId, new Dictionary<string, bool>());
        }

        return layerVisibility;
    }

    static public LayerVisibility AddVisibleLayers(this LayerVisibility layerVisibility, string serviceId, params string[] layerIds)
        => layerVisibility.AddVisibleLayers(serviceId, (IEnumerable<string>)layerIds);

    static public LayerVisibility AddVisibleLayers(this LayerVisibility layerVisibility, string serviceId, IEnumerable<string> layerIds)
    {
        if (layerVisibility != null && !string.IsNullOrEmpty(serviceId) && layerIds != null && layerIds.Any())
        {
            layerVisibility.TryAddService(serviceId);

            foreach (var layerId in layerIds.Distinct())
            {
                layerVisibility[serviceId].Add(layerId, true);
            }
        }

        return layerVisibility;
    }
}
