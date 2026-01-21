using E.Standard.WebMapping.Core.Geometry.Clipper;
using E.Standard.WebMapping.Core.Geometry.Topology;
using Proj4Net.Core.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class Polygon : Shape
{
    private List<Ring> _rings;
    private int _ringsChecked = 0;

    public Polygon()
    {
        _rings = new List<Ring>();
    }
    public Polygon(Ring ring)
        : this()
    {
        _rings.Add(ring);
    }

    public Polygon(IEnumerable<Ring> rings)
        : this()
    {
        AddRings(rings);
    }

    public void AddRing(Ring ring)
    {
        _rings.Add(ring);
    }

    public void AddRings(IEnumerable<Ring> rings)
    {
        _rings.AddRange(rings);
    }

    public void InsertRing(Ring ring, int pos)
    {
        if (pos > _rings.Count)
        {
            pos = _rings.Count;
        }

        if (pos < 0)
        {
            pos = 0;
        }

        _rings.Insert(pos, ring);
    }

    public void RemoveRing(int pos)
    {
        if (pos < 0 || pos >= _rings.Count)
        {
            return;
        }

        _rings.RemoveAt(pos);
    }

    public void CloseAllRings(double tolerance = Shape.Epsilon)
    {
        foreach (Ring ring in _rings)
        {
            ring.ClosePath(tolerance);
        }
    }

    public void CleanRings(double tolerance = Shape.Epsilon)
    {
        var newRings = new List<Ring>();

        foreach (Ring ring in _rings)
        {
            var newRing = new Ring();
            Point startPoint = null;

            for (int p = 0, pointCount = ring.PointCount; p < pointCount; p++)
            {
                if (p == 0)
                {
                    newRing.AddPoint(startPoint = ring[p]);
                }
                else if (p < pointCount - 1 && startPoint.Distance(ring[p]) < tolerance)
                {
                    newRings.Add(newRing);

                    newRing = new Ring();
                    newRing.AddPoint(startPoint = ring[p]);
                }
                else
                {
                    newRing.AddPoint(ring[p]);
                }
            }

            newRings.Add(newRing);
        }

        _rings = new List<Ring>(newRings.Where(r => r.Area > 0.0));
    }

    public int RingCount
    {
        get
        {
            return _rings.Count;
        }
    }

    public Ring this[int ringIndex]
    {
        get
        {
            if (ringIndex < 0 || ringIndex >= _rings.Count)
            {
                return null;
            }

            return _rings[ringIndex];
        }
    }

    public double Area
    {
        get
        {
            //
            // Hier sollte getestet werden, welche ringe löcher sind und welche nicht...
            //
            VerifyHoles();

            double A = 0.0;
            for (int i = 0; i < RingCount; i++)
            {
                double a = this[i].Area;
                if (this[i] is Hole)
                {
                    A -= a;
                }
                else
                {
                    A += a;
                }
            }
            return A;
        }
    }

    public double Circumference
    {
        get
        {
            double C = 0.0;
            for (int i = 0; i < RingCount; i++)
            {
                C += this[i].Circumference;
            }
            return C;
        }
    }

    public int PointCount
    {
        get
        {
            return this.Rings.Select(r => r.PointCount).Sum();
        }
    }

    public override Envelope ShapeEnvelope
    {
        get
        {
            if (RingCount == 0)
            {
                return null;
            }

            Envelope env = this[0].ShapeEnvelope;
            for (int i = 1; i < RingCount; i++)
            {
                env.Union(this[i].ShapeEnvelope);
            }
            return env;
        }
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        this.VerifyHoles();

        sb.Append("<POLYGON" + base.AXLSrsAttribute() + ">");
        foreach (Ring ring in _rings)
        {
            if (ring == null || ring is Hole)
            {
                continue;
            }

            sb.Append(ring.ArcXML(nfi, RingHoles(ring)));
        }
        sb.Append("</POLYGON>");

        return sb.ToString();
    }

    public override Polygon CalcBuffer(double distance, CancellationTokenSource cts)
    {
        if (this.IsComplex)
        {
            throw new Exception("Can't buffer object with complex geometry!");
        }

        //VerifyHoles();

        //double maxS = .01;
        //while (true)
        //{
        //    try
        //    {
        //        Polygon buffer = SpatialAlgorithms.PolygonBuffer(this, distance, maxS);
        //        return buffer;
        //    }
        //    catch (Exception ex)
        //    {
        //        maxS *= 1.1;
        //        if (maxS > 3)
        //            throw new Exception("Buffer calculation to complex");
        //    }
        //}

        if (this.PointCount > 50000)
        {
            throw new Exception("To many vertices :" + this.PointCount);
        }

        var clipperPolygon = this.ToClipperPolygons(cts);
        var result = clipperPolygon?.Buffer(distance, cts);
        if (result == null)
        {
            throw new Exception("Can't calculate buffer!");
        }

        return result.ToPolygon();
    }

    public override bool IsMultipart
    {
        get
        {
            if (_rings == null)
            {
                return false;
            }

            VerifyHoles();
            return _rings.Where(r => !(r is Hole) && r.Area > 0D).Count() > 1;
        }
    }

    public override IEnumerable<Shape> Multiparts
    {
        get
        {
            if (_rings == null)
            {
                return new Polygon[0];
            }

            VerifyHoles();

            List<Polygon> polygons = new List<Polygon>();

            foreach (var ring in _rings)
            {
                if (ring is Hole)
                {
                    continue;
                }

                var polygon = new Polygon(ring);
                foreach (var hole in RingHoles(ring))
                {
                    polygon.AddRing(hole);
                }
                polygons.Add(polygon);
            }

            return polygons;
        }
    }

    public override void AppendMuiltiparts(Shape shape)
    {
        if (shape is Polygon)
        {
            Polygon polygon = (Polygon)shape;
            for (int i = 0; i < polygon.RingCount; i++)
            {
                this.AddRing(polygon[i]);
            }
        }
        else
        {
            base.AppendMuiltiparts(shape);
        }
    }

    public void VerifyHoles()
    {
        if (_ringsChecked == _rings.Count)
        {
            return;
        }

        if (_rings.Count == 0 || _rings.Count == 1)
        {
            _ringsChecked = _rings.Count;
            return;
        }

        List<Ring> ringsList = _rings;
        _rings = new List<Ring>();

        ringsList.Sort(new RingComparerAreaInv());

        foreach (Ring ring in ringsList)
        {
            bool hole = false;
            Ring outerRing = null;
            foreach (Ring canditateRing in _rings)
            {
                if (SpatialAlgorithms.Jordan(canditateRing, ring))
                {
                    hole = !(canditateRing is Hole);
                    outerRing = canditateRing;
                    break;
                }
            }
            if (!hole)
            {
                if (ring is Hole)
                {
                    _rings.Add(new Ring(ring));
                }
                else
                {
                    _rings.Add(ring);
                }
            }
            else
            {
                if (ring is Hole)
                {
                    ((Hole)ring).OuterRing = outerRing;
                    _rings.Add(ring);
                }
                else
                {
                    Hole h = new Hole(ring);
                    h.OuterRing = outerRing;
                    _rings.Add(h);
                }
            }
        }

        _ringsChecked = _rings.Count;
    }

    public List<Hole> RingHoles(Ring ring)
    {
        List<Hole> holes = new List<Hole>();
        foreach (Ring hole in _rings)
        {
            if (hole is Hole && ((Hole)hole).OuterRing == ring)
            {
                holes.Add((Hole)hole);
            }
        }

        return holes;
    }

    public IEnumerable<Hole> Holes
    {
        get
        {
            VerifyHoles();

            return _rings.Where(r => (r is Hole)).Select(h => (Hole)h);
        }
    }

    public IEnumerable<Ring> Rings
    {
        get
        {
            return _rings.ToArray();
        }
    }

    new static public Polygon FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "POLYGON")
        {
            throw new ArgumentException();
        }

        Polygon poly = new Polygon();
        foreach (XmlNode ring in node.ChildNodes)
        {
            if (ring.Name == "RING")
            {
                poly.AddRing(Ring.FromArcXML(ring, nfi));
                foreach (XmlNode holeNode in ring.SelectNodes("HOLE"))
                {
                    poly.AddRing(Hole.FromArcXML(holeNode, nfi));
                }
            }
            if (ring.Name == "HOLE")
            {
                poly.AddRing(Hole.FromArcXML(ring, nfi));
            }
        }
        return poly;
    }

    public override void Serialize(BinaryWriter w)
    {
        w.Write(_rings.Count);
        foreach (Ring r in _rings)
        {
            r.Serialize(w);
        }
    }
    public override void Deserialize(BinaryReader w)
    {
        _rings.Clear();
        int count = w.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Ring r = new Ring();
            r.Deserialize(w);
            _rings.Add(r);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is Polygon)
        {
            var polygon = (Polygon)obj;

            if (polygon.RingCount != this.RingCount)
            {
                return false;
            }

            if (Math.Abs(polygon.Area - this.Area) > Shape.Epsilon)
            {
                return false;
            }

            var rings = _rings.OrderBy(r => r.Area).ToArray();
            var candidateRings = polygon._rings.OrderBy(r => r.Area).ToArray();

            for (int i = 0; i < rings.Length; i++)
            {
                var ring = rings[i];
                ring.ClosePath();
                for (int j = 0; j < rings.Length; j++)
                {
                    var candidateRing = candidateRings[j];
                    candidateRing.ClosePath();

                    //if (ring.PointCount != candidateRing.PointCount)
                    //    return false;

                    if (Math.Abs(ring.Area - candidateRing.Area) > Shape.Epsilon)
                    {
                        return false;
                    }

                    if (!ring.ShapeEnvelope.Equals(candidateRing.ShapeEnvelope))
                    {
                        return false;
                    }

                    // ToDo:
                    // Testen, ob die Punkte eines Rings alle auf der Kante des anderen liegen...

                    //var ringPoints = ring.ToArray();
                    //var candidatePoints = candidateRing.ToArray();

                    //foreach(var ringPoint in ringPoints)
                    //{
                    //    if (candidatePoints.Where(p => p.Equals(ringPoint)).Count() == 0)
                    //        return false;
                    //}
                }
            }
            return true;
        }

        return false;
    }

    public double Distance2D(Polygon candidate)
    {
        if (candidate == null || candidate.RingCount == 0 || this.RingCount == 0)
        {
            return double.MaxValue;
        }

        double dist = double.MaxValue;
        foreach (var candidateRing in candidate._rings)
        {
            foreach (var candidatePoint in candidateRing.ToArray())
            {
                dist = Math.Min(SpatialAlgorithms.Point2ShapeDistance(this, candidatePoint), dist);
            }
        }
        foreach (var ring in this._rings)
        {
            foreach (var point in ring.ToArray())
            {
                dist = Math.Min(SpatialAlgorithms.Point2ShapeDistance(candidate, point), dist);
            }
        }
        return dist;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public IEnumerable<Ring> ToArray()
    {
        return _rings?.ToArray() ?? new Ring[0];
    }

    public override bool IsValid()
    {
        if (this._rings is null || this._rings.Count == 0)
        {
            return false;
        }

        return _rings.All(p => p.IsValid());
    }
}
