using E.Standard.Platform;
using E.Standard.WebMapping.Core.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Shape
{
    public const double Epsilon = 1e-8;

    private enum GeometryTypeId
    {
        Point = 1,
        Multipoint = 2,
        Polyline = 3,
        Polygon = 4,
        Envelope = 5,
        Circle = 6
    };

    private BufferFilter _buffer = null;
    private bool _isComplex = false;
    private int _srsId = 0;
    private string _srsp4params = null;

    //public static NumberFormatInfo _nfi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    public BufferFilter Buffer
    {
        get { return _buffer; }
        set { _buffer = value; }
    }
    public virtual Envelope ShapeEnvelope
    {
        get { return new Envelope(0, 0, 0, 0); }
    }
    public virtual string ArcXML(NumberFormatInfo nfi)
    {
        return String.Empty;
    }
    public virtual Polygon CalcBuffer(double distance, CancellationTokenSource cts)
    {
        if (this.IsComplex)
        {
            throw new Exception("Can't buffer object with complex geometry!");
        }

        return null;
    }

    public virtual IEnumerable<Point> DeterminePointsOnShape(SpatialReferenceCollection srefCollection = null, int targetSrsId = 0)
    {
        bool isGeographic = false;
        SpatialReference sRef = null;

        var srsId = this.SrsId;
        if (srsId <= 0 && this.ShapeEnvelope != null && this.ShapeEnvelope.HasWgs84Bounds)
        {
            srsId = 4326;
        }

        if (srefCollection != null && srsId > 0)
        {
            sRef = srefCollection.ById(srsId);
            if (sRef != null)
            {
                isGeographic = !sRef.IsProjective;
            }
        }

        Point[] result = null;

        try
        {
            if (this is Point)
            {
                result = new Point[] { (Point)this };
            }
            else
            {
                result = SpatialAlgorithms.DeterminePointsOnShape(null, this, 10, !isGeographic);
            }
        }
        catch
        {
            return new Point[0];
        }

        if (sRef != null && targetSrsId > 0 && sRef.Id != targetSrsId)
        {
            var targetSRef = srefCollection.ById(targetSrsId);
            if (targetSRef != null)
            {
                List<Point> transformedPoints = new List<Point>();

                using (var transformer = new GeometricTransformerPro(sRef, targetSRef))
                {
                    foreach (var point in result)
                    {
                        //
                        // Nicht den original Punkt ändern!!
                        // Sonst gibt es Probleme bei den Abfragen => Ergebnis einer Query ist dann nicht mehr 4326, wenn die Funktion für Abfrageergebnisse aufgerufen wird 
                        // => zB Wenn ein Link mit [spatial::point::31256] steht
                        //
                        var point2transform = new Point(point);
                        transformer.Transform(point2transform);
                        transformedPoints.Add(point2transform);
                    }
                }

                result = transformedPoints.ToArray();
            }
        }

        return result;
    }

    public virtual void Serialize(BinaryWriter w)
    {
    }
    public virtual void Deserialize(BinaryReader w)
    {
    }

    public virtual bool IsMultipart
    {
        get { return false; }
    }

    public virtual IEnumerable<Shape> Multiparts
    {
        get { throw new NotImplementedException(); }
    }

    public virtual void AppendMuiltiparts(Shape shape)
    {
        throw new NotImplementedException();
    }

    public bool? HasM { get; set; }
    public bool? HasZ { get; set; }

    public static void SerializeShape(Shape shape, BinaryWriter w)
    {
        if (shape is Point)
        {
            w.Write((byte)GeometryTypeId.Point);
        }
        else if (shape is MultiPoint)
        {
            w.Write((byte)GeometryTypeId.Multipoint);
        }
        else if (shape is Polyline)
        {
            w.Write((byte)GeometryTypeId.Polyline);
        }
        else if (shape is Polygon)
        {
            w.Write((byte)GeometryTypeId.Polygon);
        }
        else if (shape is Envelope)
        {
            w.Write((byte)GeometryTypeId.Envelope);
        }
        else if (shape is CircleShape)
        {
            w.Write((byte)GeometryTypeId.Circle);
        }
        else
        {
            throw new Exception("Unknown Geometry Type for Serialization");
        }

        shape.Serialize(w);
    }

    public static Shape DeserializeShape(BinaryReader w)
    {
        GeometryTypeId type = (GeometryTypeId)(int)w.ReadByte();
        switch (type)
        {
            case GeometryTypeId.Point:
                Point point = new Point();
                point.Deserialize(w);
                return point;
            case GeometryTypeId.Multipoint:
                MultiPoint mPoint = new MultiPoint();
                mPoint.Deserialize(w);
                return mPoint;
            case GeometryTypeId.Polyline:
                Polyline pLine = new Polyline();
                pLine.Deserialize(w);
                return pLine;
            case GeometryTypeId.Polygon:
                Polygon poly = new Polygon();
                poly.Deserialize(w);
                return poly;
            case GeometryTypeId.Envelope:
                Envelope env = new Envelope();
                env.Deserialize(w);
                return env;
            case GeometryTypeId.Circle:
                CircleShape circle = new CircleShape(new Point(), 0);
                circle.Deserialize(w);
                return circle;
        }

        return null;
    }

    static public Shape FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null)
        {
            throw new ArgumentException();
        }

        switch (node.Name)
        {
            case "ENVELOPE":
                return Envelope.FromArcXML(node, nfi);
            case "POINT":
                return Point.FromArcXML(node, nfi);
            case "MULTIPOINT":
                return MultiPoint.FromArcXML(node, nfi);
            case "POLYLINE":
                return Polyline.FromArcXML(node, nfi);
            case "POLYGON":
                return Polygon.FromArcXML(node, nfi);
            case "PATH":
                return Path.FromArcXML(node, nfi);
            case "RING":
                return Ring.FromArcXML(node, nfi);
            case "HOLE":
                return Hole.FromArcXML(node, nfi);
        }

        return null;
    }

    public bool IsComplex
    {
        get { return _isComplex; }
        set { _isComplex = value; }
    }

    public int SrsId
    {
        get { return _srsId; }
        set { _srsId = value; }
    }

    public string SrsP4Parameters
    {
        get { return _srsp4params; }
        set { _srsp4params = value; }
    }

    static public double Convert(string val, NumberFormatInfo nfi)
    {
        if (nfi == null || val.Contains("."))
        {
            return val.ToPlatformDouble();
        }
        else
        {
            return double.Parse(val, nfi);
        }
    }

    protected string AXLSrsAttribute()
    {
        if (this.SrsId > 0)
        {
            return " srs=\"" + this.SrsId + "\" ";
        }

        return String.Empty;
    }
}
