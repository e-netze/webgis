using System;
using System.Collections.Specialized;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiRawBytesEventResponse : ApiEventResponse
{
    public ApiRawBytesEventResponse(byte[] bytes, string contentType)
    {
        this.RawBytes = bytes;
        this.ContentType = contentType;
    }

    public string ContentType { get; set; }
    public byte[] RawBytes { get; set; }

    public NameValueCollection Headers { get; set; }

    public void AddEtag(DateTime expires)
    {
        if (Headers == null)
        {
            Headers = new NameValueCollection();
        }

        Headers.Add("ETag", expires.Ticks.ToString());
        Headers.Add("Last-Modified", DateTime.UtcNow.ToString("R"));
        Headers.Add("Expires", expires.ToString("R"));
        Headers.Add("Cache-Control", "private, max-age=" + (int)(new TimeSpan(24, 0, 0)).TotalSeconds);
    }
}
