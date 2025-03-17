namespace E.Standard.Esri.Shapefile;

enum ShapeType
{
    /// <summary>Shape with no geometric data</summary>
    NullShape = 0,
    /// <summary>2D point</summary>
    Point = 1,
    /// <summary>2D polyline</summary>
    PolyLine = 3,
    /// <summary>2D polygon</summary>
    Polygon = 5,
    /// <summary>Set of 2D points</summary>
    MultiPoint = 8,
    /// <summary>3D point</summary>
    PointZ = 11,
    /// <summary>3D polyline</summary>
    PolyLineZ = 13,
    /// <summary>3D polygon</summary>
    PolygonZ = 15,
    /// <summary>Set of 3D points</summary>
    MultiPointZ = 18,
    /// <summary>3D point with measure</summary>
    PointM = 21,
    /// <summary>3D polyline with measure</summary>
    PolyLineM = 23,
    /// <summary>3D polygon with measure</summary>
    PolygonM = 25,
    /// <summary>Set of 3d points with measures</summary>
    MultiPointM = 28,
    /// <summary>Collection of surface patches</summary>
    MultiPatch = 31
}
