namespace E.Standard.WebMapping.GeoServices;

static public class IntExtensions
{
    static public int ToTimeoutSecondOrDefault(this int timeoutSeconds)
        => timeoutSeconds <= 0
        ? 20
        : timeoutSeconds;
}
