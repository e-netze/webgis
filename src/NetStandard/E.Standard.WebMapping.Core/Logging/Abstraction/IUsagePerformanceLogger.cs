using System.Runtime.CompilerServices;

using E.Standard.CMS.Core;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IUsagePerformanceLogger
{
    /// <summary>
    /// Whether this logger actually records performance entries. Used by
    /// <see cref="UsagePerformanceLogMessage"/> to skip building the log message entirely
    /// when logging is disabled (e.g. a null/no-op logger).
    /// </summary>
    bool IsEnabled { get; }

    ILog Start(CmsDocument.UserIdentification ui, string server, string service, string cmd,
        [InterpolatedStringHandlerArgument("")] UsagePerformanceLogMessage message = default);

    void Flush();
}
