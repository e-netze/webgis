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

    public bool IsEnabled => _logger.IsEnabled(LogLevel.Information);

    public void Flush() { }

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        if (!this.IsEnabled)
        {
            return NullLog.Instance;
        }

        return new MicrosoftLog(
            _logger,
            map, null,
            "WebGIS.API GeoService Performance", server, service, cmd.ToString(), message.ToString()
            );
    }
}
