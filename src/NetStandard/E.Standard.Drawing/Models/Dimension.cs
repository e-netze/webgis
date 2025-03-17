namespace E.Standard.Drawing.Models;

public struct Dimension
{
    public Dimension(int width = 0, int height = 0)
        => (Width, Height) = (width, height);

    public int Width { get; set; }
    public int Height { get; set; }
}
