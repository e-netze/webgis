using System.Threading.Tasks;

using E.Standard.WebMapping.Core.ServiceResponses;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IPrintableMapService
{
    //ServiceResponse GetPrintMap();
    Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext);
}
