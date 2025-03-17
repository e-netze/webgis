using System;

namespace E.Standard.WebMapping.Core.Geometry;

public interface IGeometricTransformer : IDisposable
{
    void Transform(double[] x, double[] y);

    void Transform(Shape shape);

    void InvTransform(Shape shape);
}
