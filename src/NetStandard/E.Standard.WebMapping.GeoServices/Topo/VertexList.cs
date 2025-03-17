using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.Topo;

public class VertexList<T>
{
    private List<Vertex<T>> _vertices;

    public VertexList()
    {
        _vertices = new List<Vertex<T>>();
    }

    public delegate void InsertedVertexEventHandler(Vertex<T> vertex);
    public event InsertedVertexEventHandler OnVertextInserted = null;

    public delegate void InsertedBBoxEventHandler(Vertex<T> lowerLeft, Vertex<T> upperRight);
    public event InsertedBBoxEventHandler OnBBoxInserted = null;

    public double Espilon = 0D;

    public int InsertPoint(T x, T y, object m = null)
    {
        Vertex<T> vertex = new Vertex<T>(x, y, m);

        int index = 0;
        foreach (Vertex<T> v in _vertices)
        {
            if (v.IsNode == true && v.Equals(vertex, Espilon))
            {
                return index;
            }

            index++;
        }
        _vertices.Add(vertex);
        OnVertextInserted?.Invoke(vertex);

        return _vertices.Count - 1;
    }

    public int[] InsertBBox(T minX, T minY, T maxX, T maxY)
    {
        Vertex<T> lowerLeft = new Vertex<T>(minX, minY, isNode: false);
        Vertex<T> upperRight = new Vertex<T>(maxX, maxY, isNode: false);

        _vertices.Add(lowerLeft);
        _vertices.Add(upperRight);

        OnBBoxInserted?.Invoke(lowerLeft, upperRight);

        return new int[] { _vertices.Count - 2, _vertices.Count - 1 };
    }

    public List<Vertex<T>> Vertices { get { return _vertices; } }

    public int Count { get { return _vertices.Count; } }

    public Vertex<T> this[int index]
    {
        get { return _vertices[index]; }
    }

    #region Json

    public object ToJson()
    {
        return _vertices.Select(v =>
           v.M != null ? new object[] { v.X, v.Y, v.M } : new object[] { v.X, v.Y }).ToArray();
    }

    #endregion

    #region JavaScript

    public string ToJavaScript(string name)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(name + "=new Array(");

        int i = 0;
        foreach (Vertex<T> v in _vertices)
        {
            if (i > 0)
            {
                sb.Append(",");
            }

            sb.Append("new _v(" + v.ToString() + ")");
            i++;
        }
        sb.Append(");");
        return sb.ToString();
    }

    #endregion

    #region Clone

    public VertexList<T> Clone()
    {
        var clone = new VertexList<T>();

        foreach (var vertex in _vertices)
        {
            clone._vertices.Add(new Vertex<T>(vertex.X, vertex.Y));
        }

        return clone;
    }

    #endregion
}
