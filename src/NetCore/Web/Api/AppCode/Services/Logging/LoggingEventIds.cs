using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

/// <summary>
/// Central place for the <see cref="EventId"/>s used by the Microsoft.Extensions.Logging based
/// loggers, so log lines of a given kind (a specific <see cref="GeoServiceCommand"/>, a warning,
/// an exception, ...) can be filtered/alerted on in a log backend (Seq, Application Insights, ...)
/// independent of the free-text message.
/// </summary>
internal static class LoggingEventIds
{
    // Fixed ids for the simple, non-dynamic loggers - usable directly as [LoggerMessage(EventId = ...)]
    // constants, since those require a compile-time constant.
    public const int Warning = 400;
    public const int ExceptionForUser = 501;
    public const int ExceptionForMap = 502;
    public const int ExceptionString = 503;
    public const int GeoServiceRequest = 600;
    public const int GeoServiceRequestWithBody = 601;

    // Dynamic ids for the Start/Dispose performance-logger family, where the command is either a
    // fixed GeoServiceCommand or (for OGC) an arbitrary "service/request" string only known at
    // runtime, so those can't be [LoggerMessage] compile-time constants.
    public static readonly EventId Usage = new(200, "Usage");
    public static readonly EventId Datalinq = new(300, "Datalinq");

    public static EventId ToEventId(this GeoServiceCommand cmd) => new((int)cmd, cmd.ToString());

    public static EventId ToOgcEventId(this string cmd) => new(100, cmd);
}
