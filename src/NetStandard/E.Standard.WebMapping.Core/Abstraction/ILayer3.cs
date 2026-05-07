using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Filters;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayer3 : ILayer
{
    Task<int> FeaturesCountOnly(QueryFilter filter, IRequestContext requestContext);
}
