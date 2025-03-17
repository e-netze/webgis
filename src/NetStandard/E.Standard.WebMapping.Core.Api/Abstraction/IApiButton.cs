namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiButton
{
    string Name { get; }
    string Container { get; }
    string Image { get; }
    string ToolTip { get; }
    bool HasUI { get; }
}
