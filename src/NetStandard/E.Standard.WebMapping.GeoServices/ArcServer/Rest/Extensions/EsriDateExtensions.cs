#nullable enable

using System;
using System.Globalization;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static public class EsriDateExtensions
{
    static public string DateFormatString = "dd.MM.yyyy";
    static public string TimeFormatString = "HH:mm:ss";
    static public CultureInfo CultureInfo = CultureInfo.CurrentCulture;

    static public DateTimeOffset EsriDateToDateTimeOffset(this long esriDateTime, TimeZoneInfo? timeZone = null)
    {
        var utc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDateTime);
        if (timeZone is null || timeZone == TimeZoneInfo.Utc)
        {
            return utc;
        }
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), timeZone);
    }

    static public string EsriDateToString(this object? dateValue, string? dateFormat = null, string? timeFormat = null, TimeZoneInfo? timeZone = null)
    {
        if (dateValue is null)
        {
            return string.Empty;
        }

        if (long.TryParse(dateValue.ToString(), out long esriDate) /*&& esriDate > 0*/)  // there can be dates before 1.1.1970
        {
            DateTime utcDt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDate);
            DateTime td = (timeZone is null || timeZone == TimeZoneInfo.Utc)
                ? utcDt
                : TimeZoneInfo.ConvertTimeFromUtc(utcDt, timeZone);

            //return td.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (td.TimeOfDay == TimeSpan.Zero)
            {
                return td.ToDateString(dateFormat ?? DateFormatString);
            }

            return $"{td.ToDateString(dateFormat ?? DateFormatString)} {td.ToTimeString(timeFormat ?? TimeFormatString)}";
        }

        return dateValue.ToString() ?? String.Empty;
    }

    static public bool TryParseExactEsriDate(this string dateValue, out DateTime dateTime, string? dateFormat = null, string? timeFormat = null)
    {
        if (DateTime.TryParseExact(dateValue, $"{dateFormat ?? DateFormatString} {timeFormat ?? TimeFormatString}", CultureInfo, DateTimeStyles.None, out dateTime))
        {
            return true;
        }
        else if (DateTime.TryParseExact(dateValue, dateFormat ?? DateFormatString, CultureInfo, DateTimeStyles.None, out dateTime))
        {
            return true;
        }
        else if (DateTime.TryParseExact(dateValue, timeFormat ?? TimeFormatString, CultureInfo, DateTimeStyles.None, out dateTime))
        {
            return true;
        }

        dateTime = default;
        return false;
    }

    static private string ToDateString(this DateTime dt, string? format)
        => String.IsNullOrEmpty(format)
                ? dt.ToShortDateString()
                : dt.ToString(format, CultureInfo);

    static private string ToTimeString(this DateTime dt, string? format)
        => String.IsNullOrEmpty(format)
                ? dt.ToShortDateString()
                : dt.ToString(format, CultureInfo);
}
