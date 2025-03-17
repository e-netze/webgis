namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILabelingCombo : UIElement
{
    public UILabelingCombo()
        : base("labelingcombo")
    {
    }

    public UINameValue[] customitems { get; set; }

    public string onchange { get; set; }
    public new string value { get; set; }
}
