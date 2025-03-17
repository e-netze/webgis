using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UILineWieghtSelector : UIOptionContainer
{
    public UILineWieghtSelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : this(buttonType, buttonCommand.ToString())
    {
        this.title = title;
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;
    }

    public UILineWieghtSelector(UIButton.UIButtonType buttonType, string buttonCommand)
    {
        List<UIElement> uiElements = new List<UIElement>();

        for (int i = 1; i < 20; i++)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/line-weight-" + i + ".png", buttonType, buttonCommand)
            {
                value = i
            });
        }

        this.elements = uiElements.ToArray();
    }
}
