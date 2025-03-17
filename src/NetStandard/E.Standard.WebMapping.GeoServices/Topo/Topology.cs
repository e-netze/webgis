using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.Topo;

public class Topology<T>
{
    private VertexList<T> _vlist;
    private VertexList<T> _vlist4326;
    private List<ITopoShape> _shapes;
    private List<TopoEnvelope> _envelopes;
    private List<object> _meta;
    private List<int> _metaIndex;

    public Topology()
    {
        _vlist = new VertexList<T>();
        _vlist4326 = new VertexList<T>();
        _shapes = new List<ITopoShape>();
        _envelopes = new List<TopoEnvelope>();
        _meta = new List<object>();
        _metaIndex = new List<int>();

        _vlist.OnVertextInserted += _vlist_OnVertextInserted;
        _vlist.OnBBoxInserted += _vlist_OnBBoxInserted;
    }

    public int AddShape(Shape shape)
    {
        return AddShape(shape, false, null, null);
    }

    private GeometricTransformerPro _transformer = null;

    public double Espsilon
    {
        get
        {
            if (_transformer != null && _transformer.ToIsProjective == false)
            {
                return 1e-8;  // ??? Wie genau bei geographischen Koordinaten
            }

            return 1e-3;
        }
    }

    public int SrsId { get; set; }

    public int AddShape(Shape shape, bool storeEnvelopes, object meta, Envelope clipEnvelope, GeometricTransformerPro transformer = null)
    {
        _transformer = transformer;

        _vlist.Espilon = this.Espsilon;

        if (shape == null)
        {
            return -1;
        }

        if (shape is Point)
        {
            if (clipEnvelope != null)
            {
                if (!clipEnvelope.Contains(((Point)shape).ShapeEnvelope))
                {
                    return -1;
                }
            }
            _shapes.Add(new TopoPoint(_vlist.InsertPoint(C(((Point)shape).X), C(((Point)shape).Y))));
        }
        else if (shape is MultiPoint)
        {
            TopoMultipoint tmp = new TopoMultipoint();
            MultiPoint mp = (MultiPoint)shape;

            for (int i = 0; i < mp.PointCount; i++)
            {
                Point p = mp[i];
                if (p == null)
                {
                    continue;
                }

                if (clipEnvelope != null)
                {
                    if (!clipEnvelope.Contains(p.ShapeEnvelope))
                    {
                        continue;
                    }
                }
                tmp.Add(_vlist.InsertPoint(C(p.X), C(p.Y)));
            }
            _shapes.Add(tmp);
        }
        else if (shape is Polyline)
        {
            TopoPolyline tpl = new TopoPolyline();
            Polyline pl = (Polyline)shape;

            if (clipEnvelope != null)
            {
                pl = Clip.PerformClip(clipEnvelope, pl) as Polyline;
                if (pl == null)
                {
                    return -1;
                }
            }
            for (int i = 0; i < pl.PathCount; i++)
            {
                TopoPath tp = new TopoPath();
                for (int j = 0; j < pl[i].PointCount; j++)
                {
                    Point p = pl[i][j];
                    tp.Add(_vlist.InsertPoint(C(p.X), C(p.Y), IsClipPoint(p, clipEnvelope) ? "clip" : null));
                }
                tpl.Add(tp);
            }
            _shapes.Add(tpl);
        }
        else if (shape is Polygon)
        {
            TopoPolygon tpg = new TopoPolygon();
            Polygon pg = (Polygon)shape;

            if (clipEnvelope != null)
            {
                pg = Clip.PerformClip(clipEnvelope, pg) as Polygon;
                if (pg == null)
                {
                    return -1;
                }
            }

            for (int i = 0; i < pg.RingCount; i++)
            {
                TopoRing tr = new TopoRing();
                for (int j = 0; j < pg[i].PointCount; j++)
                {
                    Point p = pg[i][j];
                    tr.Add(_vlist.InsertPoint(C(p.X), C(p.Y), IsClipPoint(p, clipEnvelope) ? "clip" : null));
                }
                tpg.Add(tr);
            }
            _shapes.Add(tpg);
        }
        else
        {
            return -1;
        }


        if (meta != null)
        {
            if (!_meta.Contains(meta))
            {
                _meta.Add(meta);
            }

            _metaIndex.Add(_meta.IndexOf(meta));
        }

        if (storeEnvelopes)
        {
            Envelope env = shape.ShapeEnvelope;

            var envIndices = _vlist.InsertBBox(C(env.MinX), C(env.MinY), C(env.MaxX), C(env.MaxY));
            _envelopes.Add(new TopoEnvelope(envIndices[0], envIndices[1]));
        }
        return _shapes.Count - 1;
    }

    private T C(object o)
    {
        if (typeof(T).Equals(typeof(int)))
        {
            return (T)(object)Convert.ToInt32(o);
        }

        if (typeof(T).Equals(typeof(double)))
        {
            return (T)(object)Convert.ToDouble(o);
        }

        throw new Exception("Unknown Topology-Generic type: " + typeof(T).GetType());
    }

