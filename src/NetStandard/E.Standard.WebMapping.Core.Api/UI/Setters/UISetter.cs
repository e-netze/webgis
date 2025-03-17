namespace E.Standard.WebMapping.Core.Api.UI;

public class UISetter : IUISetter
{
    public UISetter(string elementId, string newValue)
    {
        this.id = elementId;
        this.val = newValue;
    }

    public string name { get; set; }
    public string id { get; set; }
    public string val { get; set; }
}
