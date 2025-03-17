namespace E.Standard.CMS.UI.Controls;

public class UserControl : Control
{
    public UserControl(string name = "") : base(name) { }
}

public abstract class NameUrlUserConrol : UserControl
{
    abstract public NameUrlControl NameUrlControlInstance { get; }
}
