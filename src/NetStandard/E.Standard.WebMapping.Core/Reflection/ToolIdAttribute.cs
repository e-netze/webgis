namespace E.Standard.WebMapping.Core.Reflection;

public class ToolIdAttribute : System.Attribute
{
    public ToolIdAttribute(string toolId)
    {
        this.ToolId = toolId;
    }

    public string ToolId { get; private set; }
}
