namespace E.Standard.Web.Test;
public class HttpEncodingTests
{
    [Theory]
    [InlineData("Ein Test", "Ein%20Test")]
    [InlineData("Körrosi Straße", "K%C3%B6rrosi%20Stra%C3%9Fe")]
    public void UrlEncode_ShouldEncodeString(string input, string expected)
    {
        // Act
        var result = Uri.EscapeDataString(input);
        // Assert
        Assert.Equal(expected, result);
    }
}
