#nullable enable

using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Extracts the extra, map-derived columns (session id, request id, client IP, map center/scale,
/// username) from an <see cref="IMap"/> - shared by
/// <see cref="DbGeoServicePerformanceLogger"/>/<see cref="DbExceptionLogger"/> so both write the
/// same set of columns the same way. Mirrors what <c>CSVLogger</c> already extracts for the
/// "files" performance log (<c>SESSIONID</c>/<c>MAPREQUESTID</c>/<c>CLIENTIP</c>/<c>X</c>/
/// <c>Y</c>/<c>SCALE</c>/<c>USERNAME</c> columns), so the DB-backed loggers carry the same
/// information. How the username is actually stored (not at all/plain-text/hashed) is decided by
/// the caller-supplied <see cref="UsernameLoggingMode"/> - see <see cref="User(IMap?, UsernameLoggingMode)"/>.
/// </summary>
internal static class GeoServiceLogContext
{
    public static string? SessionId(IMap? map)
        => NullIfEmpty(map?.Environment?.UserValue("SessionID", String.Empty) as string);

    public static string? MapRequestId(IMap? map)
        => NullIfEmpty(map?.RequestId?.ToSimpleRequestId());

    public static string? ClientIp(IMap? map)
        => NullIfEmpty(map?.Environment?.UserValue("ClientIp", String.Empty) as string);

    public static string? Username(IMap? map)
        => NullIfEmpty(map?.Environment?.UserValue("username", String.Empty) as string);

    public static double? CenterX(IMap? map) => SafeMapValue(map, m => Math.Round(m.Extent.CenterPoint.X, 2));

    public static double? CenterY(IMap? map) => SafeMapValue(map, m => Math.Round(m.Extent.CenterPoint.Y, 2));

    public static double? Scale(IMap? map) => SafeMapValue(map, m => Math.Round(m.MapScale));

    private static double? SafeMapValue(IMap? map, Func<IMap, double> selector)
    {
        if (map == null)
        {
            return null;
        }

        try
        {
            return selector(map);
        }
        catch
        {
            // Extent/scale may not be fully initialized for every request kind - never let
            // logging itself fail because of it.
            return null;
        }
    }

    private static string? NullIfEmpty(string? value) => String.IsNullOrEmpty(value) ? null : value;

    /// <summary>
    /// The username, as it should actually be written into the "user" column, given
    /// <paramref name="mode"/> - <c>null</c>/not logged, plain-text, or a one-way hash. See
    /// <see cref="UsernameLogging.Apply"/>.
    /// </summary>
    public static string? User(IMap? map, UsernameLoggingMode mode) => UsernameLogging.Apply(mode, Username(map));
}
