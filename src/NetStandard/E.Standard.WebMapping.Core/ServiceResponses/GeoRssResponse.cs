namespace E.Standard.WebMapping.Core.ServiceResponses;

public class GeoRssResponse : HtmlResponse
{
    public string Marker, HighlightMarker;
    public bool RefreshToc = true;

    public GeoRssResponse(int index, string serviceID, string marker, string highlightMarker, string html, string jScript, bool refreshToc)
        : base(index, serviceID, html, jScript)
    {
        Marker = marker;
        HighlightMarker = highlightMarker;
        RefreshToc = refreshToc;
    }
}
