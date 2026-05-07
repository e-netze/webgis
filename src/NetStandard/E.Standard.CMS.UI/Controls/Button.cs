using System;

using E.Standard.CMS.Core.UI.Abstraction;

namespace E.Standard.CMS.UI.Controls;

public class Button : ControlLabel, IClickUIControl
{
    public Button(string name = "")
        : base(name)
    {

    }

    public event EventHandler OnClick;

    public void FireClick()
    {
        OnClick?.Invoke(this, new EventArgs());
    }
}
