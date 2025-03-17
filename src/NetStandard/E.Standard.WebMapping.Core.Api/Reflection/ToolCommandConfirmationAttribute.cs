namespace E.Standard.WebMapping.Core.Api.Reflection;


public class ToolCommandConfirmationAttribute : System.Attribute
{
    public ToolCommandConfirmationAttribute(string message, ApiToolConfirmationType type = ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType eventType = ApiToolConfirmationEventType.ButtonClick)
    {
        this.Message = message;
        this.Type = type;
        this.EventType = eventType;
    }

    public string Message { get; set; }
    public ApiToolConfirmationType Type { get; set; }
    public ApiToolConfirmationEventType EventType { get; set; }
}
