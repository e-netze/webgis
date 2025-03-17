using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiFeaturesEventResponse : ApiEventResponse
{
    public ApiFeaturesEventResponse(IApiButton tool = null) : base()
    {
        this.ZoomToResults = true;
        if (tool != null)
        {
            this.QueryToolId = tool.GetType().ToToolId();
        }
    }

    public ApiFeaturesEventResponse(ApiEventResponse response)
        : this()
    {
        if (response != null)
        {
            response.CloneTo(this);
        }
    }

    public FeatureCollection Features { get; set; }
    public FeatureCollection FeaturesForLinks { get; set; }
    public SpatialReference FeatureSpatialReference { get; set; }
    public IQueryBridge Query { get; set; }
    public ApiQueryFilter Filter { get; set; }
    public bool ZoomToResults { get; set; }
    public bool SelectResults { get; set; }
    public bool AppendHoverShapes { get; set; }

    public FeatureResponseType FeatureResponseType { get; set; }

    public ApiToolEventArguments.ApiToolEventClick ClickEvent { get; set; }

    public string QueryToolId { get; }

    public string CustomSelectionId { get; set; }
}
