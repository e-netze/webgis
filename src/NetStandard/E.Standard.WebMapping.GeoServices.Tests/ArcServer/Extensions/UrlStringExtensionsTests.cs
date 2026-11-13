using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Extensions;

public class UrlStringExtensionsTests
{
    [Theory]
    [InlineData("https://ags/rest/services/x/MapServer/WMTS/1.0.0/WMTSCapabilities.xml", "https://ags/rest/services/x/MapServer/WMTS/1.0.0/WMTSCapabilities.xml?f=json")]
    [InlineData("https://ags/rest/services/x/MapServer?outSR=4326&ref=1", "https://ags/rest/services/x/MapServer?outSR=4326&ref=1&f=json")]
    [InlineData("https://ags/rest/services/x/MapServer?f=pjson", "https://ags/rest/services/x/MapServer?f=pjson")]
    [InlineData("https://ags/rest/services/x/MapServer/export?bbox=1&F=image", "https://ags/rest/services/x/MapServer/export?bbox=1&F=image")]
    public void AddAgsFormatJsonIfMissing_Url(string url, string expected)
        => Assert.Equal(expected, url.AddAgsFormatJsonIfMissing());

    [Fact]
    public void AddAgsFormatJsonIfMissing_FormatInPostBody_KeepsUrl()
        => Assert.Equal("https://ags/query", "https://ags/query".AddAgsFormatJsonIfMissing("where=1%3D1&f=json"));
}
