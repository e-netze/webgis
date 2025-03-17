namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIOpacityControl : UIElement
{
    public UIOpacityControl(string serviceId)
        : base("opacity-control")
    {
        this.serviceId = serviceId;
    }

    public string serviceId { get; set; }
}
