namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIShareLinkButtons : UIElement
{
    public UIShareLinkButtons(string link2share)
        : base("sharelink-buttons")
    {
        this.link = link2share;
    }

    public string link { get; set; }
    public string subject { get; set; }
    public string qr_base64 { get; set; }
}
