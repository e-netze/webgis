using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILabel : UIElement, IUIElementLabel
{
    public UILabel()
        : base("label")
    {
        label = String.Empty;
    }

    public string label { get; set; }
}
