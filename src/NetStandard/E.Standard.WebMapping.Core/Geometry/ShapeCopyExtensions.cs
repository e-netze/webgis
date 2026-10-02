using System;
using System.IO;
using System.Text;

namespace E.Standard.WebMapping.Core.Geometry;

public static class ShapeCopyExtensions
{
    public static Shape DeepCopy(this Shape shape)
    {
        if (shape is null)
        {
            return null;
        }

        using var stream = new MemoryStream();

        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            Shape.SerializeShape(shape, writer);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var copy = Shape.DeserializeShape(reader);
        copy.SrsId = shape.SrsId;
        copy.SrsP4Parameters = shape.SrsP4Parameters;

        return copy;
    }

    /// <summary>
    /// Returns a transformed copy; the source shape stays unchanged.
    /// </summary>
    public static Shape TransformedCopy(
        this Shape shape,
        int targetSRefId,
        Func<int, int, IGeometricTransformer> createTransformer)
    {
        if (shape is null || shape.SrsId == targetSRefId)
        {
            return shape;
        }

        var copy = shape.DeepCopy();
        using (var transformer = createTransformer(shape.SrsId, targetSRefId))
        {
            transformer.Transform(copy);
        }

        return copy;
    }
}
