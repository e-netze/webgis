using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Point : Shape
{
    private double m_x, m_y, m_z;

    public Point()
    {
    }
    public Point(double x, double y)
    {
        m_x = x;
        m_y = y;
        m_z = 0.0;
    }
    public Point(double x, double y, double z)
    {
        m_x = x;
        m_y = y;
        m_z = z;
    }
    public Point(Point p)
    {
        if (p != null)
        {
            m_x = p.m_x;
            m_y = p.m_y;
            m_z = p.m_z;
        }
    }

    /// <summary>
    /// The X coordinate.
    /// </summary>
    public double X
    {
        get
        {
            return m_x;
        }
        set
        {
            m_x = value;
        }
    }

    /// <summary>
    /// The Y coordinate.
    /// </summary>
    public double Y
    {
        get
        {
            return m_y;
        }
        set
        {
            m_y = value;
        }
    }

    /// <summary>
    /// The Z coordinate or the height attribute.
    /// </summary>
    public double Z
    {
        get
        {
            return m_z;
        }
        set
        {
            m_z = value;
        }
    }

    public bool? IsSnapped { get; set; }

    public void FromPoint(Point p)
    {
        if (p != null)
        {
            this.X = p.X;
            this.Y = p.Y;
            this.Z = p.Z;
        }
    }
    public double Distance(Point p)
    {
        if (p == null)
        {
            return double.MaxValue;
        }

        return Math.Sqrt((p.X - m_x) * (p.X - m_x) + (p.Y - m_y) * (p.Y - m_y) + (p.Z - m_z) * (p.Z - m_z));
    }

    public double Distance2D(Point p)
    {
        if (p == null)
        {
            return double.MaxValue;
        }

        return Math.Sqrt((p.X - m_x) * (p.X - m_x) + (p.Y - m_y) * (p.Y - m_y));
    }

    public double Direction(Point p)
    {
        double dx = p.X - m_x;
        double dy = p.Y - m_y;

        return Math.Atan2(dy, dx);
    }
    public override Envelope ShapeEnvelope
    {
        get
        {
            return new Envelope(m_x, m_y, m_x, m_y);
        }
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<POINT ");
        sb.Append(base.AXLSrsAttribute());
        sb.Append("x=\"" + (nfi != null ? m_x.ToString(nfi) : m_x.ToString()) + "\" ");
        sb.Append("y=\"" + (nfi != null ? m_y.ToString(nfi) : m_y.ToString()) + "\" ");

        if (this is PointM && ((PointM)this).M != null)
        {
            sb.Append($@"m=""{((PointM)this).M}"" ");
        }
        if (this is PointM2 && ((PointM2)this).M2 != null)
        {
            sb.Append($@"m2=""{((PointM2)this).M2}"" ");
        }

        sb.Append("/>");

        return sb.ToString();
    }

    public override Polygon CalcBuffer(double distance, CancellationTokenSource cts)
    {
        if (this.IsComplex)
        {
            throw new Exception("Can't buffer object with complex geometry!");
        }

        if (distance <= 0)
        {
            return null;
        }

        Polygon buffer = SpatialAlgorithms.PointBuffer(this, distance);
        return buffer;
    }

    new static public Point FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "POINT")
        {
            throw new ArgumentException();
        }

        Point p = new Point();
        if (node.Attributes["coords"] != null)
        {
            string[] xy = node.Attributes["coords"].Value.Trim().Split(' ');
            if (xy.Length < 2)
            {
                throw new ArgumentException();
            }

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
        }
        else if (node.Attributes["x"] != null &&
                 node.Attributes["y"] != null)
        {
            if (nfi == null)
            {
                p.X = Shape.Convert(node.Attributes["x"].Value, null);
                p.Y = Shape.Convert(node.Attributes["y"].Value, null);
            }
            else
            {
                p.X = Shape.Convert(node.Attributes["x"].Value, nfi);
                p.Y = Shape.Convert(node.Attributes["y"].Value, nfi);
            }
        }

        if (node.Attributes["m2"] != null)
        {
            p = new PointM2(p, node.Attributes["m"]?.Value, node.Attributes["m2"]?.Value);
        }
        if (node.Attributes["m"] != null)
        {
            p = new PointM(p, node.Attributes["m"]?.Value);
        }

        return p;
    }

    static public Point Empty
    {
        get
        {
            return new Geometry.Point(double.NaN, double.NaN);
        }
    }

    public bool IsEmpty
    {
        get
        {
            return double.IsNaN(this.X) || double.IsNaN(this.Y);
        }
    }

    public override void Serialize(BinaryWriter w)
    {
        w.Write(m_x);
        w.Write(m_y);
    }
    public override void Deserialize(BinaryReader w)
    {
        m_x = w.ReadDouble();
        m_y = w.ReadDouble();
    }

    public override bool Equals(object obj)
    {
        if (obj is Point)
        {
            Point p = (Point)obj;

            return Equals(p, Shape.Epsilon);
        }
        return false;
    }

    public bool Equals(Point p, double tolerance)
    {
        return Math.Abs(p.X - this.X) < tolerance &&
               Math.Abs(p.Y - this.Y) < tolerance;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
