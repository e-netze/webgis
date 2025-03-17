using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayerDistinctQuery
{
    Task<IEnumerable<string>> QueryDistinctValues(IRequestContext requestContext, string field, string where = "", string orderBy = "", int featureLimit = 0);
}
