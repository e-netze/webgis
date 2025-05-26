using E.Standard.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace E.Standard.Extensions.Test.Text;

public class UnitTests_StringEncodingExtensions
{
    public class Latin1UrlEncodingTests
    {
        [Theory]
        [InlineData("Siegendorf;Springschitz-Gründe II", "Siegendorf%3bSpringschitz-Gr%fcnde%20II")]
        [InlineData("Straße & Café", "Stra%dfe%20%26%20Caf%e9")]
        [InlineData("äöüÄÖÜ", "%e4%f6%fc%c4%d6%dc")]
        [InlineData("A-Z a-z 0-9", "A-Z%20a-z%200-9")]
        [InlineData(" ", "%20")]
        [InlineData("", "")]
        [InlineData("abc123", "abc123")]
        [InlineData(null, "")]
        public void ToLatin1UrlEncoded_ReturnsExpectedEncodedString(string input, string expected)
        {
            // Act
            string actual = input.ToLatin1UrlEncoded();

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
