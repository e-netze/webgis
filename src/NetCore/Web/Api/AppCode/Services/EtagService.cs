using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;

namespace Api.Core.AppCode.Services;

public class EtagService
{
    protected bool HasIfNonMatch(HttpContext context) =>
        !String.IsNullOrEmpty(context.Request.Headers[HeaderNames.IfNoneMatch]);

    public bool IfMatch(HttpContext context)
    {
        try
        {
            if (HasIfNonMatch(context) == false)
            {
                return false;
            }

            var etag = long.Parse(context.Request.Headers[HeaderNames.IfNoneMatch]);

            DateTime etagTime = new DateTime(etag, DateTimeKind.Utc);
            if (DateTime.UtcNow > etagTime)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void AppendEtag(HttpContext context, DateTime expires)
    {
        context.Response.Headers.Append("ETag", expires.Ticks.ToString());
        context.Response.Headers.Append("Last-Modified", DateTime.UtcNow.ToString("R"));
        context.Response.Headers.Append("Expires", expires.ToString("R"));
        context.Response.Headers.Append("Cache-Control", "private, max-age=" + (int)(new TimeSpan(24, 0, 0)).TotalSeconds);
    }
}
