using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiCustomToolServiceProvider
{
    Task<IMapService> CreateCustomToolService(IBridge bridge, IMap map, string serviceId);
}
