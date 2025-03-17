using E.Standard.Extensions.Compare;
using System;
using Xunit;

namespace E.Standard.Extensions.Test.Compare;

public class UnitTest_CompareExtensions
{
    [Fact]
    public void Test_StringEmpty_OrTake_ValidString()
    {
        var stringEmpty = String.Empty;
        var validString = "Hello";

        var actual = stringEmpty.OrTake(validString);

        Assert.Equal("Hello", actual);
    }

    [Theory]
    [InlineData("string1", "string2", "string1")]
    [InlineData("", "string2", "string2")]
    [InlineData(null, "string2", "string2")]
    [InlineData(null, null, null)]
    public void Test_String_With_OrTake_Compare(string str1, string str2, string expected)
    {
        var actual = str1.OrTake(str2);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 2, 1)]
    public void Test_Integer_With_OrTake_Compare(int v1, int v2, int expected)
    {
        var actual = v1.OrTake(v2);

        Assert.Equal(expected, actual);
    }

    public enum FakeEnum
    {
        Unknown = 0,
        Stat1 = 1,
        Stat2 = 2
    };


    [Theory]
    [InlineData(FakeEnum.Unknown, FakeEnum.Stat2, FakeEnum.Stat2)]
    [InlineData(FakeEnum.Stat1, FakeEnum.Stat2, FakeEnum.Stat1)]
    public void Test_OrTakeEnum(FakeEnum v1, FakeEnum v2, FakeEnum expected)
    {
        var actual = v1.OrTakeEnum<FakeEnum>(v2);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", 2, 2)]
    [InlineData(null, 2, 2)]
    [InlineData("1", 2, 1)]
    [InlineData("0", 2, 2)]
    public void Test_OrTakeGeneric_With_String_to_Integer(string v1, int v2, int expected)
    {
        var actual = v1.OrTake<int>(v2);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", 2.0, 2.0)]
    [InlineData(null, 2.0, 2.0)]
    [InlineData("1", 2.0, 1.0)]
    [InlineData("0", 2.0, 2.0)]
    [InlineData("1.3", 2.0, 1.3)]
    [InlineData("0.2", 2.0, 0.2)]
    [InlineData("0,5", 2.0, 0.5)]
    public void Test_OrTakeGeneric_With_String_to_Double(string v1, double v2, double expected)
    {
        var actual = v1.OrTake<double>(v2);

        Assert.Equal(expected, actual);
    }
}