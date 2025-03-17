namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiDynamicContentEventResponse : ApiEventResponse
{
    public ApiDynamicContentEventResponse()
        : base()
    {

    }

    public string Name { get; set; }
    public string Url { get; set; }
    public DynaimcContentType Type { get; set; }
}
