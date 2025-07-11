using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using System.Globalization;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Extensions;

public class EsriDateConversionTests
{
    public EsriDateConversionTests()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        EsriDateExtensions.DateFormatString = "dd/MM/yyyy";
        EsriDateExtensions.TimeFormatString = "HH:mm";
    }

    [Fact]
    public void NullInput_ReturnsEmptyString()
    {
        object? input = null;
        var result = input.EsriDateToString();
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123abc")]
    public void InvalidStringInput_ReturnsOriginalString(string input)
    {
        var result = input.EsriDateToString();
        Assert.Equal(input, result);
    }

    [Fact]
    public void ValidEpochDate_ReturnsDateOnly()
    {
        // 2020-01-01T00:00:00.000Z in milliseconds since Unix epoch
        long esriMillis = 1577836800000;
        var result = esriMillis.EsriDateToString();
        Assert.Equal("01/01/2020", result); 
    }

    [Fact]
    public void ValidEpochDateWithTime_ReturnsDateAndTime()
    {
        // 2020-01-01T15:30:00.000Z
        long esriMillis = 1577892600000;
        var result = esriMillis.EsriDateToString();
        Assert.Contains("01/01/2020", result);
        Assert.Contains("15", result); // Hour
    }

    [Fact]
    public void DoubleValue_ValidLong_ReturnsDate()
    {
        double input = 1577836800000.0;
        var result = input.EsriDateToString();
        Assert.Contains("01/01/2020", result);
    }

    [Fact]
    public void DateTimeObject_ReturnsToStringValue()
    {
        var dt = new DateTime(2020, 1, 1);
        var result = dt.EsriDateToString();
        Assert.Equal(dt.ToString(), result);
    }
}