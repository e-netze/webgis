using E.Standard.WebMapping.Core.Extensions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIImageToolUndoButton : UIElement
{
    public UIImageToolUndoButton(Type owner, string imageName)
        : base("undobutton")
    {
        this.src = UIImageButton.ToolResourceImage(owner, imageName);
        this.undotool = owner.ToToolId();
        this.text = text;
    }

    public string undotool { get; set; }
    public string text { get; set; }
    public string src { get; set; }
}
