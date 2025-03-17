using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoPolyline : List<TopoPath>, ITopoShape
{
    #region ITopoShape

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

    #endregion
}
