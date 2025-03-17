namespace E.Standard.CMS.UI.Controls;

public class InputPassword : Input
{
    public InputPassword(string name)
        : base(name)
    {
        base.IsPassword = true;
    }
}
