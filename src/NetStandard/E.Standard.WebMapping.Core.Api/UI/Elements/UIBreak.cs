namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIBreak : UIElement
{
    public UIBreak(int countRepeat = 1)
        : base("br")
    {
        this.target = null;
        this.repeat = countRepeat;
    }

    public int repeat { get; set; }
}
