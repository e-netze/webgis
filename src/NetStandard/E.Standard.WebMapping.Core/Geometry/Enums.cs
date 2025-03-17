using System;

namespace E.Standard.WebMapping.Core.Geometry;

public enum AxisDirection
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

[Flags]
public enum ShapeSrsProperties
{
    None = 0,
    SrsId = 1,
    SrsProj4Parameters = 2
}
