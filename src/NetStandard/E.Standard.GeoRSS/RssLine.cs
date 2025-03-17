using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.GeoRSS20;

public class RssLine : RssGeometry
{
    List<RssPoint> _points = new List<RssPoint>();

    public RssLine()
    {
        _points = new List<RssPoint>();
    }

    public RssLine(string str)
        : base()
    {
        var coords = str.Split(' ').ToArray();

        for (int i = 0; i < coords.Length - 1; i += 2)
        {
            _points.Add(new RssPoint(double.Parse(coords[i], Formatter.nhi), double.Parse(coords[i + 1], Formatter.nhi)));
        }
    }

    public int PointCount => _points.Count;

    public RssPoint this[int index]
    {
        get => _points[index];
    }

    public override string ToString()
    {
        return String.Join(" ", _points.Select(p => p.ToString()));
    }
}
