using System;

using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILiteralBold : UIElement, IUIElementLiteral
{
    public UILiteralBold()
        : base("literal-bold")
    {
        literal = String.Empty;
    }

    public string literal { get; set; }
    public bool as_markdown { get; set; }
}
