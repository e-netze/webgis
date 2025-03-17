namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class Lense
{
    public double Width { get; set; }
    public double Height { get; set; }
    public bool Zoom { get; set; }
    public double? LenseScale { get; set; }
    public string ScaleControl { get; set; }

    public static Lense Current
    {
        get
        {
            return new Lense()
            {
                Width = -1,
                Height = -1
            };
        }
    }
}