    private void _vlist_OnVertextInserted(Vertex<T> vertex)
    {
        InsertPoint4326(_transformer, vertex.X, vertex.Y, vertex.M);
    }

    private void _vlist_OnBBoxInserted(Vertex<T> lowerLeft, Vertex<T> upperRight)
    {
        InsertBBox4326(_transformer, lowerLeft, upperRight);
    }

    private void InsertPoint4326(GeometricTransformerPro transformer, T x, T y, object M)
    {
        if (transformer != null && typeof(T).Equals(typeof(double)))
        {
            Point p = new Point(Convert.ToDouble(x), Convert.ToDouble(y));
            transformer.InvTransform(p);
            _vlist4326.Vertices.Add(new Vertex<T>((T)(object)p.X, (T)(object)p.Y, M));
        }
    }

    private void InsertBBox4326(GeometricTransformerPro transformer, Vertex<T> lowerLeft, Vertex<T> upperRight)
    {
        if (transformer != null && typeof(T).Equals(typeof(double)))
        {
            var cornerPoints = new Point[4];
            cornerPoints[0] = new Point(Convert.ToDouble(lowerLeft.X), Convert.ToDouble(lowerLeft.Y));
            cornerPoints[1] = new Point(Convert.ToDouble(lowerLeft.X), Convert.ToDouble(upperRight.Y));
            cornerPoints[2] = new Point(Convert.ToDouble(upperRight.X), Convert.ToDouble(upperRight.Y));
            cornerPoints[3] = new Point(Convert.ToDouble(upperRight.X), Convert.ToDouble(lowerLeft.Y));

            transformer.InvTransform(cornerPoints[0]);
            transformer.InvTransform(cornerPoints[1]);
            transformer.InvTransform(cornerPoints[2]);
            transformer.InvTransform(cornerPoints[3]);

            var minX = cornerPoints.Select(p => p.X).Min();
            var minY = cornerPoints.Select(p => p.Y).Min();
            var maxX = cornerPoints.Select(p => p.X).Max();
            var maxY = cornerPoints.Select(p => p.Y).Max();

            _vlist4326.Vertices.Add(new Vertex<T>((T)(object)minX, (T)(object)minX));
            _vlist4326.Vertices.Add(new Vertex<T>((T)(object)maxX, (T)(object)maxY));
        }
    }

    private bool IsClipPoint(Point p, Envelope clipEnvelpe)
    {
        double epsi = this.Espsilon;

        if (clipEnvelpe == null)
        {
            return false;
        }

        if (((Math.Abs(p.X - clipEnvelpe.MinX) <= epsi || Math.Abs(p.X - clipEnvelpe.MaxX) <= epsi) && p.Y >= clipEnvelpe.MinY && p.Y <= clipEnvelpe.MaxY) ||
            ((Math.Abs(p.Y - clipEnvelpe.MinY) <= epsi || Math.Abs(p.Y - clipEnvelpe.MaxY) <= epsi) && p.X >= clipEnvelpe.MinX && p.X <= clipEnvelpe.MaxX))
        {
            return true;
        }

        return false;
    }

    #region Json

    public object ToJsonObject()
    {
        return new
        {
            srs_id = this.SrsId,
            shapes = ToJsonShapes(),
            vertices = _vlist.ToJson(),
            vertices_wgs84 = _vlist4326.ToJson(),
            envelopes = _envelopes,
            meta = _meta,
            meta_index = _metaIndex
        };
    }

    private List<int[]> ToJsonShapes()
    {
        List<int[]> shapes = new List<int[]>();

        foreach (object shape in _shapes)
        {
            List<int> values = new List<int>();

            if (shape is TopoPoint)
            {
                shapes.Add(ToJsonPoint((TopoPoint)shape));
            }
            else if (shape is TopoMultipoint)
            {
                shapes.Add(ToJsonMultiPoint((TopoMultipoint)shape));
            }
            else if (shape is TopoPolyline)
            {
                shapes.Add(ToJsonPolyline((TopoPolyline)shape));
            }
            else if (shape is TopoPolygon)
            {
                shapes.Add(ToJsonPolgon((TopoPolygon)shape));
            }
        }

        return shapes;
    }

    private int[] ToJsonPoint(TopoPoint point)
    {
        return new int[] { 0, point.Vertex };
    }

    private int[] ToJsonMultiPoint(TopoMultipoint mpoint)
    {
        if (mpoint.Count == 1)
        {
            return ToJsonPoint(new TopoPoint(mpoint[0]));
        }

        List<int> ret = new List<int>();

        ret.AddRange(new int[] { 1, mpoint.Count });
        ret.AddRange(mpoint);

        return ret.ToArray();
    }

    private int[] ToJsonPolyline(TopoPolyline pline)
    {
        List<int> ret = new List<int>();
        ret.AddRange(new int[] { 2, pline.Count });

        foreach (var path in pline)
        {
            ret.Add(path.Count);
            ret.AddRange(path);
        }

        return ret.ToArray();
    }

