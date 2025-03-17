using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayer2 : ILayer
{
    Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext);
    Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext);
    Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext);
}
