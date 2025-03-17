namespace E.Standard.Drawing.Models;

public struct DimensionF
{
    public DimensionF(float width = 0, float height = 0)
        => (Width, Height) = (width, height);

    public float Width { get; set; }
    public float Height { get; set; }
}
