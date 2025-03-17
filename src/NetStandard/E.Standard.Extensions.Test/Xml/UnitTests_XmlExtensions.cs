using E.Standard.Extensions.Xml;
using System.Text;
using Xunit;

namespace E.Standard.Extensions.Test.Xml;

public class UnitTests_XmlExtensions
{
    [Theory]
    [InlineData("<tag>Text & ' \" > < </tag>", "&lt;tag&gt;Text &amp; &apos; &quot; &gt; &lt; &lt;/tag&gt;")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void EscapeXml_ShouldEscapeXmlCharacters(string input, string expected)
    {
        // Act
        string result = input.EscapeXml();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<?xml version='1.0' encoding='UTF-8'?><root>text</root>", "utf-8", "<?xml version='1.0' encoding='UTF-8'?><root>text</root>")]
    [InlineData("<?xml version='1.0' encoding='ISO-8859-1'?><root>text</root>", "iso-8859-1", "<?xml version='1.0' encoding='ISO-8859-1'?><root>text</root>")]
    [InlineData("<?xml version='1.0'?><root>text</root>", "utf-8", "<?xml version='1.0'?><root>text</root>")]
    [InlineData("", "utf-8", "")]
    public void ToXmlString_ShouldReturnXmlStringAndEncoding(string input, string expectedEncoding, string expected)
    {
        // Arrange
        Encoding encoding;
        byte[] buffer = Encoding.UTF8.GetBytes(input);

        // Act
        string result = buffer.ToXmlString(out encoding);

        // Assert
        Assert.Equal(expectedEncoding, encoding.WebName);
        Assert.Equal(expected, result);
    }

}
