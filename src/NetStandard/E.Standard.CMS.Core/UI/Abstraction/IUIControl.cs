using System.Collections.Generic;

namespace E.Standard.CMS.Core.UI.Abstraction;

public interface IUIControl
{
    IEnumerable<IUIControl> ChildControls { get; }

    IUIControl GetControl(string name);

    IEnumerable<IUIControl> AllControls();

    void SetDirty(bool isDirty);

    IEnumerable<IUIControl> GetDitryControls();

    string Name { get; }

    bool? IsClickable { get; set; }
}
