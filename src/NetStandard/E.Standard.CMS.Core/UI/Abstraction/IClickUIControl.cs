using System;

namespace E.Standard.CMS.Core.UI.Abstraction;

public interface IClickUIControl
{
    void FireClick();
    event EventHandler OnClick;
}
