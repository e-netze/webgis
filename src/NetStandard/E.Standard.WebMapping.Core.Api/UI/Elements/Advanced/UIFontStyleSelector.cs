using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIFontStyleSelector : UIOptionContainer
{
    public UIFontStyleSelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : this(buttonType, buttonCommand.ToString())
    {
        this.title = title;
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;
    }

    public UIFontStyleSelector(UIButton.UIButtonType buttonType, string buttonCommand)
    {
        List<UIElement> uiElements = new List<UIElement>();

        string[] fontstyle = new string[] { "regular", "italic", "bold", "bolditalic", "underline" };


        for (int i = 0; i < fontstyle.Length; i++)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/font-style-" + fontstyle[i] + ".png", buttonType, buttonCommand)
            {
                value = fontstyle[i]
            });
        }

        this.elements = uiElements.ToArray();
    }
}
