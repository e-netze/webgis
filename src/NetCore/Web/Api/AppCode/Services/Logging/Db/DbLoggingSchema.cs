#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

using E.Standard.DbConnector;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Creates the <c>webgis_performance</c>/<c>webgis_exceptions</c> tables the first time a
/// DB-backed logger for a given connection string is constructed, so no manual DB
/// setup/migration step is needed for the "sqlserver"/"postgres"/"sqlite"/"oracle"
/// <c>Api:logging-type</c> values - matching how the "files" type also creates its log files on
/// demand. If the table already exists (e.g. created by an earlier version of this feature,
/// before a column was added), any of the columns below that are still missing are added
/// automatically too (<c>ALTER TABLE ... ADD ...</c>) - so upgrading never requires a manual
/// schema migration.
/// </summary>
internal static class DbLoggingSchema
{
    public const string PerformanceTableName = "webgis_performance";
    public const string ExceptionsTableName = "webgis_exceptions";

    // One create/migrate-attempt per (table, connection string) is enough for the lifetime of
    // the process - avoids hitting the database on every single logged request just to re-check
    // whether the table/columns already exist.
    private static readonly ConcurrentDictionary<string, bool> _ensured = new();

    // (column name, SQL Server type, PostgreSQL type, SQLite type, Oracle type) - shared between
    // the initial CREATE TABLE and the ALTER TABLE fallback for tables that already existed with
    // fewer columns.
    private static readonly (string Name, string SqlServerType, string PostgresType, string SqliteType, string OracleType)[] PerformanceColumns =
    [
        ("session_id", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("map_request_id", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("client_ip", "NVARCHAR(100) NULL", "VARCHAR(100)", "TEXT", "NVARCHAR2(100)"),
        ("user", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("center_x", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
        ("center_y", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
        ("scale", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
    ];

    private static readonly (string Name, string SqlServerType, string PostgresType, string SqliteType, string OracleType)[] ExceptionsColumns =
    [
        ("user", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("session_id", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("map_request_id", "NVARCHAR(200) NULL", "VARCHAR(200)", "TEXT", "NVARCHAR2(200)"),
        ("client_ip", "NVARCHAR(100) NULL", "VARCHAR(100)", "TEXT", "NVARCHAR2(100)"),
        ("center_x", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
        ("center_y", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
        ("scale", "FLOAT NULL", "DOUBLE PRECISION", "REAL", "BINARY_DOUBLE"),
    ];

    public static void EnsurePerformanceTableCreated(string connectionString)
        => EnsureTableCreated(connectionString, PerformanceTableName, PerformanceCreateTableSql, PerformanceColumns);

    public static void EnsureExceptionsTableCreated(string connectionString)
        => EnsureTableCreated(connectionString, ExceptionsTableName, ExceptionsCreateTableSql, ExceptionsColumns);

    private static void EnsureTableCreated(string connectionString, string tableName, Func<DBType, string> createTableSql,
        (string Name, string SqlServerType, string PostgresType, string SqliteType, string OracleType)[] columns)
    {
        if (!_ensured.TryAdd($"{tableName}|{connectionString}", true))
        {
            return;
        }

        using var factory = new DBFactory(connectionString);

        if (!factory.TableExits(tableName))
        {
            using var connection = factory.GetConnection();
            connection.Open();

            var command = factory.GetCommand(connection);
            command.CommandText = createTableSql(factory.DatabaseType);
            command.ExecuteNonQuery();

            if (factory.DatabaseType == DBType.oracle)
            {
                // Oracle has no cross-version auto-increment column syntax (GENERATED ... AS
                // IDENTITY needs 12c+) - reuse the sequence+trigger fallback already used
                // elsewhere in the codebase for Oracle "serial" primary keys instead of assuming
                // a minimum Oracle version.
                foreach (var afterCreateCommand in factory.AfterCreateTableCommands(tableName, new[] { "id" }))
                {
                    command.CommandText = afterCreateCommand;
                    command.ExecuteNonQuery();
                }
            }

            return;
        }

        using var existingConnection = factory.GetConnection();
        existingConnection.Open();
        EnsureColumnsExist(factory, existingConnection, tableName, columns);
    }

    private static void EnsureColumnsExist(DBFactory factory, DbConnection connection, string tableName,
        (string Name, string SqlServerType, string PostgresType, string SqliteType, string OracleType)[] columns)
    {
        var existingColumns = GetExistingColumns(factory, connection, tableName);

        foreach (var column in columns)
        {
            if (existingColumns.Contains(column.Name))
            {
                continue;
            }

            string columnType = factory.DatabaseType switch
            {
                DBType.sql or DBType.microsoftsql => column.SqlServerType,
                DBType.postgres => column.PostgresType,
                DBType.sqlite => column.SqliteType,
                DBType.oracle => column.OracleType,
                _ => throw new NotSupportedException($"DB logging schema migration is not supported for '{factory.DatabaseType}'."),
            };

            var alterCommand = factory.GetCommand(connection);
            // "COLUMN" is optional in the ADD-column grammar of PostgreSQL/SQLite, but SQL
            // Server's ADD only accepts "ADD name type" (no "COLUMN" keyword at all) - so it's
            // left out here to stay valid on all four dialects.
            alterCommand.CommandText = $"ALTER TABLE {tableName} ADD {QuoteIdentifier(factory.DatabaseType, column.Name)} {columnType}";
            alterCommand.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Quotes an identifier consistently with how it was written in the initial CREATE TABLE
    /// (<see cref="PerformanceCreateTableSql"/>/<see cref="ExceptionsCreateTableSql"/>), so a
    /// column added later via ALTER TABLE (for tables created by an earlier version of this
    /// feature) ends up with the exact same casing. Also used by
    /// <see cref="DbGeoServicePerformanceLogger"/>/<see cref="DbExceptionLogger"/> to reference
    /// the "user" column in their INSERT statements with the matching casing.
    /// </summary>
    internal static string QuoteIdentifier(DBType dbType, string columnName)
    {
        if (dbType == DBType.oracle)
        {
            // Oracle folds unquoted identifiers to uppercase, so the CREATE TABLE below leaves
            // regular columns unquoted (letting them fold, the idiomatic Oracle style) and only
            // quotes "user" (a reserved word/pseudo-column there too) as "USER" to match that
            // folding. Quoting every other column here too would instead create a *lowercase*,
            // case-sensitive column next to the already-uppercase ones from CREATE TABLE.
            return columnName == "user" ? "\"USER\"" : columnName;
        }

        // SQL Server/PostgreSQL/SQLite: always quote - "user" needs it (a reserved word there),
        // and quoting a non-reserved identifier too is harmless on all three dialects.
        return $"\"{columnName}\"";
    }

    private static HashSet<string> GetExistingColumns(DBFactory factory, DbConnection connection, string tableName)
    {
        var command = factory.GetCommand(connection);
        command.CommandText = factory.DatabaseType switch
        {
            DBType.sqlite => $"PRAGMA table_info({tableName})",
            // Oracle stores unquoted (i.e. the vast majority of our columns) identifiers folded
            // to uppercase in the data dictionary - USER_TAB_COLUMNS is scoped to the current
            // schema, which is exactly the table this connection's user just created.
            DBType.oracle => $"SELECT column_name FROM user_tab_columns WHERE table_name = '{tableName.ToUpperInvariant()}'",
            _ => $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'",
        };

        using var reader = command.ExecuteReader();

        int nameOrdinal = factory.DatabaseType == DBType.sqlite
            ? reader.GetOrdinal("name")
            : reader.GetOrdinal("column_name");

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            existing.Add(reader.GetString(nameOrdinal));
        }

        return existing;
    }

    private static string PerformanceCreateTableSql(DBType dbType) => dbType switch
    {
        DBType.sql or DBType.microsoftsql =>
            """
            IF OBJECT_ID(N'webgis_performance', N'U') IS NULL
            CREATE TABLE webgis_performance (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                timestamp_utc DATETIME2 NOT NULL,
                server NVARCHAR(400) NULL,
                service NVARCHAR(400) NULL,
                command NVARCHAR(100) NULL,
                map NVARCHAR(200) NULL,
                success BIT NOT NULL,
                duration_ms FLOAT NOT NULL,
                message NVARCHAR(MAX) NULL,
                session_id NVARCHAR(200) NULL,
                map_request_id NVARCHAR(200) NULL,
                client_ip NVARCHAR(100) NULL,
                "user" NVARCHAR(200) NULL,
                center_x FLOAT NULL,
                center_y FLOAT NULL,
                scale FLOAT NULL
            )
            """,
        DBType.postgres =>
            """
            CREATE TABLE IF NOT EXISTS webgis_performance (
                id BIGSERIAL PRIMARY KEY,
                timestamp_utc TIMESTAMP NOT NULL,
                server VARCHAR(400),
                service VARCHAR(400),
                command VARCHAR(100),
                map VARCHAR(200),
                success BOOLEAN NOT NULL,
                duration_ms DOUBLE PRECISION NOT NULL,
                message TEXT,
                session_id VARCHAR(200),
                map_request_id VARCHAR(200),
                client_ip VARCHAR(100),
                "user" VARCHAR(200),
                center_x DOUBLE PRECISION,
                center_y DOUBLE PRECISION,
                scale DOUBLE PRECISION
            )
            """,
        DBType.sqlite =>
            """
            CREATE TABLE IF NOT EXISTS webgis_performance (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                server TEXT,
                service TEXT,
                command TEXT,
                map TEXT,
                success INTEGER NOT NULL,
                duration_ms REAL NOT NULL,
                message TEXT,
                session_id TEXT,
                map_request_id TEXT,
                client_ip TEXT,
                "user" TEXT,
                center_x REAL,
                center_y REAL,
                scale REAL
            )
            """,
        DBType.oracle =>
            """
            CREATE TABLE webgis_performance (
                id NUMBER(19) PRIMARY KEY,
                timestamp_utc TIMESTAMP(3) NOT NULL,
                server NVARCHAR2(400),
                service NVARCHAR2(400),
                command NVARCHAR2(100),
                map NVARCHAR2(200),
                success NUMBER(1) NOT NULL,
                duration_ms BINARY_DOUBLE NOT NULL,
                message CLOB,
                session_id NVARCHAR2(200),
                map_request_id NVARCHAR2(200),
                client_ip NVARCHAR2(100),
                "USER" NVARCHAR2(200),
                center_x BINARY_DOUBLE,
                center_y BINARY_DOUBLE,
                scale BINARY_DOUBLE
            )
            """,
        _ => throw new NotSupportedException($"DB performance logging is not supported for '{dbType}'."),
    };

    private static string ExceptionsCreateTableSql(DBType dbType) => dbType switch
    {
        DBType.sql or DBType.microsoftsql =>
            """
            IF OBJECT_ID(N'webgis_exceptions', N'U') IS NULL
            CREATE TABLE webgis_exceptions (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                timestamp_utc DATETIME2 NOT NULL,
                server NVARCHAR(400) NULL,
                service NVARCHAR(400) NULL,
                command NVARCHAR(200) NULL,
                map NVARCHAR(200) NULL,
                "user" NVARCHAR(200) NULL,
                exception_type NVARCHAR(400) NULL,
                message NVARCHAR(MAX) NULL,
                stack_trace NVARCHAR(MAX) NULL,
                session_id NVARCHAR(200) NULL,
                map_request_id NVARCHAR(200) NULL,
                client_ip NVARCHAR(100) NULL,
                center_x FLOAT NULL,
                center_y FLOAT NULL,
                scale FLOAT NULL
            )
            """,
        DBType.postgres =>
            """
            CREATE TABLE IF NOT EXISTS webgis_exceptions (
                id BIGSERIAL PRIMARY KEY,
                timestamp_utc TIMESTAMP NOT NULL,
                server VARCHAR(400),
                service VARCHAR(400),
                command VARCHAR(200),
                map VARCHAR(200),
                "user" VARCHAR(200),
                exception_type VARCHAR(400),
                message TEXT,
                stack_trace TEXT,
                session_id VARCHAR(200),
                map_request_id VARCHAR(200),
                client_ip VARCHAR(100),
                center_x DOUBLE PRECISION,
                center_y DOUBLE PRECISION,
                scale DOUBLE PRECISION
            )
            """,
        DBType.sqlite =>
            """
            CREATE TABLE IF NOT EXISTS webgis_exceptions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                server TEXT,
                service TEXT,
                command TEXT,
                map TEXT,
                "user" TEXT,
                exception_type TEXT,
                message TEXT,
                stack_trace TEXT,
                session_id TEXT,
                map_request_id TEXT,
                client_ip TEXT,
                center_x REAL,
                center_y REAL,
                scale REAL
            )
            """,
        DBType.oracle =>
            """
            CREATE TABLE webgis_exceptions (
                id NUMBER(19) PRIMARY KEY,
                timestamp_utc TIMESTAMP(3) NOT NULL,
                server NVARCHAR2(400),
                service NVARCHAR2(400),
                command NVARCHAR2(200),
                map NVARCHAR2(200),
                "USER" NVARCHAR2(200),
                exception_type NVARCHAR2(400),
                message CLOB,
                stack_trace CLOB,
                session_id NVARCHAR2(200),
                map_request_id NVARCHAR2(200),
                client_ip NVARCHAR2(100),
                center_x BINARY_DOUBLE,
                center_y BINARY_DOUBLE,
                scale BINARY_DOUBLE
            )
            """,
        _ => throw new NotSupportedException($"DB exception logging is not supported for '{dbType}'."),
    };
}
