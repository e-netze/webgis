using System.Collections.Specialized;

using E.Standard.WebMapping.Core.Api;

namespace E.Standard.WebMapping.Core.Api.Tests;

public class ApiToolEventArgumentsTests
{
    [Fact]
    public void EventBody_StringValues_ArePassedUnchanged()
    {
        var e = new ApiToolEventArguments("""{ "_method": "save", "text": "a|b=c;d,e", "empty": "" }""");

        Assert.Equal("save", e.ServerCommandMethod);
        Assert.Equal("a|b=c;d,e", e["text"]);
        Assert.Equal(String.Empty, e["empty"]);
    }

    [Fact]
    public void EventBody_NumbersAndBooleans_AreNormalizedToStrings()
    {
        var e = new ApiToolEventArguments("""{ "_mapscale": 1000.5, "_mapcrs": 3857, "_calccrs": 31256, "_calccrs_is_dynamic": true, "flag": false, "small": 1e-7 }""");

        Assert.Equal(1000.5, e.MapScale);
        Assert.Equal(3857, e.MapCrs);
        Assert.Equal(31256, e.CalcCrs);
        Assert.True(e.CalcCrsIsDynamic);
        Assert.Equal("false", e["flag"]);
        Assert.False(e.GetBoolean("flag"));
        Assert.Equal("1e-7", e["small"]);
        Assert.Equal(1e-7, e.GetDouble("small"));
    }

    [Fact]
    public void EventBody_Null_IsKeptAsPropertyAndReturnsEmptyString()
    {
        var e = new ApiToolEventArguments("""{ "x": null, "_sketch": null }""");

        Assert.Contains("x", e.Properties);
        Assert.Equal(String.Empty, e["x"]);
        Assert.True(e.IsEmpty("x"));
        Assert.Null(e.Sketch);
    }

    [Fact]
    public void EventBody_Arrays_AreNormalizedToCommaSeparatedStrings()
    {
        var e = new ApiToolEventArguments("""{ "_mapbbox": [1.5, 2, 3, 4.25], "_mapsize": [800, 600], "_overlay_services": ["a", "b"], "empty": [] }""");

        Assert.Equal("1.5,2,3,4.25", e["_mapbbox"]);
        Assert.Equal(new[] { 1.5, 2, 3, 4.25 }, e.MapBBox());
        Assert.Equal(new[] { 800, 600 }, e.MapSize());
        Assert.Equal(new[] { "a", "b" }, e.MapOverlayServices);
        Assert.Equal(String.Empty, e["empty"]);
    }

    [Fact]
    public void EventBody_Indented_IsParsedLikeCompact()
    {
        // client sends indented JSON when webgis.logLevel is set (debugging)
        var e = new ApiToolEventArguments("{\n  \"_method\": \"save\",\n  \"_mapbbox\": [\n    1,\n    2\n  ],\n  \"text\": \"a\"\n}");

        Assert.Equal("save", e["_method"]);
        Assert.Equal("1,2", e["_mapbbox"]);
        Assert.Equal("a", e["text"]);
    }

    [Fact]
    public void EventBody_Objects_AreNormalizedToRawJsonAndDeserialized()
    {
        var e = new ApiToolEventArguments("""
            {
              "_selectioninfo": { "serviceid": "srv", "layerid": "1", "queryid": "q", "ids": [1, 2] },
              "_deviceinfo": { "is_mobile_device": true, "screen_width": 1024, "screen_height": 768, "advanced_tool_behaviour": false },
              "_sketchInfo": { "geometryType": "circle", "radius": 10, "center": { "lat": 47, "lng": 15 } },
              "custom": { "a": "x|y" }
            }
            """);

        Assert.Equal("srv", e.SelectionInfo.ServiceId);
        Assert.Equal(new[] { 1, 2 }, e.SelectionInfo.ObjectIds);
        Assert.True(e.DeviceInfo.IsMobileDevice);
        Assert.Equal(1024, e.DeviceInfo.ScreenWidth);
        Assert.Equal("circle", e.SketchInfo.GeometryType);
        Assert.Equal(10, e.SketchInfo.Radius);
        Assert.Equal("""{ "a": "x|y" }""", e["custom"]);
    }

    [Fact]
    public void EventBody_ClickValues_AreNormalizedToStrings()
    {
        var e = new ApiToolEventArguments("""{ "lng": 15.5, "lat": 47.1, "crs": 4326, "x": 100, "y": 200, "event_scale": 5000 }""");

        Assert.Equal("15.5", e["lng"]);
        Assert.Equal("47.1", e["lat"]);
        Assert.Equal(4326, e.GetInt("crs"));
        Assert.Equal("100", (string)e.RawEvent.x);
        Assert.Equal(5000, e.GetDouble("event_scale"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("{}")]
    public void EventBody_Empty_HasNoProperties(string? eventBody)
    {
        var e = new ApiToolEventArguments(eventBody!);

        Assert.Empty(e.Properties);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"_method=save\"")]
    public void EventBody_NotAnObject_Throws(string eventBody)
    {
        Assert.Throws<ArgumentException>(() => new ApiToolEventArguments(eventBody));
    }

    [Fact]
    public void EventBody_Roundtrip_KeepsAllValues()
    {
        // e.g. __res: WebStorage passes the RawEventBody to the storage api, which creates new arguments from it
        var original = new ApiToolEventArguments("""{ "_method": "save", "text": "a|b", "_mapbbox": [1, 2, 3, 4], "obj": { "a": 1 }, "n": null }""");

        var copy = new ApiToolEventArguments(original.RawEventBody);

        Assert.Equal(original.Properties, copy.Properties);
        foreach (var property in original.Properties)
        {
            Assert.Equal(original[property], copy[property]);
        }
    }

    [Fact]
    public void NameValueCollection_IsConvertedToJsonEventBody()
    {
        var nvc = new NameValueCollection()
        {
            { "hmac_hash", "xyz" },
            { "toolid", "webgis.tools.edit" },
            { "_mapscale", "2000" },
            { "attribute", "a|b=c" }
        };

        var e = new ApiToolEventArguments(null!, nvc, new[] { "toolid" });

        Assert.Equal(new[] { "_mapscale", "attribute" }, e.Properties);
        Assert.Equal(2000, e.MapScale);
        Assert.Equal("a|b=c", e["attribute"]);

        var copy = new ApiToolEventArguments(e.RawEventBody);
        Assert.Equal("a|b=c", copy["attribute"]);
    }
}
