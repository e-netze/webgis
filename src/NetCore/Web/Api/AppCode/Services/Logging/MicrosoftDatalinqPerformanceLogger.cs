#nullable enable

using System.Runtime.CompilerServices;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftDatalinqPerformanceLogger : IDatalinqPerformanceLogger
{
    private ILogger<MicrosoftDatalinqPerformanceLogger> _logger;

    public MicrosoftDatalinqPerformanceLogger(ILogger<MicrosoftDatalinqPerformanceLogger> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled => _logger.IsEnabled(LogLevel.Warning);

    public void Flush() { }

    public ILog Start(CmsDocument.UserIdentification ui, string server, string service, string cmd,
        [InterpolatedStringHandlerArgument("")] UsagePerformanceLogMessage message = default)
    {
        return new MicrosoftLog(
            _logger,
            null, ui,
            "datalinq", LoggingEventIds.Datalinq,
            "WebGIS.API DataLinq Performance", server, service, cmd, message.ToString()
            );
    }
}
