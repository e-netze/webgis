#nullable enable

using System;
using System.Globalization;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static public class EsriDateExtensions
{
    static public string DateFormatString = "dd.MM.yyyy";
    static public string TimeFormatString = "HH:mm:ss";
    static public CultureInfo CultureInfo = CultureInfo.CurrentCulture;

    static public DateTimeOffset EsriDateToDateTimeOffset(this long esriDateTime)
        => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDateTime);

    static public string EsriDateToString(this object? dateValue, string? dateFormat = null, string? timeFormat = null)
    {
        if (dateValue is null)
        {
            return string.Empty;
        }

        if (long.TryParse(dateValue.ToString(), out long esriDate) /*&& esriDate > 0*/)  // there can be dates before 1.1.1970
        {
            DateTime td = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDate);

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
