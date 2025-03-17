using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public class ApiSpatialFilter : ApiQueryFilter
{
    public ApiSpatialFilter()
        : base()
    {

    }

    internal ApiSpatialFilter(ApiSpatialFilter filter)
        : base(filter)
    {
        this.QueryShape = filter.QueryShape;
        this.FilterSpatialReference = filter.FilterSpatialReference;
    }

    public Shape QueryShape { get; set; }

    public SpatialReference FilterSpatialReference { get; set; }

    public override ApiQueryFilter Clone()
    {
        var clone = new ApiSpatialFilter(this);
        return clone;
    }
}
