namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIMenuItemCheckable : UIMenuItem
{
    public UIMenuItemCheckable(object callbackObject, ApiToolEventArguments e, UIButton.UIButtonType? type = null, string command = "")
        : base("menuitem")
    {
        base.Init(callbackObject, e, type, command);
    }

    public bool show_checkbox => true;
}
