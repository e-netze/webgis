using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class ApiToolEventDTO
{
    private IApiToolEvent _event;
    public ApiToolEventDTO(IApiToolEvent @event)
    {
        _event = @event;
    }

    public string @event
    {
        get { return _event.Event.ToString().ToLower().Replace("_", "-"); }
    }
    public string command
    {
        get { return _event.ToolCommand; }
    }
}