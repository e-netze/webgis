using System;

namespace E.Standard.WebGIS.Core;

public enum HttpSchema
{
    Default = 0,
    Current = 1,
    Https = 2,
    Empty = 3
}

public enum QueryGeometryType
{
    Full = 0,
    Simple = 1,
    None = 2
}

[Flags]
public enum FocusableUIElements
{
    None = 0,
    MapOverlayUIEments = 1,
    TabPresentions = 2,
    TabQueryResults = 4,
    TabTools = 8,
    TabSettings = 16
}
