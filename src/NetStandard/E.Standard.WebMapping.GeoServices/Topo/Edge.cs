namespace E.Standard.WebMapping.GeoServices.Topo;

public class Edge
{
    int _v1, _v2;

    public Edge(int v1, int v2)
    {
        _v1 = v1;
        _v2 = v2;
    }

    public int Vertex1 { get { return _v1; } }
    public int Vertex2 { get { return _v2; } }
}
