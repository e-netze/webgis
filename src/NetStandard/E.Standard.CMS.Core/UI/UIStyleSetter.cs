namespace E.Standard.CMS.Core.UI;

public class UIStyleSetter
{
    public UIStyleSetter()
    {
        Append = true;
    }

    public string Selector { get; set; }
    public string ClassName { get; set; }
    public bool Append { get; set; }
}
