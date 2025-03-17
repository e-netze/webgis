namespace E.Standard.WebMapping.Core.Geometry;

public interface IGeometricTransformer2 : IGeometricTransformer
{
    int FromSrsId { get; }
    int ToSrsId { get; }

    void Transform(Shape shape, ShapeSrsProperties setSrsProperties = ShapeSrsProperties.SrsId);

    void InvTransform(Shape shape, ShapeSrsProperties setSrsProperties = ShapeSrsProperties.SrsId);
}
