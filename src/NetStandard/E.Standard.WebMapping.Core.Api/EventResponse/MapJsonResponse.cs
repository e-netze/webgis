namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class MapJsonResponse : ApiEventResponse
{
    public string SerializationMapJson { get; set; }
    public string Master0Json { get; set; }
    public string Master1Json { get; set; }

    public string HtmlMetaTags { get; set; }

    public string MapDescription { get; set; }

    public string MapTitle { get; set; }

    public override void CloneTo(ApiEventResponse to)
    {
        base.CloneTo(to);

        if (to is MapJsonResponse)
        {
            ((MapJsonResponse)to).SerializationMapJson = this.SerializationMapJson;
            ((MapJsonResponse)to).Master0Json = this.SerializationMapJson;
            ((MapJsonResponse)to).Master1Json = this.SerializationMapJson;

            ((MapJsonResponse)to).HtmlMetaTags = this.HtmlMetaTags;

            ((MapJsonResponse)to).MapDescription = this.MapDescription;
        }
    }
}
