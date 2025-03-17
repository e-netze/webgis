using System;

namespace E.Standard.CMS.Core.UI.Abstraction;

public interface IEditableUIControl
{
    void FireChange();
    event EventHandler OnChange;
}
