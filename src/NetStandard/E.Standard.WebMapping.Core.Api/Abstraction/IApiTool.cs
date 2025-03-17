namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiTool : IApiButton
{
    ToolType Type { get; }

    ToolCursor Cursor { get; }
}
