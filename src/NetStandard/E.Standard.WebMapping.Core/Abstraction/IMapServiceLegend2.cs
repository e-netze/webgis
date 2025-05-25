using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceLegend2
{
    //IEnumerable<LayerLegendItem> GetLayerLegendItems(string layerId);
    Task<IEnumerable<LayerLegendItem>> GetLayerLegendItemsAsync(string layerId, IRequestContext requestContext);
}
