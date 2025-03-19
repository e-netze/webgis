using E.Standard.Extensions.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Extensions;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIMenuItem : UIElement, IUIElementText
{
    public UIMenuItem(object callbackObject, ApiToolEventArguments e, UIButton.UIButtonType? type = null, string command = "")
        : base("menuitem")
    {
        Init(callbackObject, e, type, command);
    }

    protected UIMenuItem(string elementType)
        : base(elementType) { }

    protected void Init(object callbackObject, ApiToolEventArguments e, UIButton.UIButtonType? type = null, string command = "")
    {
        if (callbackObject.GetType().IsApiServerTool()
            || type == UIButton.UIButtonType.servertoolcommand_ext)
        {
            this.callback = new
            {
                tool = new
                {
                    id = (e != null && e.AsDefaultTool == true ? CoreApiGlobals.DefaultToolPrefix : "") + callbackObject.GetType().ToToolId(),
                    tooltype = callbackObject is IApiTool ? ((IApiTool)callbackObject).Type.ToString().ToLower() : String.Empty
                },
                type = type.HasValue ? type.Value.ToString().ToLower() : "toolevent",
                command = command
            };
        }
    }

    public string text { get; set; }
    public string text2 { get; set; }
    public string subtext { get; set; }
    public string icon { get; set; }
    public string icon_large { get; set; }

    public string highlight_feature { get; set; }

    public bool large_image { get; set; }

    public object callback
    {
        get;
        set;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? removable { get; set; }
}
