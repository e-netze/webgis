using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILiteral : UIElement, IUIElementLiteral
{
    public UILiteral()
        : base("literal")
    {
        literal = String.Empty;
    }

    public string literal { get; set; }
    public bool as_markdown { get; set; }
}
