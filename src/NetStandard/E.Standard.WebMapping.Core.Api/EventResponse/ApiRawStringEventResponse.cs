using System;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiRawStringEventResponse : ApiEventResponse
{
    public ApiRawStringEventResponse(string rawString, string contentType)
    {
        this.RawString = rawString;
        this.ContentType = !String.IsNullOrWhiteSpace(contentType) ? contentType : "text/plain";
    }

    public string ContentType { get; set; }
    public string RawString { get; set; }
}
