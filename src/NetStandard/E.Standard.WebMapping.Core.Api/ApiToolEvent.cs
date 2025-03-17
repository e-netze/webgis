using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebMapping.Core.Api.UI;

public class ApiToolEvent : IApiToolEvent
{
    public ApiToolEvent(ApiToolEvents @event, string toolCommand)
    {
        this.Event = @event;
        this.ToolCommand = toolCommand;
    }

    #region IApiToolEvent Member

    public ApiToolEvents Event
    {
        get;
        private set;
    }

    public string ToolCommand
    {
        get;
        private set;
    }

    #endregion
}
