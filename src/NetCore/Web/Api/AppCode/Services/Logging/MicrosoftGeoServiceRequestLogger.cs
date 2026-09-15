using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

/// <summary>
/// <see cref="IGeoServiceRequestLogger"/> implementation writing to <see cref="ILogger"/> at
/// <see cref="LogLevel.Trace"/>, as an alternative to the file based <c>SimpleServiceRequestLogger</c>.
/// </summary>
public partial class MicrosoftGeoServiceRequestLogger : IGeoServiceRequestLogger
{
    private readonly ILogger<MicrosoftGeoServiceRequestLogger> _logger;

    public MicrosoftGeoServiceRequestLogger(ILogger<MicrosoftGeoServiceRequestLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public void LogString(string server, string service, string command, string msg, int performaceMilliseconds = 0, bool success = true)
        => LogTrace(server, service, command, msg);

    public ILog PerformanceLogger(string server, string service, string cmd, string message)
    {
        return NullLog.Instance;
    }

    public IWebGISLogger Clone(IMap map) => this;

    public IWebGISLogger Clone(CmsDocument.UserIdentification ui) => this;

    [LoggerMessage(EventId = LoggingEventIds.GeoServiceRequest, Level = LogLevel.Trace, Message = "WebGIS.API GeoService Request: {server} {service} {command} - {message}")]
    private partial void LogTrace(string server, string service, string command, string message);
}
