using E.Standard.WebMapping.Core;
using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class EsriConstExtensions
{
    private const TimePeriod DefaultMinPeriod = TimePeriod.Days;
    private const TimePeriod DefaultMaxPeriod = TimePeriod.Years;

    // Possible values
    // esriTimeUnitsCenturies | esriTimeUnitsDays
    //    | esriTimeUnitsDecades | esriTimeUnitsHours
    //    | esriTimeUnitsMilliseconds | esriTimeUnitsMinutes
    //    | esriTimeUnitsMonths | esriTimeUnitsSeconds
    //    | esriTimeUnitsWeeks | esriTimeUnitsYears
    //    | esriTimeUnitsUnknown
    static public TimePeriod ToTimePeriod(
        this string esriTimeConst,
        TimePeriod defaultPeriod = TimePeriod.Unknown,
        TimePeriod minimunPeriod = DefaultMinPeriod,
        TimePeriod maximumPeriod = DefaultMaxPeriod)
    {
        var timePeriod = esriTimeConst switch
        {
            "esriTimeUnitsMilliseconds" => TimePeriod.MilliSeconds,
            "esriTimeUnitsSeconds" => TimePeriod.Seconds,
            "esriTimeUnitsMinutes" => TimePeriod.Minutes,
            "esriTimeUnitsHours" => TimePeriod.Hours,
            "esriTimeUnitsDays" => TimePeriod.Days,
            "esriTimeUnitsWeeks" => TimePeriod.Weeks,
            "esriTimeUnitsMonths" => TimePeriod.Months,
            "esriTimeUnitsYears" => TimePeriod.Years,
            "esriTimeUnitsDecades" => TimePeriod.Decades,
            "esriTimeUnitsCenturies" => TimePeriod.Centuries,
            _ => defaultPeriod,
        };

        return timePeriod
            .MaxPeriod(minimunPeriod)
            .MinPeriod(maximumPeriod);
    }

    private static TimePeriod MinPeriod(this TimePeriod a, TimePeriod b)
        => b != TimePeriod.Unknown
            ? (TimePeriod)Math.Min((int)a, (int)b)
            : a;

    private static TimePeriod MaxPeriod(this TimePeriod a, TimePeriod b)
        => b != TimePeriod.Unknown
            ? (TimePeriod)Math.Max((int)a, (int)b)
            : a;
}
