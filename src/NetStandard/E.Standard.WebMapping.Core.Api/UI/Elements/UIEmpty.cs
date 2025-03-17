namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIEmpty : UIElement
{
    public UIEmpty()
        : base("empty")
    {
        this.closetarget = true;
    }

    public bool closetarget { get; set; }
}
