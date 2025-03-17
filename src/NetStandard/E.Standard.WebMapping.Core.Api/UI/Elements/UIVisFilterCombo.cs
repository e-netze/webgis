namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIVisFilterCombo : UIElement
{
    public UIVisFilterCombo()
        : base("visfiltercombo")
    {
    }

    public UINameValue[] customitems { get; set; }

    public string onchange { get; set; }
    new public string value { get; set; }
}
