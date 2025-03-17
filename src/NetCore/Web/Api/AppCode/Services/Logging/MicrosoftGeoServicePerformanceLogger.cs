#nullable enable

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

    public void Flush() { }

    public ILog Start(IMap map, string server, string service, string cmd, string message)
    {
        return new MicrosoftLog(
            _logger,
            map, null,
            "WebGIS.API GeoService Performance", server, service, cmd, message
            );
    }
}