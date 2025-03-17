namespace E.Standard.Drawing.Models;

public struct Position
{
    public Position(int x = 0, int y = 0)
        => (X, Y) = (x, y);

    public int X { get; set; }
    public int Y { get; set; }
}
