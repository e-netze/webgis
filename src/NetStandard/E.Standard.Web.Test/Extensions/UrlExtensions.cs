using E.Standard.Web.Extensions;
using System.Collections.Specialized;



namespace E.Standard.Web.Test.Extensions;

public class UrlExtensionsTests
{
    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldReturnOriginalUrl_WhenHeadersAreNull()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection? headers = null;

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal(url, result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldReturnOriginalUrl_WhenHeadersAreEmpty()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection headers = new NameValueCollection();

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal(url, result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldReturnOriginalUrl_WhenNoPlaceholdersInUrl()
    {
        // Arrange
        string url = "https://www.kommmunalnet.at/somepath";

        NameValueCollection headers = new NameValueCollection()
        {
            { "X-ANY-HEADER", "xyz" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers);

        // Assert
        Assert.Equal(url, result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldReturnOriginalUrl_WhenNoHeaderMatches()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection headers = new NameValueCollection()
        {
            { "X-ANY-HEADER", "xyz" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers);

        // Assert
        Assert.Equal(url, result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldReplacePlaceholdersWithHeaderValues()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal("https://www.kommmunalnet.at/somepath", result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldUseDefaultValue_WhenHeaderNotFound()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => $"default-{placeholder}");

        // Assert
        Assert.Equal("https://default-header:X-ORIG-HOST/somepath", result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldUseDefaultValue_WhenHeaderNotFound2()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST}/somepath";
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers);

        // Assert
        Assert.Equal("https://{header:X-ORIG-HOST}/somepath", result);
    }

    [Fact]
    public void ReplaceUrlHeaderPlaceholders_ShouldHandleUrlsWithoutPlaceholders()
    {
        // Arrange
        string url = "https://www.kommmunalnet.at/somepath";
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal(url, result);
    }

    [Fact]
    public void ReplaceUrlPlaceholders_WithInnerSubPath()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST/sub1}/somepath";
        // should be cut before /sub1
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at/sub1/and_some_more/etc" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal("https://www.kommmunalnet.at/somepath", result);
    }

    [Fact]
    public void ReplaceUrlPlaceholders_WithInnerSubPaths1()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST/sub1|/sub2}/somepath";
        // should be cut before /sub1
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at/sub1/and_some_more/etc" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal("https://www.kommmunalnet.at/somepath", result);
    }

    [Fact]
    public void ReplaceUrlPlaceholders_WithInnerSubPaths2()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST/sub1|/sub2}/somepath";
        // should be cut before /sub1
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at/sub2/and_some_more/etc" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal("https://www.kommmunalnet.at/somepath", result);
    }

    [Fact]
    public void ReplaceUrlPlaceholders_WithInnerSubPathsNoMatch()
    {
        // Arrange
        string url = "{header:X-ORIG-SCHEME}://{header:X-ORIG-HOST/sub1|/sub2}/somepath";
        // should be cut before /sub1
        NameValueCollection headers = new NameValueCollection
        {
            { "X-ORIG-SCHEME", "https" },
            { "X-ORIG-HOST", "www.kommmunalnet.at/sub3/and_some_more/etc" }
        };

        // Act
        var result = url.ReplaceUrlHeaderPlaceholders(headers, placeholder => "default");

        // Assert
        Assert.Equal("https://www.kommmunalnet.at/sub3/and_some_more/etc/somepath", result);
    }
}