    private int[] ToJsonPolgon(TopoPolygon poly)
    {
        List<int> ret = new List<int>();
        ret.AddRange(new int[] { 2, poly.Count });

        foreach (var ring in poly)
        {
            ring.CloseRing();

            ret.Add(ring.Count);
            ret.AddRange(ring);
        }

        return ret.ToArray();
    }

    private void ToJsonGraph()
    {
        Dictionary<int, Dictionary<int, double>> graph = new Dictionary<int, Dictionary<int, double>>();
        for (int i = 0, to = _vlist.Count; i < to; i++)
        {

        }
    }

    #endregion

    #region JavaScript

    public string ToJavaScript(string vertexListName, string shapeListName)
    {
        return ToJavaScript(vertexListName, shapeListName, null, null, null);
    }
    public string ToJavaScript(string vertexListName, string shapeListName, string envelopeListName, string metaListName, string metaIndexListName)
    {
        if (_shapes.Count == 0)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        sb.Append(_vlist.ToJavaScript(vertexListName));
        sb.Append(shapeListName + "=new Array(");

        bool first = true;
        foreach (object shape in _shapes)
        {
            if (!first)
            {
                sb.Append(",");
            }
            else
            {
                first = false;
            }

            if (shape is TopoPoint)
            {
                AddPointJavaScript((TopoPoint)shape, sb);
            }

            if (shape is TopoMultipoint)
            {
                AddMultiPointJavaScript((TopoMultipoint)shape, sb);
            }

            if (shape is TopoPolyline)
            {
                AddPolylineJavaScript((TopoPolyline)shape, sb);
            }

            if (shape is TopoPolygon)
            {
                AddPolygonJavaScript((TopoPolygon)shape, sb);
            }
        }
        sb.Append(");");

        if (!String.IsNullOrEmpty(envelopeListName))
        {
            first = true;
            sb.Append(envelopeListName + "=new Array(");
            foreach (TopoEnvelope env in _envelopes)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }

                sb.Append(env.LowerLeft + "," + env.UpperRight);
            }
            sb.Append(");");
        }
        if (!String.IsNullOrEmpty(metaListName) && !String.IsNullOrEmpty(metaIndexListName))
        {
            first = true;
            sb.Append(metaListName + "=new Array(");
            foreach (string m in _meta)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }

                sb.Append("'" + m.Replace("\"", "").Replace("\'", "").Replace("\n", "").Replace("\r", "").Replace("\\", "") + "'");
            }
            sb.Append(");");

            first = true;
            sb.Append(metaIndexListName + "=new Array(");
            if (_metaIndex.Count == 1)
            {
                sb.Append(");" + metaIndexListName + ".push(" + 0 + ");");
            }
            else
            {
                foreach (int i in _metaIndex)
                {
                    if (!first)
                    {
                        sb.Append(",");
                    }
                    else
                    {
                        first = false;
                    }

                    sb.Append(i);
                }
                sb.Append(");");
            }
        }

        return sb.ToString();
    }

    private void AddPointJavaScript(TopoPoint point, StringBuilder sb)
    {
        sb.Append("new Array(0," + point.Vertex + ")");
    }
    private void AddIntList(List<int> l, StringBuilder sb)
    {
        bool first = true;
        foreach (int i in l)
        {
            if (!first)
            {
                sb.Append(",");
            }
            else
            {
                first = false;
            }

            sb.Append(i);
        }
    }
    private void AddMultiPointJavaScript(TopoMultipoint mpoint, StringBuilder sb)
    {
        if (mpoint.Count == 1)
        {
            AddPointJavaScript(new TopoPoint(mpoint[0]), sb);
            return;
        }
        sb.Append("new Array(1," + mpoint.Count);
        if (mpoint.Count > 0)
        {
            sb.Append(",");
            AddIntList(mpoint, sb);
        }
        sb.Append(")");
    }
    private void AddPolylineJavaScript(TopoPolyline pline, StringBuilder sb)
    {
        sb.Append("new Array(2," + pline.Count);

        if (pline.Count > 0)
        {
            sb.Append(",");
            bool first = true;
            foreach (TopoPath path in pline)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }

                sb.Append(path.Count);
                if (path.Count > 0)
                {
                    sb.Append(",");
                    AddIntList(path, sb);
                }
            }
        }
        sb.Append(")");
    }
    private void AddPolygonJavaScript(TopoPolygon poly, StringBuilder sb)
    {
        sb.Append("new Array(3," + poly.Count);

        if (poly.Count > 0)
        {
            sb.Append(",");
            bool first = true;
            foreach (TopoRing ring in poly)
            {
                ring.CloseRing();

                if (!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }

                sb.Append(ring.Count);
                if (ring.Count > 0)
                {
                    sb.Append(",");
                    AddIntList(ring, sb);
                }
            }
        }
        sb.Append(")");

    }

    #endregion
}
