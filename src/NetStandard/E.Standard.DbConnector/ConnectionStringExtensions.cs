#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.DbConnector;
public static class ConnectionStringExtensions
{
    static public string AddRequiredConnectionStringParameters(this string connectionString, DBType dbType)
    {
        return dbType switch
        {
            DBType.sql => connectionString.AppendSqlServerParametersIfNotExists(),
            DBType.microsoftsql => connectionString.AppendSqlServerParametersIfNotExists(),
            _ => connectionString,
        };
    }

    static public string AddRequiredConnectionStringParameters(this string connectionString, string dbType)
    {
        return dbType?.ToLowerInvariant() switch
        {
            "sql" => connectionString.AppendSqlServerParametersIfNotExists(),
            "mssql" => connectionString.AppendSqlServerParametersIfNotExists(),
            _ => connectionString,
        };
    }

    #region Sql Server

    static public Dictionary<string, string> SqlServerParametersToAppend = new Dictionary<string, string>()
    {
        { "TrustServerCertificate", "true" },
        { "Encrypt", "true" },
        //{ "Integrated Security", "false" }
    };

    static public void SetSqlServerParametersToAppend(this string[]? parameters)
    {
        if (parameters == null) // use the defaults
        {
            return;
        }

        SqlServerParametersToAppend.Clear();

        foreach (var parameter in parameters)
        {
            var keyValue = parameter.Split('=');
            if (keyValue.Length == 2)
            {
                SqlServerParametersToAppend.Add(keyValue[0].Trim(), keyValue[1].Trim());
            }
        }
    }

    static public string AppendSqlServerParametersIfNotExists(this string connectionString)
    {
        var sb = new StringBuilder(connectionString);

        var connectionStringWithoutSpaces = (connectionString ?? "").Replace(" ", "").Trim();
        if (!connectionStringWithoutSpaces.EndsWith(";"))
        {
            sb.Append(";");
        }

        foreach (var parameter in SqlServerParametersToAppend)
        {
            if (!connectionStringWithoutSpaces.Contains($"{parameter.Key}=", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($"{parameter.Key}={parameter.Value};");
            }
        }

        return sb.ToString();
    }

    #endregion
}
