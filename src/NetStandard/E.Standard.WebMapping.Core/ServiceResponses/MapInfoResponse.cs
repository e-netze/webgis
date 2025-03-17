namespace E.Standard.WebMapping.Core.ServiceResponses;

public class MapInfoResponse : HtmlResponse
{
    public MapInfoResponse(int index, string serviceID, string html)
        : base(index, serviceID, html)
    {
    }
    public MapInfoResponse(int index, string serviceID, string html, string jScript)
        : base(index, serviceID, html, jScript)
    {
    }
}
