namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UICard : UIElement
{
    public UICard(string title = "")
        : base("card") 
        => (this.title) = (title);

    public string title { get; set; }
}
