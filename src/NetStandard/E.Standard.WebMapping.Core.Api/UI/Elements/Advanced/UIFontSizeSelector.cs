using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIFontSizeSelector : UIOptionContainer
{
    public UIFontSizeSelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : this(buttonType, buttonCommand.ToString())
    {
        this.title = title;
        this.CollapseState = CollapseStatus.Collapsed;
    }

    public UIFontSizeSelector(UIButton.UIButtonType buttonType, string buttonCommand)
    {
        List<UIElement> uiElements = new List<UIElement>();

        int[] fontsize = new int[] { 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 38, 48, 58, 68, 80, 100, 120 };


        for (int i = 0; i < fontsize.Length; i++)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/font-size-" + fontsize[i] + ".png", buttonType, buttonCommand)
            {
                value = fontsize[i].ToString()
            });
        }

        this.elements = uiElements.ToArray();
    }
}
