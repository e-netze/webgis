using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class PointCollection : Shape
{
    protected List<Point> _points;

    public PointCollection()
    {
        _points = new List<Point>();
    }
    public PointCollection(IEnumerable<Point> points)
        : this()
    {
        foreach (Point point in points)
        {
            _points.Add(new Point(point.X, point.Y));
        }
    }
    public PointCollection(PointCollection pColl)
        : this()
    {
        if (pColl != null)
        {
            for (int i = 0; i < pColl.PointCount; i++)
            {
                Point p = pColl[i];
                if (p != null)
                {
                    _points.Add(p);
                }
            }
        }
    }

    public void Dispose()
    {

    }

    public void AddPoint(Point point)
    {
        if (point != null)
        {
            _points.Add(point);
        }
    }

    public void AddPoints(IEnumerable<Point> points)
    {
        _points.AddRange(points.Where(p => p != null));
    }

    public void InsertPoint(Point point, int pos)
    {
        if (pos > _points.Count)
        {
            pos = _points.Count;
        }

        if (pos < 0)
        {
            pos = 0;
        }

        _points.Insert(pos, point);
    }
    public void ReomvePoint(int pos)
    {
        if (pos < 0 || pos >= _points.Count)
        {
            return;
        }

        _points.RemoveAt(pos);
    }
    public int PointCount
    {
        get
        {
            return _points.Count;
        }
    }
    public Point this[int pointIndex]
    {
        get
        {
            if (pointIndex < 0 || pointIndex >= _points.Count)
            {
                return null;
            }

            return _points[pointIndex];
        }
    }

    public Point[] ToArray(int fromIndex = 0, bool reverse = false)
    {
        if (reverse)
        {
            return ((IEnumerable<Point>)_points).Reverse().Skip(fromIndex).ToArray();
        }
        else
        {
            return _points.Skip(fromIndex).ToArray();
        }
    }

    public Point[] CopyToAray(int fromIndex = 0, bool reverse = false)
    {
        var points = ToArray(fromIndex, reverse);

        return points.Select(p => new Point(p.X, p.Y, p.Z)).ToArray();
    }


    public void Clear()
    {
        if (_points != null)
        {
            _points.Clear();
        }
    }
    public void RemoveRange(int index, int count)
    {
        if (_points != null)
        {
            _points.RemoveRange(index, count);
        }
    }

    public override Envelope ShapeEnvelope
    {
        get
        {
            if (PointCount == 0)
            {
                return null;
            }

            bool first = true;
            double minx = 0, miny = 0, maxx = 0, maxy = 0;

            foreach (Point point in _points)
            {
                if (first)
                {
                    minx = maxx = point.X;
                    miny = maxy = point.Y;
                    first = false;
                }
                else
                {
                    minx = Math.Min(minx, point.X);
                    miny = Math.Min(miny, point.Y);
                    maxx = Math.Max(maxx, point.X);
                    maxy = Math.Max(maxy, point.Y);
                }
            }
            return new Envelope(minx, miny, maxx, maxy);
        }
    }

    public override Polygon CalcBuffer(double distance, CancellationTokenSource cts)
    {
        if (this.IsComplex)
        {
            throw new Exception("Can't buffer object with complex geometry!");
        }

        if (distance <= 0.0)
        {
            return null;
        }

        List<Polygon> buffers = new List<Polygon>();

        foreach (Point p in _points)
        {
            if (p == null)
            {
                continue;
            }

            Polygon buffer = SpatialAlgorithms.PointBuffer(p, distance);
            if (buffer == null)
            {
                continue;
            }

            buffers.Add(buffer);
        }
        return SpatialAlgorithms.FastMergePolygon(buffers, cts);
    }

    public override void Serialize(BinaryWriter w)
    {
        w.Write(_points.Count);
        foreach (Point p in _points)
        {
            p.Serialize(w);
        }
    }
    public override void Deserialize(BinaryReader w)
    {
        _points.Clear();
        int count = w.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Point p = new Point();
            p.Deserialize(w);
            _points.Add(p);
        }
    }

    static public void FromArcXML(XmlNode node, PointCollection pColl, NumberFormatInfo nfi)
    {
        if (node == null || pColl == null)
        {
            throw new ArgumentException();
        }

        XmlNode coordsNode = node.SelectSingleNode("COORDS");
        if (coordsNode != null)
        {
            foreach (string coord in coordsNode.InnerText.Split(';'))
            {
                string[] xy = coord.Trim().Split(' ');
                if (xy.Length < 2)
                {
                    throw new ArgumentException();
                }

                Point p = new Point();
                if (nfi == null)
                {
                    p.X = Shape.Convert(xy[0], null);
                    p.Y = Shape.Convert(xy[1], null);
                }
                else
                {
                    p.X = Shape.Convert(xy[0], nfi);
                    p.Y = Shape.Convert(xy[1], nfi);
                }
                pColl.AddPoint(p);
            }
        }
        else
        {
            foreach (XmlNode pointNode in node.SelectNodes("POINT"))
            {
                pColl.AddPoint(Point.FromArcXML(pointNode, nfi));
            }
        }
    }
}
