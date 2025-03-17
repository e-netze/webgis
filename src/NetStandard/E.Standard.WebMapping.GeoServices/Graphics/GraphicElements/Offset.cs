namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class Offset
{
    public Offset(float left, float top)
    {
        this.Left = left;
        this.Top = top;
    }

    public float Top { get; set; }
    public float Left { get; set; }
}
