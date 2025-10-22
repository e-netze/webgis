using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class MultiPoint : PointCollection
{
    public MultiPoint()
        : base()
    {
    }
    public MultiPoint(PointCollection pColl)
        : base(pColl)
    {
    }
    public MultiPoint(IEnumerable<Point> points)
    {
        this.AddPoints(points);
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<MULTIPOINT" + base.AXLSrsAttribute() + ">");
        foreach (Point point in _points)
        {
            if (point == null)
            {
                continue;
            }

            sb.Append(point.ArcXML(nfi));
        }
        sb.Append("</MULTIPOINT>");

        return sb.ToString();
    }

    new static public MultiPoint FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "MULTIPOINT")
        {
            throw new ArgumentException();
        }

        MultiPoint pColl = new MultiPoint();
        PointCollection.FromArcXML(node, pColl, nfi);

        return pColl;
    }

    public override IEnumerable<Shape> Multiparts
    {
        get
        {
            return _points.ToArray();
        }
    }

    public override void AppendMuiltiparts(Shape shape)
    {
        if (shape is Point)
        {
            _points.Add((Point)shape);
        }
        else if (shape is PointCollection)
        {
            _points.AddRange(((PointCollection)shape).ToArray());
        }
        else
        {
            base.AppendMuiltiparts(shape);
        }
    }
}
