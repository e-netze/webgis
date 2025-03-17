using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Ring : Path
{
    public Ring()
        : base()
    {
    }
    public Ring(List<Point> points)
        : base(points)
    {
    }
    public Ring(PointCollection pColl)
        : base(pColl)
    {
    }
    //internal Ring(Ring ring)
    //    : base()
    //{
    //    for (int i = 0; i < ring.PointCount; i++)
    //    {
    //        this.AddPoint(new Point(ring[i].X, ring[i].Y, ring[i].Z));
    //    }
    //}

    public double Area
    {
        get
        {
            return Math.Abs(RealArea);
        }
    }

    public void Close()
    {
        if (this.PointCount < 3)
        {
            return;
        }

        if (!this[0].Equals(this[this.PointCount - 1]))
        {
            this.AddPoint(new Point(this[0]));
        }
    }
    private double RealArea
    {
        get
        {
            /*
             * var F=0,max=shape_vertexX.length;
		            if(max<3) return 0;
		            for(var i=0;i<max;i++) {
			            var y1=(i==max-1) ? shape_vertexY[0]     : shape_vertexY[i+1];
			            var y2=(i==0)     ? shape_vertexY[max-1] : shape_vertexY[i-1];
			            F+=0.5*shape_vertexX[i]*(y1-y2);	
		        }
             * */
            if (PointCount < 3)
            {
                return 0.0;
            }

            int max = PointCount;

            double A = 0.0;
            for (int i = 0; i < max; i++)
            {
                double y1 = (i == max - 1) ? this[0].Y : this[i + 1].Y;
                double y2 = (i == 0) ? this[max - 1].Y : this[i - 1].Y;

                A += 0.5 * this[i].X * (y1 - y2);
            }
            return A;
        }
    }
    public Point Centroid
    {
        get
        {
            double cx = 0, cy = 0, A = RealArea;
            if (A == 0.0)
            {
                return null;
            }

            int to = PointCount;
            if (this[0].X != this[to - 1].X ||
                this[0].Y != this[to - 1].Y)
            {
                to += 1;
            }
            Point p0 = this[0], p1;
            for (int i = 1; i < to; i++)
            {
                p1 = (i < PointCount) ? this[i] : this[0];
                double h = (p0.X * p1.Y - p1.X * p0.Y);
                cx += (p0.X + p1.X) * h / 6.0;
                cy += (p0.Y + p1.Y) * h / 6.0;
                p0 = p1;
            }
            return new Point(cx / A, cy / A);
        }
    }

    public double Circumference
    {
        get
        {
            if (_points.Count < 2)
            {
                return 0.0;
            }

            double len = 0.0;
            for (int i = 0; i < _points.Count; i++)
            {
                int i2 = (i < _points.Count - 1) ? i + 1 : 0;

                len += Math.Sqrt(
                    (this[i].X - this[i2].X) * (this[i].X - this[i2].X) +
                    (this[i].Y - this[i2].Y) * (this[i].Y - this[i2].Y));
            }
            return len;
        }
    }

    public Polygon ToPolygon()
    {
        return new Polygon(new Ring(this));  // Create new Ring -> Holes get outer rings!!
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        return ArcXML(nfi, null);
    }

    internal string ArcXML(NumberFormatInfo nfi, List<Hole> holes)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<RING>");
        foreach (Point point in _points)
        {
            if (point == null)
            {
                continue;
            }

            sb.Append(point.ArcXML(nfi));
        }
        if (holes != null)
        {
            foreach (Hole hole in holes)
            {
                sb.Append(hole.ArcXML(nfi));
            }
        }
        sb.Append("</RING>");

        return sb.ToString();
    }
    new static public Ring FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "RING")
        {
            throw new ArgumentException();
        }

        Ring pColl = new Ring();
        PointCollection.FromArcXML(node, pColl, nfi);

        return pColl;
    }
}
