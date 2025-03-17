using System;
using System.Globalization;
using System.IO;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class CircleShape : Shape
{
    private Point _center;
    private double _radius;

    public CircleShape(Point center, double radius)
    {
        _center = center;
        _radius = radius;
    }

    public CircleShape(Point center, Point circlePoint)
    {
        _center = center;

        _radius = Math.Sqrt(Math.Pow(circlePoint.X - center.X, 2.0) + Math.Pow(circlePoint.Y - center.Y, 2.0));
    }


    public override Envelope ShapeEnvelope
    {
        get
        {
            return new Envelope(_center.X - _radius, _center.Y - _radius, _center.X + _radius, _center.Y + _radius);
        }
    }

    public Point Center
    {
        get { return new Point(_center.X, _center.Y); }
    }

    public double Radius
    {
        get { return _radius; }
    }

    public override void Serialize(BinaryWriter w)
    {
        _center.Serialize(w);
        w.Write(_radius);
    }

    public override void Deserialize(BinaryReader w)
    {
        _center = new Point();
        _center.Deserialize(w);
        _radius = w.ReadDouble();
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        MultiPoint mp = new MultiPoint();
        mp.AddPoint(_center);
        mp.AddPoint(new Point(_center.X + _radius, _center.Y));

        return mp.ArcXML(nfi).Replace("MULTIPOINT", "CIRCLE");
    }
}
