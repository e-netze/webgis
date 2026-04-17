using System;

using E.Standard.WebMapping.Core.Api.UI.Abstractions;

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
