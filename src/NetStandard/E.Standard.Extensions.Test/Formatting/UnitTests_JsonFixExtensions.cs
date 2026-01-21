using E.Standard.Extensions.Formatting;
using Xunit;

namespace E.Standard.Extensions.Test.Formatting;

public class UnitTests_JsonFixExtensions
{
    [Fact]
    public void Should_Fix_Simple_Object()
    {
        // Arrange
        var input = "{lng:14.7,lat:47.2}";
        var expected = "{\"lng\":14.7,\"lat\":47.2}";

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_Handle_Whitespace_Around_Keys()
    {
        // Arrange
        var input = "{ lng : 10 , lat : 20 }";
        var expected = "{ \"lng\": 10 , \"lat\": 20 }";

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_Not_Change_Already_Quoted_Keys()
    {
        // Arrange
        var input = "{\"lng\":14.7,\"lat\":47.2}";
        var expected = "{\"lng\":14.7,\"lat\":47.2}";

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_Fix_Nested_Object()
    {
        // Arrange
        var input = "{pos:{lng:14.7,lat:47.2},name:\"test\"}";
        var expected = "{\"pos\":{\"lng\":14.7,\"lat\":47.2},\"name\":\"test\"}";

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_Fix_Nested_Object_Not_All_Properties_Are_Unquoted()
    {
        // Arrange
        var input = "{\"pos\":{lng:14.7,\"lat\":47.2},name:\"test\"}";
        var expected = "{\"pos\":{\"lng\":14.7,\"lat\":47.2},\"name\":\"test\"}";

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_Return_Input_When_Null_Or_Empty()
    {
        // Arrange
        string input = null;

        // Act
        var result = input.FixToStrictJson();

        // Assert
        Assert.Null(result);

        // Arrange
        input = "";

        // Act
        result = input.FixToStrictJson();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Converts_Single_Quoted_Value_To_Double_Quoted()
    {
        var input = "{lng:14.7,lat:47.2,text:'Hallo Welt'}";
        var expected = "{\"lng\":14.7,\"lat\":47.2,\"text\":\"Hallo Welt\"}";

        var result = input.FixToStrictJson();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Preserves_Escaped_Single_Quote_Inside_Value()
    {
        var input = "{text:'Bob\\'s car'}";
        var expected = "{\"text\":\"Bob's car\"}";

        var result = input.FixToStrictJson();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escapes_Existing_Double_Quotes_Inside_Single_Quoted_Value()
    {
        var input = "{text:'He said \"hi\"'}";
        var expected = "{\"text\":\"He said \\\"hi\\\"\"}";

        var result = input.FixToStrictJson();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Works_In_Arrays_Too()
    {
        var input = "[{text:'a'},{text:'b c'}]";
        var expected = "[{\"text\":\"a\"},{\"text\":\"b c\"}]";

        var result = input.FixToStrictJson();

        Assert.Equal(expected, result);
    }
}
