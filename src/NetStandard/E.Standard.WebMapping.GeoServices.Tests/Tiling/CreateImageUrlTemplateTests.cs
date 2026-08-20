using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.GeoServices.Tiling;

namespace E.Standard.WebMapping.GeoServices.Tests.Tiling;

/// <summary>
/// Minimal concrete <see cref="TileService"/> used only to expose the protected
/// <see cref="TileService.CreateImageUrlTemplate(string[])"/> method for testing.
/// </summary>
internal class TestTileService : TileService
{
    public TestTileService() : base(false) { }

    public (string tileUrl, string[] domains) CallCreateImageUrlTemplate(string[] imageUrls)
        => CreateImageUrlTemplate(imageUrls);

    public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
        => Task.FromResult(true);

    public override IMapService Clone(IMap parent)
        => throw new NotImplementedException();

    public override string ImageUrl(IRequestContext requestContext, IMap map)
        => throw new NotImplementedException();

    public override string[] ImageUrls(IRequestContext requestContext, IMap map)
        => throw new NotImplementedException();

    public override (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map)
        => throw new NotImplementedException();
}

public class CreateImageUrlTemplateTests
{
    private readonly TestTileService _service = new();

    // With a single Url the function short-circuits (imageUrls.Length == 1) and
    // returns the Url unchanged with an empty domains array - the port
    // normalization logic below is only exercised with 2+ Urls.
    [Theory]
    [InlineData("http://gistiles1/tiles/1/2/3.png")]
    [InlineData("http://localhost:5001/tiles/1/2/3.png")]
    [InlineData("https://gistiles1:443/tiles/1/2/3.png")]
    [InlineData("//gistiles1:5001/tiles/1/2/3.png")]
    public void SingleUrl_ReturnsUrlUnchangedWithEmptyDomains(string imageUrl)
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[] { imageUrl });

        Assert.Equal(imageUrl, tileUrl);
        Assert.Empty(domains);
    }

    [Fact]
    public void TwoUrls_WithDefaultPort_ReturnsTemplateWithoutPort()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1/tiles/1/2/3.png",
            "http://gistiles2/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1", "gistiles2" }, domains);
    }

    [Fact]
    public void TwoUrls_WithNonDefaultPort_ReturnsTemplateWithPortKept()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://localhost:5001/tiles/1/2/3.png",
            "http://gistiles2:5001/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "localhost:5001", "gistiles2:5001" }, domains);
    }

    [Fact]
    public void TwoUrls_HttpsWithDefaultPort443_ReturnsTemplateWithoutPort()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "https://gistiles1:443/tiles/1/2/3.png",
            "https://gistiles2/tiles/1/2/3.png"
        });

        Assert.Equal("https://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1", "gistiles2" }, domains);
    }

    [Fact]
    public void TwoUrls_HttpsWithNonDefaultPort_ReturnsTemplateWithPortKept()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "https://gistiles1:8443/tiles/1/2/3.png",
            "https://gistiles2:8443/tiles/1/2/3.png"
        });

        Assert.Equal("https://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1:8443", "gistiles2:8443" }, domains);
    }

    [Fact]
    public void TwoUrls_SchemeRelativeWithoutPort_ReturnsTemplateWithoutPort()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "//gistiles1/tiles/1/2/3.png",
            "//gistiles2/tiles/1/2/3.png"
        });

        Assert.Equal("//{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1", "gistiles2" }, domains);
    }

    [Fact]
    public void TwoUrls_SchemeRelativeWithPort_ReturnsTemplateWithPortKept()
    {
        // Scheme (http/https) is unknown for scheme-relative Urls, so an
        // explicitly given port must always be kept as-is (can't be compared
        // against a "default" port of an unknown scheme).
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "//gistiles1:5001/tiles/1/2/3.png",
            "//gistiles2:5001/tiles/1/2/3.png"
        });

        Assert.Equal("//{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1:5001", "gistiles2:5001" }, domains);
    }

    [Fact]
    public void TwoUrls_ExplicitAndImplicitDefaultPort_NormalizeToSameHost_AreDeduplicated()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1/tiles/1/2/3.png",
            "http://gistiles1:80/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1" }, domains);
    }

    [Fact]
    public void ThreeUrls_SameSchemeAndPath_DifferentHosts_ReturnsAllDomains()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1:5001/tiles/1/2/3.png",
            "http://gistiles2:5001/tiles/1/2/3.png",
            "http://gistiles3:5001/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1:5001", "gistiles2:5001", "gistiles3:5001" }, domains);
    }

    [Fact]
    public void MultipleUrls_DuplicateHosts_AreDeduplicated()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1:5001/tiles/1/2/3.png",
            "http://gistiles1:5001/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1:5001" }, domains);
    }

    [Fact]
    public void MultipleUrls_DifferentPaths_FallsBackToFirstUrl()
    {
        var imageUrls = new[]
        {
            "http://gistiles1/tiles/a/1/2/3.png",
            "http://gistiles2/tiles/b/1/2/3.png"
        };

        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(imageUrls);

        Assert.Equal(imageUrls[0], tileUrl);
        Assert.Empty(domains);
    }

    [Fact]
    public void MultipleUrls_DifferentHostsAndPorts_SameTemplate_AllDomainsKept()
    {
        // Different hosts/ports still produce the same {s}-template, since {s}
        // substitutes the whole "host:port" segment.
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1:5001/tiles/1/2/3.png",
            "http://gistiles2:5002/tiles/1/2/3.png"
        });

        Assert.Equal("http://{s}/tiles/1/2/3.png", tileUrl);
        Assert.Equal(new[] { "gistiles1:5001", "gistiles2:5002" }, domains);
    }

    [Fact]
    public void TwoUrls_WithQueryString_IsIncludedInTemplate()
    {
        var (tileUrl, domains) = _service.CallCreateImageUrlTemplate(new[]
        {
            "http://gistiles1:5001/tiles?x=1&y=2&z=3",
            "http://gistiles2:5001/tiles?x=1&y=2&z=3"
        });

        Assert.Equal("http://{s}/tiles?x=1&y=2&z=3", tileUrl);
        Assert.Equal(new[] { "gistiles1:5001", "gistiles2:5001" }, domains);
    }
}
