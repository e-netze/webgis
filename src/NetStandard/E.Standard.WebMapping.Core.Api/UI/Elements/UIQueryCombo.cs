namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIQueryCombo : UIElement
{
    public enum ComboType
    {
        query = 0,
        indentify = 1
    };

    public UIQueryCombo(ComboType type = ComboType.query)
        : base("querycombo")
    {
        this.combotype = type.ToString().ToLower();
    }

    public string combotype { get; set; }

    public UINameValue[] customitems { get; set; }
}
