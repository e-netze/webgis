namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIColumn : UIElement
{
    public UIColumn(int width, int margin = 10) : base("div")
    {
        this.style = $"width:{width}px;margin:0px {margin}px;display:inline-block;vertical-align:top";
    }
}
