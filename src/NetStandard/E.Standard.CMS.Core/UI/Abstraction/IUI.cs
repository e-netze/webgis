namespace E.Standard.CMS.Core.UI.Abstraction;

public interface IUI
{
    IUIControl GetUIControl(bool create);
}
