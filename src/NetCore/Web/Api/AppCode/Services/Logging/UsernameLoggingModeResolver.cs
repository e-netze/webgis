#nullable enable

using System;

using E.Standard.Api.App.Configuration;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging;

/// <summary>
/// Resolves the <c>Api:logging-username-mode</c> setting into a <see cref="UsernameLoggingMode"/>
/// - shared by every logger backend (CSV/Microsoft/database) so they all interpret the same
/// config value identically.
/// </summary>
internal static class UsernameLoggingModeResolver
{
    public static UsernameLoggingMode Resolve(IConfiguration configuration) =>
        configuration[ApiConfigKeys.LoggingUsernameMode]?.Trim().ToLowerInvariant() switch
        {
            "none" => UsernameLoggingMode.None,
            "hash" => UsernameLoggingMode.Hash,
            "plaintext" => UsernameLoggingMode.PlainText,
            // Unset/unrecognized - behave exactly like before this setting existed (plain-text
            // username, if the backend/column-list logs it at all).
            _ => UsernameLoggingMode.PlainText,
        };
}
