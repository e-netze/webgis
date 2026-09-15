using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public partial class MicrosoftWarningsLogger : IWarningsLogger
{
    private readonly ILogger<MicrosoftWarningsLogger> _logger;

    public MicrosoftWarningsLogger(ILogger<MicrosoftWarningsLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string command, string message, int performaceMilliseconds = 0)
        => LogWarningCore(server, service, command, message, ui?.Username ?? "");

    [LoggerMessage(EventId = LoggingEventIds.Warning, Level = LogLevel.Warning, Message = "WebGIS.API: {server} {service} {command} {message} - {username}")]
    private partial void LogWarningCore(string server, string service, string command, string message, string username);
}
