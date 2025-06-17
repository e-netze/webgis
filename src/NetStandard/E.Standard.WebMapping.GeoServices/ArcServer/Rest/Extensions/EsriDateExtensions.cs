#nullable enable

using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
static public class EsriDateExtensions
{
    static public string DateFormatString = "dd.MM.yyyy";
    static public string TimeFormatString = "HH:mm:ss";

    static public string EsriDateToString(this object? dateValue, string? dateFormat = null, string? timeFormat = null)
    {
        if (dateValue is null)
        {
            return string.Empty;
        }

        if (long.TryParse(dateValue.ToString(), out long esriDate) && esriDate > 0)
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

    static private string ToDateString(this DateTime dt, string? format)
        => String.IsNullOrEmpty(format)
                ? dt.ToShortDateString()
                : dt.ToString(format);

    static private string ToTimeString(this DateTime dt, string? format)
        => String.IsNullOrEmpty(format)
                ? dt.ToShortDateString()
                : dt.ToString(format);
}
