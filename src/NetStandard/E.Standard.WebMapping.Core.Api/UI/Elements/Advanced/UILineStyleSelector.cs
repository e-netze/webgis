using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UILineStyleSelector : UIOptionContainer
{
    public UILineStyleSelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : this(buttonType, buttonCommand.ToString())
    {
        this.title = title;
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;
    }

    public UILineStyleSelector(UIButton.UIButtonType buttonType, string buttonCommand)
    {
        List<UIElement> uiElements = new List<UIElement>();

        string[] dashStyle = new string[] { "solid", "dash", "dashdot", "dashdotdot", "dot" };
        // https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/stroke-dasharray
        string[] dashArray = new string[] { "1", "10,20", "15,15,3,15", "15,15,3,15,3,15", "3,15" };

        for (int i = 0; i < dashStyle.Length; i++)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/line-style-" + dashStyle[i] + ".png", buttonType, buttonCommand)
            {
                value = dashArray[i]
            });
        }

        this.elements = uiElements.ToArray();
    }
}
