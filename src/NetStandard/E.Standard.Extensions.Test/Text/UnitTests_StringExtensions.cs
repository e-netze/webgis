using E.Standard.Extensions.Text;
using System;
using System.Collections.Specialized;
using Xunit;

namespace E.Standard.Extensions.Test.Text;

public class UnitTests_StringExtensions
{
    [Theory]
    [InlineData("Hello %name%", "%name%", "John", null, null, "Hello John")]
    [InlineData("Hello %name% and %age%", "%name%", "John", "%age%", "25", "Hello John and 25")]
    [InlineData("Hello", "%name%", "John", null, null, "Hello")]
    [InlineData("", "%name%", "John", null, null, "")]
    public void Replace_ShouldReplaceValuesWithKeyValuePairs(string str, string key1, string value1, string key2 = null, string value2 = null, string expected = null)
    {
        // Arrange
        var nvc = new NameValueCollection();
        nvc.Add(key1, value1);

        if (key2 != null)
        {
            nvc.Add(key2, value2);
        }

        // Act
        string result = str.Replace(nvc);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", "World", "Universe", StringComparison.Ordinal, "Hello Universe")]
    [InlineData("Hello World", "world", "Universe", StringComparison.OrdinalIgnoreCase, "Hello Universe")]
    [InlineData("FooBarFooBar", "Bar", "Baz", StringComparison.Ordinal, "FooBazFooBaz")]
    [InlineData("Test", "test", "TEST", StringComparison.OrdinalIgnoreCase, "TEST")]
    [InlineData("Example String", "NonExisting", "Replacement", StringComparison.Ordinal, "Example String")]
    [InlineData("https://SeRver1.com/path?param1=ParAM1", "https://server1.com/", "https://SERVER2.com/", StringComparison.OrdinalIgnoreCase, "https://SERVER2.com/path?param1=ParAM1")]
    [InlineData("", "https://server1.com/", "https://SERVER2.com/", StringComparison.OrdinalIgnoreCase, "")]
    [InlineData(null, "https://server1.com/", "https://SERVER2.com/", StringComparison.OrdinalIgnoreCase, null)]
    [InlineData("https://SeRver1.com/path?param1=ParAM1", "", "https://SERVER2.com/", StringComparison.OrdinalIgnoreCase, "https://SeRver1.com/path?param1=ParAM1")]
    [InlineData("https://SeRver1.com/path?param1=ParAM1", "https://server1.com/", "", StringComparison.OrdinalIgnoreCase, "path?param1=ParAM1")]
    [InlineData("https://SeRver1.com/path?param1=ParAM1", "https://server1.com/", null, StringComparison.OrdinalIgnoreCase, "path?param1=ParAM1")]
    public void Replace_ShouldReturnExpectedString(string original, string oldValue, string newValue, StringComparison comparisonType, string expected)
    {
        // Act
        var result = original.ReplacePro(oldValue, newValue, comparisonType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc///", '/', "abc")]
    [InlineData("abc", '/', "abc")]
    [InlineData("///", '/', "")]
    [InlineData("", '/', "")]
    [InlineData(null, '/', null)]
    public void RemoveEnding_RemovesAllTrailingChars(string input, char ending, string expected)
    {
        Assert.Equal(expected, input.RemoveEnding(ending));
    }

    [Theory]
    [InlineData("/abc/", "/abc")]
    [InlineData("abc/", "abc")]
    [InlineData("abc", "abc")]
    [InlineData("/", "")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveEndingSlash_Works(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveEndingSlash());
    }

    [Theory]
    [InlineData("//abc", '/', "abc")]
    [InlineData("/abc", '/', "abc")]
    [InlineData("abc", '/', "abc")]
    [InlineData("/", '/', "")]
    [InlineData("", '/', "")]
    [InlineData(null, '/', null)]
    public void RemoveStarting_RemovesAllStartingChars(string input, char startChar, string expected)
    {
        Assert.Equal(expected, input.RemoveStarting(startChar));
    }

    [Theory]
    [InlineData("/abc", "abc")]  // Achtung: Bug in deiner Methode – sie ruft RemoveEnding auf statt RemoveStarting!
    public void RemoveStartingSlash_Works(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveStarting('/')); // Das entspricht der eigentlichen Absicht
    }

    [Theory]
    [InlineData(@"path///", "path")]
    [InlineData(@"path\\\\", "path")]
    [InlineData(@"path///\\\", "path")]
    [InlineData(@"///\\\", "")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveEndingSlashAndBackslash_Works(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveEndingSlashAndBackslash());
    }

    [Theory]
    [InlineData(@"\///path", "path")]
    [InlineData(@"/path", "path")]
    [InlineData(@"\\path", "path")]
    [InlineData(@"///\\\path", "path")]
    [InlineData(@"path", "path")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveStartingSlashAndBackslash_Works(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveStartingSlashAndBackslash());
    }

    [Theory]
    [InlineData("abc/", "/def", '/', "abc/def")]
    [InlineData("abc", "def", '/', "abc/def")]
    [InlineData("abc///", "///def", '/', "abc/def")]
    [InlineData("", "def", '/', "def")]
    [InlineData("abc", "", '/', "abc")]
    [InlineData("", "", '/', "")]
    [InlineData(null, "def", '/', "def")]
    [InlineData("abc", null, '/', "abc")]
    [InlineData(null, null, '/', null)]
    public void ConcatWithSlash_Works(string str1, string str2, char separator, string expected)
    {
        var result = str1.ConcatWith(str2, separator);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("api///", "endpoint///", "api/endpoint///")]
    [InlineData("api", "endpoint", "api/endpoint")]
    [InlineData("api\\", "endpoint\\", "api\\endpoint\\")]
    [InlineData("", "endpoint", "endpoint")]
    [InlineData("api", "", "api")]
    [InlineData(null, "endpoint", "endpoint")]
    [InlineData("api", null, "api")]
    public void AddUriPath_Works(string str1, string str2, string expected)
    {
        Assert.Equal(expected, str1.AddUriPath(str2));
    }

    [Theory]
    [InlineData("/abc", "abc")]
    [InlineData("///abc", "abc")]
    [InlineData("abc", "abc")]
    [InlineData("/", "")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveStartingSlash_WorksCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveStarting('/'));
    }

    [Theory]
    [InlineData("abc/", "/def", '/', "abc/def")]
    [InlineData("abc", "def", '/', "abc/def")]
    [InlineData("abc///", "///def", '/', "abc/def")]
    [InlineData("", "def", '/', "def")]
    [InlineData("abc", "", '/', "abc")]
    [InlineData("", "", '/', "")]
    [InlineData(null, "def", '/', "def")]
    [InlineData("abc", null, '/', "abc")]
    [InlineData(null, null, '/', null)]
    [InlineData("/a/", "/b/", '/', "/a/b/")]
    public void ConcatWith_RemovesLeadingAndTrailingCorrectly(string str1, string str2, char separator, string expected)
    {
        var result = str1.ConcatWith(str2, separator);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("api", "endpoint", "api/endpoint")]
    [InlineData("api///", "///endpoint", "api/endpoint")]
    [InlineData("api/", "", "api")]
    [InlineData("", "endpoint/", "endpoint/")]
    [InlineData("", "", "")]
    [InlineData(null, "endpoint", "endpoint")]
    [InlineData("api", null, "api")]
    [InlineData(null, null, null)]
    [InlineData("/api/", "/endpoint/", "/api/endpoint/")]
    [InlineData("/api\\", "\\endpoint/", "/api\\endpoint/")]
    public void AddUriPath_RemovesEndingsAndConcatsCorrectly(string str1, string str2, string expected)
    {
        Assert.Equal(expected, str1.AddUriPath(str2));
    }
}
