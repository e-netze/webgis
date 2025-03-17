namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIMenu : UIElement
{
    public UIMenu()
        : base("menu")
    {

    }

    public string header { get; set; }

    public bool collapsable { get; set; }
}
