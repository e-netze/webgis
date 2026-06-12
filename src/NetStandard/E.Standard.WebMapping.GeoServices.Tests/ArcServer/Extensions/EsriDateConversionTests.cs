using System.Globalization;

using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

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

    [Fact]
    public void WithUtcTimeZone_BehaviorUnchanged()
    {
        // 2020-01-01T00:00:00.000Z
        long esriMillis = 1577836800000;
        var result = esriMillis.EsriDateToString(timeZone: TimeZoneInfo.Utc);
        Assert.Equal("01/01/2020", result);
    }

    [Fact]
    public void WithNullTimeZone_BehaviorUnchanged()
    {
        // 2020-01-01T00:00:00.000Z
        long esriMillis = 1577836800000;
        var result = esriMillis.EsriDateToString(timeZone: null);
        Assert.Equal("01/01/2020", result);
    }

    [Fact]
    public void WithPositiveUtcOffset_DateTimeShiftedForward()
    {
        // 2020-01-01T22:00:00.000Z => in UTC+2 this becomes 2020-01-02T00:00:00
        long esriMillis = new DateTimeOffset(2020, 1, 1, 22, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var tz = TimeZoneInfo.CreateCustomTimeZone("TestTZ", TimeSpan.FromHours(2), "TestTZ", "TestTZ");
        var result = esriMillis.EsriDateToString(timeZone: tz);
        // midnight in UTC+2 => only date, no time component
        Assert.Equal("02/01/2020", result);
    }

    [Fact]
    public void WithPositiveUtcOffset_TimeShiftedForward()
    {
        // 2020-06-15T12:30:00.000Z => in UTC+2 this becomes 2020-06-15T14:30:00
        long esriMillis = new DateTimeOffset(2020, 6, 15, 12, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var tz = TimeZoneInfo.CreateCustomTimeZone("TestTZ+2", TimeSpan.FromHours(2), "TestTZ+2", "TestTZ+2");
        var result = esriMillis.EsriDateToString(timeZone: tz);
        Assert.Contains("15/06/2020", result);
        Assert.Contains("14:30", result);
    }

    [Fact]
    public void EsriDateToDateTimeOffset_WithTimeZone_ConvertsCorrectly()
    {
        // 2020-01-01T00:00:00.000Z => UTC+1 => 2020-01-01T01:00:00+01:00
        long esriMillis = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var tz = TimeZoneInfo.CreateCustomTimeZone("TestTZ+1", TimeSpan.FromHours(1), "TestTZ+1", "TestTZ+1");
        var result = esriMillis.EsriDateToDateTimeOffset(tz);
        Assert.Equal(1, result.Hour);
        Assert.Equal(TimeSpan.FromHours(1), result.Offset);
    }

    [Fact]
    public void EsriDateToDateTimeOffset_WithoutTimeZone_ReturnsUtc()
    {
        long esriMillis = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var result = esriMillis.EsriDateToDateTimeOffset();
        Assert.Equal(0, result.Hour);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }
}
