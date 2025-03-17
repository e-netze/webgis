using E.Standard.WebMapping.Core.Filters;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayer3 : ILayer
{
    Task<int> FeaturesCountOnly(QueryFilter filter, IRequestContext requestContext);
}
