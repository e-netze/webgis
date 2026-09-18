#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using E.Standard.DbConnector;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Writes GeoService performance entries into a <c>webgis_performance</c> table (SQL Server/
/// PostgreSQL/SQLite/Oracle - selected by the connection string's DB-engine prefix, see
/// <see cref="DbLoggingConnectionStrings"/>). The table is created automatically on first use
/// (<see cref="DbLoggingSchema"/>). Entries are buffered in memory and written in batches (one
/// connection/transaction per batch, not one per request) - see
/// <see cref="BatchedDbLogBuffer{TRow}"/>. Registered as a singleton, so the buffer/timer live
/// for the lifetime of the process; the DI container disposes it (and flushes anything still
/// buffered) on shutdown since it implements <see cref="IDisposable"/>.
/// </summary>
public sealed class DbGeoServicePerformanceLogger : IGeoServicePerformanceLogger, IDisposable
{
    private readonly BatchedDbLogBuffer<PerformanceLogRow> _buffer;
    private readonly UsernameLoggingMode _usernameMode;

    public DbGeoServicePerformanceLogger(string connectionString, UsernameLoggingMode usernameMode = UsernameLoggingMode.PlainText)
    {
        DbLoggingSchema.EnsurePerformanceTableCreated(connectionString);
        _usernameMode = usernameMode;
        _buffer = new BatchedDbLogBuffer<PerformanceLogRow>(rows => WriteBatch(connectionString, rows), batchSize: 2000, flushInterval: TimeSpan.FromSeconds(60));
    }

    public bool IsEnabled => true;

    public void Flush() => _buffer.Flush();

    public void Dispose() => _buffer.Dispose();

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
        => new DbPerformanceLog(_buffer, server, service, cmd.ToString(), map?.Name, message.ToString(),
            GeoServiceLogContext.SessionId(map), GeoServiceLogContext.MapRequestId(map), GeoServiceLogContext.ClientIp(map),
            GeoServiceLogContext.User(map, _usernameMode), GeoServiceLogContext.CenterX(map), GeoServiceLogContext.CenterY(map), GeoServiceLogContext.Scale(map));

    private static void WriteBatch(string connectionString, IReadOnlyList<PerformanceLogRow> rows)
    {
        using var factory = new DBFactory(connectionString);
        using var connection = factory.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = factory.GetCommand(connection);
        command.Transaction = transaction;
        command.CommandText =
            $"insert into {DbLoggingSchema.PerformanceTableName} " +
            "(timestamp_utc, server, service, command, map, success, duration_ms, message, " +
            $"session_id, map_request_id, client_ip, {DbLoggingSchema.QuoteIdentifier(factory.DatabaseType, "user")}, center_x, center_y, scale) values " +
            $"({factory.ParaName("timestamp")}, {factory.ParaName("server")}, {factory.ParaName("service")}, " +
            $"{factory.ParaName("command")}, {factory.ParaName("map")}, {factory.ParaName("success")}, " +
            $"{factory.ParaName("duration")}, {factory.ParaName("message")}, " +
            $"{factory.ParaName("sessionid")}, {factory.ParaName("requestid")}, {factory.ParaName("clientip")}, " +
            $"{factory.ParaName("user")}, {factory.ParaName("centerx")}, {factory.ParaName("centery")}, {factory.ParaName("scale")})";

        var pTimestamp = factory.GetParameter("timestamp", DBNull.Value, typeof(DateTime));
        var pServer = factory.GetParameter("server", DBNull.Value, typeof(string));
        var pService = factory.GetParameter("service", DBNull.Value, typeof(string));
        var pCommand = factory.GetParameter("command", DBNull.Value, typeof(string));
        var pMap = factory.GetParameter("map", DBNull.Value, typeof(string));
        var pSuccess = factory.GetParameter("success", DBNull.Value, typeof(bool));
        var pDuration = factory.GetParameter("duration", DBNull.Value, typeof(double));
        var pMessage = factory.GetParameter("message", DBNull.Value, typeof(string));
        var pSessionId = factory.GetParameter("sessionid", DBNull.Value, typeof(string));
        var pRequestId = factory.GetParameter("requestid", DBNull.Value, typeof(string));
        var pClientIp = factory.GetParameter("clientip", DBNull.Value, typeof(string));
        var pUser = factory.GetParameter("user", DBNull.Value, typeof(string));
        var pCenterX = factory.GetParameter("centerx", DBNull.Value, typeof(double));
        var pCenterY = factory.GetParameter("centery", DBNull.Value, typeof(double));
        var pScale = factory.GetParameter("scale", DBNull.Value, typeof(double));

        command.Parameters.Add(pTimestamp);
        command.Parameters.Add(pServer);
        command.Parameters.Add(pService);
        command.Parameters.Add(pCommand);
        command.Parameters.Add(pMap);
        command.Parameters.Add(pSuccess);
        command.Parameters.Add(pDuration);
        command.Parameters.Add(pMessage);
        command.Parameters.Add(pSessionId);
        command.Parameters.Add(pRequestId);
        command.Parameters.Add(pClientIp);
        command.Parameters.Add(pUser);
        command.Parameters.Add(pCenterX);
        command.Parameters.Add(pCenterY);
        command.Parameters.Add(pScale);

        // Reuse the same prepared command/parameters for every row - a single connection and
        // transaction for the whole batch, one commit at the end.
        foreach (var row in rows)
        {
            pTimestamp.Value = row.TimestampUtc;
            pServer.Value = (object?)row.Server ?? DBNull.Value;
            pService.Value = (object?)row.Service ?? DBNull.Value;
            pCommand.Value = (object?)row.Command ?? DBNull.Value;
            pMap.Value = (object?)row.Map ?? DBNull.Value;
            pSuccess.Value = row.Success;
            pDuration.Value = row.DurationMs;
            pMessage.Value = (object?)row.Message ?? DBNull.Value;
            pSessionId.Value = (object?)row.SessionId ?? DBNull.Value;
            pRequestId.Value = (object?)row.MapRequestId ?? DBNull.Value;
            pClientIp.Value = (object?)row.ClientIp ?? DBNull.Value;
            pUser.Value = (object?)row.User ?? DBNull.Value;
            pCenterX.Value = (object?)row.CenterX ?? DBNull.Value;
            pCenterY.Value = (object?)row.CenterY ?? DBNull.Value;
            pScale.Value = (object?)row.Scale ?? DBNull.Value;

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

