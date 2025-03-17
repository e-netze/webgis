namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIMarkerCircleRadiusCombo : UIElement
{
    public UIMarkerCircleRadiusCombo()
        : base("circleradiuscombo")
    {
    }

    public int[] radii { get; set; }
    new public int value { get; set; }
}
