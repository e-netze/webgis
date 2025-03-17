using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftWarningsLogger : IWarningsLogger
{
    private ILogger<MicrosoftWarningsLogger> _logger;

    public MicrosoftWarningsLogger(ILogger<MicrosoftWarningsLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string command, string message, int performaceMilliseconds = 0)
    {
        _logger.LogWarning("WebGIS.API: {server} {service} {command} {message} - {username}", service, service, command, message, ui?.Username ?? "");
    }
}
