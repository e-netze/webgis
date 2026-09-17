#nullable enable

using System;
using System.Collections.Generic;

using E.Standard.CMS.Core;
using E.Standard.DbConnector;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Writes exceptions/error strings into a <c>webgis_exceptions</c> table (SQL Server/PostgreSQL/
/// SQLite - selected by the connection string's DB-engine prefix, see
/// <see cref="DbLoggingConnectionStrings"/>). The table is created automatically on first use
/// (<see cref="DbLoggingSchema"/>). Entries are buffered in memory and written in batches (one
/// connection/transaction per batch, not one per exception) - see
/// <see cref="BatchedDbLogBuffer{TRow}"/>. Registered as a singleton, so the buffer/timer live
/// for the lifetime of the process; the DI container disposes it (and flushes anything still
/// buffered) on shutdown since it implements <see cref="IDisposable"/>.
/// </summary>
public sealed class DbExceptionLogger : IExceptionLogger, IDisposable
{
    private readonly BatchedDbLogBuffer<ExceptionLogRow> _buffer;
    private readonly UsernameLoggingMode _usernameMode;

    public DbExceptionLogger(string connectionString, UsernameLoggingMode usernameMode = UsernameLoggingMode.PlainText)
    {
        DbLoggingSchema.EnsureExceptionsTableCreated(connectionString);
        _usernameMode = usernameMode;
        _buffer = new BatchedDbLogBuffer<ExceptionLogRow>(rows => WriteBatch(connectionString, rows));
    }

    public void Flush() => _buffer.Flush();

    public void Dispose() => _buffer.Dispose();

    public void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
        => _buffer.Enqueue(new ExceptionLogRow(DateTime.UtcNow, server, service, command, Map: null, UsernameLogging.Apply(_usernameMode, ui?.Username),
            ex.GetType().FullName, ex.Message, ex.StackTrace, SessionId: null, MapRequestId: null, ClientIp: null, CenterX: null, CenterY: null, Scale: null));

    public void LogException(IMap map, string server, string service, string command, Exception ex)
        => _buffer.Enqueue(new ExceptionLogRow(DateTime.UtcNow, server, service, command, map?.Name, GeoServiceLogContext.User(map, _usernameMode),
            ex.GetType().FullName, ex.Message, ex.StackTrace, GeoServiceLogContext.SessionId(map), GeoServiceLogContext.MapRequestId(map),
            GeoServiceLogContext.ClientIp(map), GeoServiceLogContext.CenterX(map), GeoServiceLogContext.CenterY(map), GeoServiceLogContext.Scale(map)));

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0)
        => _buffer.Enqueue(new ExceptionLogRow(DateTime.UtcNow, server, service, cmd, Map: null, UsernameLogging.Apply(_usernameMode, ui?.Username),
            ExceptionType: null, msg, StackTrace: null, SessionId: null, MapRequestId: null, ClientIp: null, CenterX: null, CenterY: null, Scale: null));

    private static void WriteBatch(string connectionString, IReadOnlyList<ExceptionLogRow> rows)
    {
        using var factory = new DBFactory(connectionString);
        using var connection = factory.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = factory.GetCommand(connection);
        command.Transaction = transaction;
        command.CommandText =
            $"insert into {DbLoggingSchema.ExceptionsTableName} " +
            "(timestamp_utc, server, service, command, map, \"user\", exception_type, message, stack_trace, " +
            "session_id, map_request_id, client_ip, center_x, center_y, scale) values " +
            $"({factory.ParaName("timestamp")}, {factory.ParaName("server")}, {factory.ParaName("service")}, " +
            $"{factory.ParaName("command")}, {factory.ParaName("map")}, {factory.ParaName("user")}, " +
            $"{factory.ParaName("extype")}, {factory.ParaName("message")}, {factory.ParaName("stacktrace")}, " +
            $"{factory.ParaName("sessionid")}, {factory.ParaName("requestid")}, {factory.ParaName("clientip")}, " +
            $"{factory.ParaName("centerx")}, {factory.ParaName("centery")}, {factory.ParaName("scale")})";

        var pTimestamp = factory.GetParameter("timestamp", DBNull.Value, typeof(DateTime));
        var pServer = factory.GetParameter("server", DBNull.Value, typeof(string));
        var pService = factory.GetParameter("service", DBNull.Value, typeof(string));
        var pCommand = factory.GetParameter("command", DBNull.Value, typeof(string));
        var pMap = factory.GetParameter("map", DBNull.Value, typeof(string));
        var pUser = factory.GetParameter("user", DBNull.Value, typeof(string));
        var pExceptionType = factory.GetParameter("extype", DBNull.Value, typeof(string));
        var pMessage = factory.GetParameter("message", DBNull.Value, typeof(string));
        var pStackTrace = factory.GetParameter("stacktrace", DBNull.Value, typeof(string));
        var pSessionId = factory.GetParameter("sessionid", DBNull.Value, typeof(string));
        var pRequestId = factory.GetParameter("requestid", DBNull.Value, typeof(string));
        var pClientIp = factory.GetParameter("clientip", DBNull.Value, typeof(string));
        var pCenterX = factory.GetParameter("centerx", DBNull.Value, typeof(double));
        var pCenterY = factory.GetParameter("centery", DBNull.Value, typeof(double));
        var pScale = factory.GetParameter("scale", DBNull.Value, typeof(double));

        command.Parameters.Add(pTimestamp);
        command.Parameters.Add(pServer);
        command.Parameters.Add(pService);
        command.Parameters.Add(pCommand);
        command.Parameters.Add(pMap);
        command.Parameters.Add(pUser);
        command.Parameters.Add(pExceptionType);
        command.Parameters.Add(pMessage);
        command.Parameters.Add(pStackTrace);
        command.Parameters.Add(pSessionId);
        command.Parameters.Add(pRequestId);
        command.Parameters.Add(pClientIp);
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
            pUser.Value = (object?)row.User ?? DBNull.Value;
            pExceptionType.Value = (object?)row.ExceptionType ?? DBNull.Value;
            pMessage.Value = (object?)row.Message ?? DBNull.Value;
            pStackTrace.Value = (object?)row.StackTrace ?? DBNull.Value;
            pSessionId.Value = (object?)row.SessionId ?? DBNull.Value;
            pRequestId.Value = (object?)row.MapRequestId ?? DBNull.Value;
            pClientIp.Value = (object?)row.ClientIp ?? DBNull.Value;
            pCenterX.Value = (object?)row.CenterX ?? DBNull.Value;
            pCenterY.Value = (object?)row.CenterY ?? DBNull.Value;
            pScale.Value = (object?)row.Scale ?? DBNull.Value;

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

