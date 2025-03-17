#nullable enable

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Logging;
using System;

namespace Api.Core.AppCode.Services.Logging;

internal class MicrosoftLog : ILog
{
    internal const string StructuredMessage = "{header}: {server} {service} - {command}: {message} {duration}ms {user} ({map_scale} - {max_x}, {max_y})";

    private ILogger _logger;
    private IMap? _map;
    private CmsDocument.UserIdentification? _ui;
    private string _header;
    private string _message;
    long _ticks;

    public MicrosoftLog(
        ILogger logger,
        IMap? map, CmsDocument.UserIdentification? ui,
        string header, string server, string service, string cmd, string message)
    {
        this.Server = server;
        this.Service = service;
        this.Command = cmd;

        _logger = logger;
        _map = map;
        _ui = ui;
        _header = header;
        _message = message;
        _ticks = DateTime.UtcNow.Ticks;
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

        _logger.LogInformation(StructuredMessage,
            _header, this.Server, this.Service, this.Command, this._message, durationTicks / 10000,
            _ui?.Username ?? "",
            Math.Round(_map?.MapScale ?? 0), _map?.Extent?.CenterPoint.X, _map?.Extent?.CenterPoint.Y);
    }
}