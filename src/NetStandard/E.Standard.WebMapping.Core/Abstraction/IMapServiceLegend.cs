using E.Standard.WebMapping.Core.ServiceResponses;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceLegend
{
    bool ShowServiceLegendInMap { get; set; }
    bool LegendVisible { get; set; }
    //ServiceResponse GetLegend();
    Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext);
    LegendOptimization LegendOptMethod { get; set; }
    double LegendOptSymbolScale { get; set; }
    string FixLegendUrl { get; set; }
}
