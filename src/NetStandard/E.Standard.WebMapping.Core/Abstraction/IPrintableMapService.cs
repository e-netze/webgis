using E.Standard.WebMapping.Core.ServiceResponses;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IPrintableMapService
{
    //ServiceResponse GetPrintMap();
    Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext);
}
