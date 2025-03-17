using E.Standard.Platform;
using System;
using System.Collections.Generic;
namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIOpacitySelector : UIOptionContainer
{
    public UIOpacitySelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : this(buttonType, buttonCommand.ToString())
    {
        this.title = title;
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;
    }
    public UIOpacitySelector(UIButton.UIButtonType buttonType, string buttonCommand)
    {
        List<UIElement> uiElements = new List<UIElement>();
        int[] opacity = new int[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        for (int i = 0; i < opacity.Length; i++)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/fill-opacity-" + opacity[i] + ".png", buttonType, buttonCommand)
            {
                value = Math.Round(opacity[i] / 100f, 1).ToPlatformNumberString()
            });
        }
        this.elements = uiElements.ToArray();
    }
}
