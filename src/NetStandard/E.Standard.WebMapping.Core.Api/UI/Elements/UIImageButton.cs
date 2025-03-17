using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Extensions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIImageButton : UIElement, IUIElementText
{
    public UIImageButton(Type owner, string imageName, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : base("imagebutton")
    {
        this.src = ToolResourceImage(owner, imageName);
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand.ToString();
    }
    public UIImageButton(Type owner, string imageName, UIButton.UIButtonType buttonType, string buttonCommand)
        : base("imagebutton")
    {
        this.src = ToolResourceImage(owner, imageName);
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand;
    }
    public UIImageButton(string imagePath, UIButton.UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : base("imagebutton")
    {
        this.src = imagePath;
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand.ToString();
    }
    public UIImageButton(string imagePath, UIButton.UIButtonType buttonType, string buttonCommand)
        : base("imagebutton")
    {
        this.src = imagePath;
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand;
    }

    public string src { get; set; }
    public string text { get; set; }

    public string buttontype { get; set; }
    public string buttoncommand { get; set; }

    public static string ToolResourceImage(Type owner, string imageName)
    {
        return "rest/toolresource/" + (owner.ToToolId() + "." + imageName).Replace(".", "-").ToLower();
    }
    public static string ToolResourceImage(IApiButton owner, string imageName)
    {
        return "rest/toolresource/" + (owner.GetType().ToToolId() + "." + imageName).Replace(".", "-").ToLower();
    }
}
