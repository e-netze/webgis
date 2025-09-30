using E.Standard.WebMapping.Core;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class EsriConstExtensions
{
    // Possible values
    // esriTimeUnitsCenturies | esriTimeUnitsDays
    //    | esriTimeUnitsDecades | esriTimeUnitsHours
    //    | esriTimeUnitsMilliseconds | esriTimeUnitsMinutes
    //    | esriTimeUnitsMonths | esriTimeUnitsSeconds
    //    | esriTimeUnitsWeeks | esriTimeUnitsYears
    //    | esriTimeUnitsUnknown
    static public TimePeriod ToTimePeriod(this string esriTimeConst, TimePeriod defaultPeriod = TimePeriod.Unknown)
        => esriTimeConst switch
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
}
