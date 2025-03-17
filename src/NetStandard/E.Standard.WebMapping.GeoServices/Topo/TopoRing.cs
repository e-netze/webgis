namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoRing : TopoPointCollection
{
    public void CloseRing()
    {
        if (this.Count == 0)
        {
            return;
        }

        if (this[this.Count - 1] != this[0])
        {
            this.Add(this[0]);
        }
    }
}
