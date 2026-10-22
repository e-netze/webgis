using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Models;

namespace E.Standard.WebMapping.Core.Abstraction;

/// <summary>
/// Layers that can calculate field statistics (count, min, max, avg, sum, value groups) on the server
/// for all features matching a filter (where + spatial filter), eg. ArcGIS outStatistics.
/// </summary>
public interface ILayerStatisticsQuery
{
    bool SupportsStatistics { get; }

    Task<LayerStatistics> QueryStatisticsAsync(QueryFilter filter, LayerStatisticsRequest request, IRequestContext requestContext);
}
