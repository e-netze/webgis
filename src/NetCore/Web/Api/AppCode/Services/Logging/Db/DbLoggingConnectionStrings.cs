#nullable enable

using E.Standard.Api.App.Configuration;

using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Resolves the api.config connection-string setting for a "sqlserver"/"postgres"/"sqlite"
/// <c>Api:logging-type</c> entry and prefixes it for <see cref="E.Standard.DbConnector.DBConnection"/>
/// (which dispatches to the right ADO.NET provider based on a "mssql:"/"postgres:"/"sqlite:" prefix).
/// </summary>
internal static class DbLoggingConnectionStrings
{
    public static string? ForLoggingType(string loggingType, IConfiguration configuration) => loggingType switch
    {
        "sqlserver" => Prefixed("mssql", configuration[ApiConfigKeys.LoggingSqlServerConnectionString]),
        "postgres" => Prefixed("postgres", configuration[ApiConfigKeys.LoggingPostgresConnectionString]),
        "sqlite" => Prefixed("sqlite", configuration[ApiConfigKeys.LoggingSqliteConnectionString]),
        _ => null,
    };

    private static string? Prefixed(string enginePrefix, string? rawConnectionString)
        => string.IsNullOrWhiteSpace(rawConnectionString) ? null : $"{enginePrefix}:{rawConnectionString}";
}
