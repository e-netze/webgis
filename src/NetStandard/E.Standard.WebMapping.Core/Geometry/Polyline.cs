using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class Polyline : Shape
{
    private readonly List<Path> _paths;

    public Polyline()
    {
        _paths = new List<Path>();
    }

    public Polyline(Path path)
        : this()
    {
        _paths.Add(path);
    }

    public Polyline(IEnumerable<Path> paths)
        : this()
    {
        if (paths != null)
        {
            _paths.AddRange(paths);
        }
    }

    public Polyline(Point[] points)
        : this()
    {
        Path path = new Path();
        _paths.Add(path);
        if (points != null)
        {
            foreach (Point point in points)
            {
                if (point == null)
                {
                    continue;
                }

                path.AddPoint(point);
            }
        }
    }

    #region IPolyline Member

    /// <summary>
    /// Adds a path.
    /// </summary>
    /// <param name="path"></param>
    public void AddPath(Path path)
    {
        _paths.Add(path);
    }

    /// <summary>
    /// Adds a path at a given position.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="pos"></param>
    public void InsertPath(Path path, int pos)
    {
        if (pos > _paths.Count)
        {
            pos = _paths.Count;
        }

        if (pos < 0)
        {
            pos = 0;
        }

        _paths.Insert(pos, path);
    }

    /// <summary>
    /// Removes the path at a given position (index).
    /// </summary>
    /// <param name="pos"></param>
    public void RemovePath(int pos)
    {
        if (pos < 0 || pos >= _paths.Count)
        {
            return;
        }

        _paths.RemoveAt(pos);
    }

    /// <summary>
    /// The number of paths.
    /// </summary>
    public int PathCount
    {
        get
        {
            return _paths.Count;
        }
    }

    /// <summary>
    /// Returns the path at the given position (index).
    /// </summary>
    public Path this[int pathIndex]
    {
        get
        {
            if (pathIndex < 0 || pathIndex >= _paths.Count)
            {
                return null;
            }

            return _paths[pathIndex];
        }
    }

    #endregion

    public override Envelope ShapeEnvelope
    {
        get
        {
            if (PathCount == 0)
            {
                return null;
            }

            Envelope env = this[0].ShapeEnvelope;
            for (int i = 1; i < PathCount; i++)
            {
                env.Union(this[i].ShapeEnvelope);
            }
            return env;
        }
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<POLYLINE" + base.AXLSrsAttribute() + ">");
        foreach (Path path in _paths)
        {
            if (path == null)
            {
                continue;
            }

            sb.Append(path.ArcXML(nfi));
        }
        sb.Append("</POLYLINE>");

        return sb.ToString();
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

        Polygon buffer = SpatialAlgorithms.PolylineBuffer(this, distance, cts);
        return buffer;
    }

    public override bool IsMultipart
    {
        get
        {
            if (_paths == null)
            {
                return false;
            }

            return _paths.Where(p => p.Length > 0D).Count() > 1;
        }
    }

    public override IEnumerable<Shape> Multiparts
    {
        get
        {
            if (_paths == null)
            {
                return new Polyline[0];
            }

            return _paths.Select(p => new Polyline(p)).ToArray();
        }
    }

    public override void AppendMuiltiparts(Shape shape)
    {
        if (shape is Polyline)
        {
            Polyline polyline = (Polyline)shape;
            for (int i = 0; i < polyline.PathCount; i++)
            {
                this.AddPath(polyline[i]);
            }
        }
        else
        {
            base.AppendMuiltiparts(shape);
        }
    }

    new static public Polyline FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "POLYLINE")
        {
            throw new ArgumentException();
        }

        Polyline pLine = new Polyline();
        foreach (XmlNode path in node.ChildNodes)
        {
            if (path.Name == "PATH")
            {
                pLine.AddPath(Path.FromArcXML(path, nfi));
            }
        }
        return pLine;
    }

    public override void Serialize(BinaryWriter w)
    {
        w.Write(_paths.Count);
        foreach (Path p in _paths)
        {
            p.Serialize(w);
        }
    }
    public override void Deserialize(BinaryReader w)
    {
        _paths.Clear();
        int count = w.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Path p = new Path();
            p.Deserialize(w);
            _paths.Add(p);
        }
    }

    public double Length
    {
        get
        {
            //
            // Hier sollte getestet werden, welche ringe löcher sind und welche nicht...
            //

            double L = 0.0;
            for (int i = 0; i < PathCount; i++)
            {
                double l = this[i].Length;

                L += l;
            }
            return L;
        }
    }

    public double Length3D
    {
        get
        {
            double L = 0.0;
            for (int i = 0; i < PathCount; i++)
            {
                double l = this[i].Length3D;

                L += l;
            }
            return L;
        }
    }

    public double Distance2D(Polyline candidate)
    {
        if (candidate == null || candidate.PathCount == 0 || this.PathCount == 0)
        {
            return double.MaxValue;
        }

        double dist = double.MaxValue;
        foreach (var candidatePath in candidate._paths)
        {
            foreach (var candidatePoint in candidatePath.ToArray())
            {
                dist = Math.Min(SpatialAlgorithms.Point2ShapeDistance(this, candidatePoint), dist);
            }
        }
        foreach (var path in this._paths)
        {
            foreach (var point in path.ToArray())
            {
                dist = Math.Min(SpatialAlgorithms.Point2ShapeDistance(candidate, point), dist);
            }
        }
        return dist;
    }

    public Path[] ToArray()
    {
        return _paths?.ToArray() ?? new Path[0];
    }
}
