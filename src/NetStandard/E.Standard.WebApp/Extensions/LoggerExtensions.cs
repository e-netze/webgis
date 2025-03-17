using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text;

namespace E.Standard.WebApp.Extensions;

static public class LoggerExtensions
{
    static public ILogger LogClaims(this ILogger logger, LogLevel logLevel, ClaimsPrincipal principal)
    {

        if (logger.IsEnabled(logLevel) == false
            || principal?.Claims is null)
        {
            return logger;
        }

        StringBuilder sb = new StringBuilder();

        foreach (var claim in principal.Claims)
        {
            sb.Append($"Claim {claim.Type} = {claim.Value}");
            sb.Append(Environment.NewLine);
        }

        logger.Log(logLevel, sb.ToString());

        return logger;
    }
}
