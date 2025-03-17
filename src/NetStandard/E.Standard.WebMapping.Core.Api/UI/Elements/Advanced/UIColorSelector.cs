using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIColorSelector : UIOptionContainer
{
    public UIColorSelector(string title, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown, bool allowNoColor = false)
        : this(buttonType, buttonCommand.ToString(), allowNoColor)
    {
        this.title = title;
        this.CollapseState = CollapseStatus.Collapsed;

    }

    bool AllowNoColor { get; set; }

    public UIColorSelector(UIButton.UIButtonType buttonType, string buttonCommand, bool allowNoColor = false)
    {
        this.AllowNoColor = allowNoColor;

        List<UIElement> uiElements = new List<UIElement>();

        List<string> colors = new List<string>();

        #region Generate Colors

        string[] colorMap = new string[] { "00", "33", "66", "99", "aa", "cc", "ee", "ff" };
        for (int i = 0; i < colorMap.Length; i++)
        {
            colors.Add(colorMap[i] + colorMap[i] + colorMap[i]);
        }
        // Blue
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[0] + colorMap[0] + colorMap[i]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[i] + colorMap[i] + colorMap[7]);
            }
        }
        // Green
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[0] + colorMap[i] + colorMap[0]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[i] + colorMap[7] + colorMap[i]);
            }
        }
        // Red
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[i] + colorMap[0] + colorMap[0]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[7] + colorMap[i] + colorMap[i]);
            }
        }
        // Yellow
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[i] + colorMap[i] + colorMap[0]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[7] + colorMap[7] + colorMap[i]);
            }
        }
        // Cyan
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[0] + colorMap[i] + colorMap[i]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[i] + colorMap[7] + colorMap[7]);
            }
        }
        // Magenta
        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 4 && i != 6)
            {
                colors.Add(colorMap[i] + colorMap[0] + colorMap[i]);
            }
        }

        for (int i = 2; i < colorMap.Length; i++)
        {
            if (i != 2 && i != 4 && i != 6 && i != 7)
            {
                colors.Add(colorMap[7] + colorMap[i] + colorMap[7]);
            }
        }

        #endregion

        foreach (string color in colors)
        {
            uiElements.Add(new UIImageButton(String.Empty, buttonType, buttonCommand)
            {
                style = "background-color:#" + color + ";",
                value = "#" + color
            });
        }

        if (this.AllowNoColor)
        {
            uiElements.Add(new UIImageButton("content/api/img/graphics/ui/none.png", buttonType, buttonCommand)
            {
                style = "background-color:#fff;background-position:center;border:1px solid #000",
                value = "none"
            });
        }

        this.elements = uiElements.ToArray();
    }
}
