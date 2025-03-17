namespace E.Standard.WebMapping.Core.Geometry.Topology;

internal class ShapeWrapper<TShape, TData>
    where TShape : Shape
{
    public ShapeWrapper(TShape shape, TData data)
    {
        this.Shape = shape;
        this.Data = data;
    }

    public TShape Shape { get; }
    public TData Data { get; }
}
