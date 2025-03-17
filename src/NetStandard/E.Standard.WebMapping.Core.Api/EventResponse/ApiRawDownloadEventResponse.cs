using System;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiRawDownloadEventResponse : ApiRawBytesEventResponse
{
    public ApiRawDownloadEventResponse(string name, byte[] bytes)
        : base(bytes, String.Empty)
    {
        this.Name = name;
    }

    public string Name { get; set; }
}
