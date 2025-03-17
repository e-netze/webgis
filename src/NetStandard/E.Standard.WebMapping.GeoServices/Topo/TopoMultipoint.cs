using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoPointCollection : List<int>, ITopoShape
{
    new public void Add(int i)
    {
        if (this.Count > 0 &&
            this[this.Count - 1] == i)
        {
            return;
        }

        base.Add(i);
    }

    #region ITopoShape

    public IEnumerable<int> Vertices
    {
        get
        {
            return this;
        }
    }

    #endregion
}
class TopoMultipoint : TopoPointCollection
{

}
