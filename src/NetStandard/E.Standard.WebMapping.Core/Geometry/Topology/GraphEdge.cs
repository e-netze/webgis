namespace E.Standard.WebMapping.Core.Geometry.Topology;

internal class GraphEdge
{
    public GraphEdge(int index, int from, int to)
    {
        Index = index;
        From = from;
        To = to;
    }

    public int Index { get; }
    public int From { get; }
    public int To { get; }

    public bool MarkAsReverse { get; set; }
}
