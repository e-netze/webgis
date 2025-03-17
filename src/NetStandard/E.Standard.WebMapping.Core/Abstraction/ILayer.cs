using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayer : IClone<ILayer, IMapService>
{
    string Name { get; }
    string ID { get; }
    string GlobalID { get; }

    LayerType Type { get; }
    bool Visible { get; set; }
    double MinScale { get; }
    double MaxScale { get; }

    bool Queryable { get; }

    FieldCollection Fields { get; }

    string Filter { get; set; }
    //bool ShowInLegend { get; set; }

    Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext);

    string IdFieldName { get; }
    string ShapeFieldName { get; }

    string Description { get; }

    IMapService Service { get; }
}
