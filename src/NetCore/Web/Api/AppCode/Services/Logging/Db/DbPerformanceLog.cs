#nullable enable

using System;

using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Measures a single GeoService request and, on <see cref="Dispose"/>, enqueues one row into the
/// shared <see cref="BatchedDbLogBuffer{TRow}"/> instead of writing to the database directly -
/// the actual DB write happens later, batched (see <see cref="DbGeoServicePerformanceLogger"/>).
/// </summary>
internal sealed class DbPerformanceLog : ILog
{
    private readonly BatchedDbLogBuffer<PerformanceLogRow> _buffer;
    private readonly string? _map;
    private readonly string? _sessionId;
    private readonly string? _mapRequestId;
    private readonly string? _clientIp;
    private readonly string? _user;
    private readonly double? _centerX;
    private readonly double? _centerY;
    private readonly double? _scale;
    private readonly long _startTicks;
    private string _message;

    public DbPerformanceLog(BatchedDbLogBuffer<PerformanceLogRow> buffer, string server, string service, string command, string? map, string message,
        string? sessionId, string? mapRequestId, string? clientIp, string? user, double? centerX, double? centerY, double? scale)
    {
        _buffer = buffer;
        Server = server;
        Service = service;
        Command = command;
        _map = map;
        _message = message;
        _sessionId = sessionId;
        _mapRequestId = mapRequestId;
        _clientIp = clientIp;
        _user = user;
        _centerX = centerX;
        _centerY = centerY;
        _scale = scale;
        _startTicks = DateTime.UtcNow.Ticks;
    }

    public bool Success { get; set; }
    public bool SuppressLogging { get; set; }

    public string Server { get; }
    public string Service { get; }
    public string Command { get; }

    public void AppendToMessage(string message) => _message += message;

    public void Dispose()
    {
        if (SuppressLogging)
        {
            // Cached/duplicate requests are not real performance data - matches the behavior of
            // the file/Microsoft performance loggers, which also skip these entirely.
            return;
        }

        double durationMs = (DateTime.UtcNow.Ticks - _startTicks) / 10000d;

        _buffer.Enqueue(new PerformanceLogRow(DateTime.UtcNow, Server, Service, Command, _map, Success, durationMs, _message,
            _sessionId, _mapRequestId, _clientIp, _user, _centerX, _centerY, _scale));
    }
}
