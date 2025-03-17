using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Extensions;

static public class LayerPropertiesExtensions
{
    static public ILayerProperties GetLayerProperties(this IEnumerable<ILayerProperties> layerPropertiesList, string layerId)
    {
        return layerPropertiesList?.Where(l => l?.Id == layerId)
                                   .FirstOrDefault();
    }

    static public string LegendAliasname(this IEnumerable<ILayerProperties> layerPropertiesList, string layerId)
    {
        return layerPropertiesList?.GetLayerProperties(layerId)?.LegendAliasname ?? String.Empty;
    }

    static public string Aliasname(this IEnumerable<ILayerProperties> layerPropertiesList, string layerId)
    {
        return layerPropertiesList?.GetLayerProperties(layerId)?.Aliasname ?? String.Empty;
    }

    static public bool ShowInLegend(this IEnumerable<ILayerProperties> layerPropertiesList, string layerId)
    {
        var layerProperties = layerPropertiesList?.GetLayerProperties(layerId);

        if (layerProperties == null)
        {
            return true;
        }

        return layerProperties.ShowInLegend;
    }
}
