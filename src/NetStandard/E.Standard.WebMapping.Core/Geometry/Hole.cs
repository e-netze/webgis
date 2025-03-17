using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Hole : Ring
{
    private Ring _outerRing = null;

    public Hole()
        : base()
    {
    }
    public Hole(List<Point> points)
        : base(points)
    {
    }

    internal Hole(Ring ring)
        : base(ring)
    {
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<HOLE>");
        foreach (Point point in _points)
        {
            if (point == null)
            {
                continue;
            }

            sb.Append(point.ArcXML(nfi));
        }
        sb.Append("</HOLE>");

        return sb.ToString();
    }

    public Ring OuterRing
    {
        get { return _outerRing; }
        set { _outerRing = value; }
    }

    new static public Hole FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "HOLE")
        {
            throw new ArgumentException();
        }

        Hole pColl = new Hole();
        PointCollection.FromArcXML(node, pColl, nfi);

        return pColl;
    }
}
