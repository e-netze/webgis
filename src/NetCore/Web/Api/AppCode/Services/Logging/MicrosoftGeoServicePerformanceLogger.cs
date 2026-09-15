#nullable enable

using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftGeoServicePerformanceLogger : IGeoServicePerformanceLogger
{
    private ILogger<MicrosoftGeoServicePerformanceLogger> _logger;

    public MicrosoftGeoServicePerformanceLogger(ILogger<MicrosoftGeoServicePerformanceLogger> logger)
    {
        _logger = logger;
    }

    // Warning (not Information) - a failed request is logged at Warning by MicrosoftLog, so the
    // message must still be built whenever Warning is enabled, even if Information is not.
    public bool IsEnabled => _logger.IsEnabled(LogLevel.Warning);

    public void Flush() { }

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        // Always create the log entry - metrics/tracing (see GeoServiceTelemetry) are recorded
        // independently of the configured ILogger level, only the message text is skipped lazily.
        return new MicrosoftLog(
            _logger,
            map, null,
            "geoservice", cmd.ToEventId(),
            "WebGIS.API GeoService Performance", server, service, cmd.ToString(), message.ToString()
            );
    }
}
