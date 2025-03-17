using E.Standard.WebMapping.Core.Extensions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIToolUndoButton : UIElement
{
    public UIToolUndoButton(Type owner, string text = "Undo")
        : base("undobutton")
    {
        this.undotool = owner.ToToolId();
        this.text = text;
    }

    public string undotool { get; set; }
    public string text { get; set; }
}
