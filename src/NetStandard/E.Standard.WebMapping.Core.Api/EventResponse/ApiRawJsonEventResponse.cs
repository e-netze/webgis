namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiRawJsonEventResponse : ApiEventResponse
{
    public ApiRawJsonEventResponse(object rowJsonObject)
    {
        this.RawJsonObject = rowJsonObject;
    }

    public object RawJsonObject { get; set; }
}
