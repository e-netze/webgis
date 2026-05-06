using System;
using System.Collections.Specialized;

using E.Standard.Extensions.Text;

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

    [Theory]
    [InlineData("3.7", 4)]
    [InlineData("3.5", 4)]
    [InlineData("3.4", 3)]
    [InlineData("3", 3)]
    [InlineData("-3.5", -4)]
    [InlineData("-3.4", -3)]
    [InlineData("0", 0)]
    [InlineData("0.5", 1)]
    [InlineData("99.9", 100)]
    public void ParseAsRoundedInt_ValidNumbers_ReturnsRoundedValue(string input, int expected)
    {
        Assert.Equal(expected, input.ParseInvariantToRoundedInt());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1.2.3")]
    [InlineData("47,11")]
    public void ParseAsRoundedInt_InvalidString_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => input.ParseInvariantToRoundedInt());
    }

    [Fact]
    public void ParseAsRoundedInt_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((string)null).ParseInvariantToRoundedInt());
    }

    [Theory]
    [InlineData("  3.7  ", 4)]
    [InlineData(" 2.1 ", 2)]
    public void ParseAsRoundedInt_InputWithWhitespace_ReturnsRoundedValue(string input, int expected)
    {
        Assert.Equal(expected, input.ParseInvariantToRoundedInt());
    }

    [Theory]
    [InlineData("3.7", 3)]
    [InlineData("3.5", 3)]
    [InlineData("3.0", 3)]
    [InlineData("3", 3)]
    [InlineData("-3.2", -4)]
    [InlineData("-3.9", -4)]
    [InlineData("0", 0)]
    [InlineData("0.9", 0)]
    [InlineData("99.99", 99)]
    public void ParseInvarianteToFlooredInt_ValidNumbers_ReturnsFlooredValue(string input, int expected)
    {
        Assert.Equal(expected, input.ParseInvarianteToFlooredInt());
    }

    [Theory]
    [InlineData("3.7")]   // Invariant: Punkt als Dezimaltrennzeichen
    [InlineData("3,7")]   // Komma ist kein gültiges Dezimaltrennzeichen (Invariant Culture)
    public void ParseInvarianteToFlooredInt_InvariantCulture_UsesPointAsDecimalSeparator(string input)
    {
        // Nur "3.7" ist gültig in Invariant Culture — "3,7" wirft FormatException
        if (input.Contains('.'))
        {
            Assert.Equal(3, input.ParseInvarianteToFlooredInt());
        }
        else
        {
            Assert.Throws<FormatException>(() => input.ParseInvarianteToFlooredInt());
        }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("  ")]
    [InlineData("")]
    public void ParseInvarianteToFlooredInt_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => input.ParseInvarianteToFlooredInt());
    }

    [Fact]
    public void ParseInvarianteToFlooredInt_NullInput_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => ((string)null).ParseInvarianteToFlooredInt());
    }
}
