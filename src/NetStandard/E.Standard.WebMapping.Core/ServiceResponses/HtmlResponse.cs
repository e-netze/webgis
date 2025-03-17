using System;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class HtmlResponse : JavaScriptResponse
{
    public string Html;
    public bool SetHtml = true;

    public HtmlResponse(int index, string serviceID, string html)
        : base(index, serviceID, String.Empty)
    {
        Html = html;
    }
    public HtmlResponse(int index, string serviceID, string html, string jScript)
        : base(index, serviceID, jScript)
    {
        Html = html;
    }
}
