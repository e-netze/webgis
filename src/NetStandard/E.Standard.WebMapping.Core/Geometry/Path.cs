using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Path : PointCollection
{
    public Path()
        : base()
    {
    }
    public Path(List<Point> points)
        : base(points)
    {
    }
    public Path(PointCollection pColl)
        : base(pColl)
    {
    }

    public double Length
    {
        get
        {
            if (_points.Count < 2)
            {
                return 0.0;
            }

            double len = 0.0;
            for (int i = 1; i < _points.Count; i++)
            {
                len += Math.Sqrt(
                    (this[i - 1].X - this[i].X) * (this[i - 1].X - this[i].X) +
                    (this[i - 1].Y - this[i].Y) * (this[i - 1].Y - this[i].Y));
            }
            return len;
        }
    }

    public double Length3D
    {
        get
        {
            if (_points.Count < 2)
            {
                return 0.0;
            }

            double len = 0.0;
            for (int i = 1; i < _points.Count; i++)
            {
                len += Math.Sqrt(
                    (this[i - 1].X - this[i].X) * (this[i - 1].X - this[i].X) +
                    (this[i - 1].Y - this[i].Y) * (this[i - 1].Y - this[i].Y) +
                    (this[i - 1].Z - this[i].Z) * (this[i - 1].Z - this[i].Z));
            }
            return len;
        }
    }

    public Polyline ToPolyline()
    {
        var path = new Path(this);
        if (this is Ring)
        {
            path.ClosePath();
        }

        return new Polyline(path);
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<PATH>");
        foreach (Point point in _points)
        {
            if (point == null)
            {
                continue;
            }

            sb.Append(point.ArcXML(nfi));
        }
        sb.Append("</PATH>");

        return sb.ToString();
    }

    new static public Path FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "PATH")
        {
            throw new ArgumentException();
        }

        Path pColl = new Path();
        PointCollection.FromArcXML(node, pColl, nfi);

        return pColl;
    }

    public void ClosePath(double tolerance = Shape.Epsilon)
    {
        if (this.PointCount < 3)
        {
            return;
        }

        Point ps = this[0];
        Point pe = this[this.PointCount - 1];

        if (ps == null || pe == null ||
            ps.Distance2D(pe) < tolerance)
        {
            pe.X = ps.X;
            pe.Y = ps.Y;

            return;
        }

        this.AddPoint(new Point(ps.X, ps.Y));
    }

    public Point MidPoint2D
    {
        get
        {
            if (this.PointCount == 0)
            {
                return null;
            }

            double length = this.Length;
            if (length == 0)
            {
                return this[0];
            }

            return SpatialAlgorithms.PolylinePoint(new Polyline(this), length / 2D);
        }
    }
}
