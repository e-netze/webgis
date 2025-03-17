using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Extensions;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIButton : UIElement, IButtonUIElement
{
    public UIButton(UIButtonType buttonType, ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.unknown)
        : base("button")
    {
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand.ToString();
    }
    public UIButton(UIButtonType buttonType, string buttonCommand)
        : base("button")
    {
        this.buttontype = buttonType.ToString();
        this.buttoncommand = buttonCommand;
    }

    public string text { get; set; }

    public string icon { get; set; }

    public string ctrl_shortcut { get; set; }

    public string buttontype { get; set; }
    public string buttoncommand { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string buttoncommand_argument { get; set; }

    public enum UIButtonType
    {
        clientbutton = 0,
        serverbutton = 1,
        servertoolcommand = 2,
        servertoolcommand_ext = 3
    }

    public static string ToolResourceImage(Type owner, string imageName)
    {
        return "rest/toolresource/" + (owner.ToToolId() + "." + imageName).Replace(".", "-").ToLower();
    }
}
