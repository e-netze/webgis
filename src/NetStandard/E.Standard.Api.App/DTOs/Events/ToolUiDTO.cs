using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class ToolUiDTO
{
    public IUIElement[] elements { get; set; }
    public IUISetter[] setters { get; set; }
    public bool append { get; set; }
}