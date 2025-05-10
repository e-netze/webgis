using E.DataLinq.Core.Engines.Abstraction;
using E.Standard.DbConnector;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqDbFactoryProviderConnectionStringModifyService : IDbFactoryProviderConnectionStringModifyService
{
    public string ModifyConnectionString(string dbPrefix, string connectionString) => dbPrefix?.ToLowerInvariant() switch
    {
        "sql" => connectionString.AppendSqlServerParametersIfNotExists(),
        "mssql" => connectionString.AppendSqlServerParametersIfNotExists(),
        "sqlserver" => connectionString.AppendSqlServerParametersIfNotExists(),
        _ => connectionString,
    };
}