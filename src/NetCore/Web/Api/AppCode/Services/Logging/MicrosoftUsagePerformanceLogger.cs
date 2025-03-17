#nullable enable

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftUsagePerformanceLogger : IUsagePerformanceLogger
{
    private ILogger<MicrosoftUsagePerformanceLogger> _logger;

    public MicrosoftUsagePerformanceLogger(ILogger<MicrosoftUsagePerformanceLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public ILog Start(CmsDocument.UserIdentification ui, string server, string service, string cmd, string message)
    {
        return new MicrosoftLog(
            _logger,
            null, ui,
            "WebGIS.API Usage Performance", server, service, cmd, message
            );
    }
}
