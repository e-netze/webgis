using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry.Topology;

public class UniquePointList
{
    private readonly List<Point> _points;
    private readonly double _tolerance;

    public UniquePointList(double tolerance = 1e-7)
    {
        _points = new List<Point>();
        _tolerance = tolerance;
    }

    public int TryAddPoint(Point point)
    {
        if (point == null)
        {
            return -1;
        }

        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i].Distance2D(point) < _tolerance)
            {
                return i;
            }
        }

        _points.Add(point);

        return _points.Count - 1;
    }
}
