using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UICallbackButton : UIButton
{
    public UICallbackButton(IApiServerButton callbackServerButton, string buttonCommand)
        : base(UIButtonType.servertoolcommand_ext, buttonCommand)
    {
        this.id = callbackServerButton?.GetType().ToToolId();
    }

    public UICallbackButton(IApiServerButtonAsync callbackServerButton, string buttonCommand)
        : base(UIButtonType.servertoolcommand_ext, buttonCommand)
    {
        this.id = callbackServerButton?.GetType().ToToolId();
    }

    public UICallbackButton(IApiButton callbackServerButton, string buttonCommand)
        : base(UIButtonType.servertoolcommand_ext, buttonCommand)
    {
        this.id = callbackServerButton?.GetType().ToToolId();
    }
}

public class UICallbackButton<T> : UIButton
{
    public UICallbackButton(IApiServerButtonLocalizable<T> callbackServerButton, string buttonCommand)
        : base(UIButtonType.servertoolcommand_ext, buttonCommand)
    {
        this.id = callbackServerButton?.GetType().ToToolId();
    }



    public UICallbackButton(IApiServerButtonLocalizableAsync<T> callbackServerButton, string buttonCommand)
        : base(UIButtonType.servertoolcommand_ext, buttonCommand)
    {
        this.id = callbackServerButton?.GetType().ToToolId();
    }
}
