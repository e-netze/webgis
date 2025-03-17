using System;

namespace E.Standard.WebMapping.Core.Geometry.Snapping;

[Flags]
public enum SnappingTypes
{
    None = 0,
    Nodes = 1,
    Edges = 2,
    Endpoints = 4
}
