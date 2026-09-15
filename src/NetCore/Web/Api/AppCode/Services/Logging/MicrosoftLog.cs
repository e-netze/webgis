#nullable enable

using System;
using System.Diagnostics;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

internal class MicrosoftLog : ILog
{
    internal const string StructuredMessage = "{header}: {server} {service} - {command}: {message} {duration}ms {user} ({map_scale} - {max_x}, {max_y})";

    private readonly ILogger _logger;
    private readonly IMap? _map;
    private readonly CmsDocument.UserIdentification? _ui;
    private readonly string _header;
    private readonly string _category;
    private readonly EventId _eventId;
    private readonly Activity? _activity;
    private readonly long _ticks;
    private string _message;

    public MicrosoftLog(
        ILogger logger,
        IMap? map, CmsDocument.UserIdentification? ui,
        string category, EventId eventId,
        string header, string server, string service, string cmd, string message)
    {
        this.Server = server;
        this.Service = service;
        this.Command = cmd;

        _logger = logger;
        _map = map;
        _ui = ui;
        _header = header;
        _category = category;
        _eventId = eventId;
        _message = message;
        _ticks = DateTime.UtcNow.Ticks;

        _activity = GeoServiceTelemetry.ActivitySource.StartActivity($"{category}:{cmd} {service}");
        if (_activity is not null)
        {
            _activity.SetTag("webgis.category", category);
            _activity.SetTag("webgis.command", cmd);
            _activity.SetTag("webgis.server", server);
            _activity.SetTag("webgis.service", service);
            if (!String.IsNullOrEmpty(map?.Name))
            {
                _activity.SetTag("webgis.map", map.Name);
            }
        }
    }

    public bool Success { get; set; }
    public bool SuppressLogging { get; set; }

    public string Server { get; internal set; }

    public string Service { get; internal set; }

    public string Command { get; internal set; }

    public void AppendToMessage(string message)
    {
        this._message += message;
    }

    public void Dispose()
    {
        long durationTicks = DateTime.UtcNow.Ticks - _ticks;
        double durationMs = durationTicks / 10000d;

        if (this.SuppressLogging)
        {
            // Cached/duplicate requests are not real performance data - matches the behavior of
            // SimpleFilePerformanceLogger, which also skips these entirely.
            _activity?.Dispose();
            return;
        }

        GeoServiceTelemetry.RecordRequest(_category, this.Command, this.Server, this.Service, this.Success, durationMs);

        if (_activity is not null)
        {
            _activity.SetTag("webgis.success", this.Success);
            _activity.SetStatus(this.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            _activity.Dispose();
        }

        // Failed requests are logged at Warning so they can be found/alerted on via plain
        // log-level filtering, without having to parse the message text.
        var level = this.Success ? LogLevel.Information : LogLevel.Warning;
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        _logger.Log(level, _eventId, StructuredMessage,
            _header, this.Server, this.Service, this.Command, this._message, (long)durationMs,
            _ui?.Username ?? "",
            Math.Round(_map?.MapScale ?? 0), _map?.Extent?.CenterPoint.X, _map?.Extent?.CenterPoint.Y);
    }
}

