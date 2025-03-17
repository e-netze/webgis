using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoPolygon : List<TopoRing>, ITopoShape
{
    public IEnumerable<int> Vertices
    {
        get
        {
            List<int> ret = new List<int>();
            foreach (var p in this)
            {
                ret.AddRange(p);
            }

            return ret;
        }
    }
}
