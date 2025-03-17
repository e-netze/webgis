using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoPoint : ITopoShape
{
    int _v;

    public TopoPoint(int v)
    {
        _v = v;
    }

    public int Vertex { get { return _v; } }

    #region ITopoShape

    public IEnumerable<int> Vertices
    {
        get
        {
            return new int[] { _v };
        }
    }

    #endregion
}
