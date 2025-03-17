namespace E.Standard.Drawing.Models;

public struct PositionF
{
    public PositionF(float x = 0, float y = 0)
        => (X, Y) = (x, y);

    public float X { get; set; }
    public float Y { get; set; }
}
