using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UITitle : UIElement, IUIElementLabel
{
    public UITitle()
        : base("title")
    {
        label = String.Empty;
    }

    public string label { get; set; }
}
