using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IGeoServicePerformanceLogger
{
    /// <summary>
    /// Whether this logger actually records performance entries. Used by
    /// <see cref="GeoServicePerformanceLogMessage"/> to skip building the log message entirely
    /// when logging is disabled (e.g. a null/no-op logger).
    /// </summary>
    bool IsEnabled { get; }

    ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default);

    void Flush();
}
