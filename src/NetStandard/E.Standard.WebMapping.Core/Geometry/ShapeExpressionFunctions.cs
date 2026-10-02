using System;
using System.Collections.Generic;

using E.Standard.Parsing.StructuredExpressions;

namespace E.Standard.WebMapping.Core.Geometry;

public static class ShapeExpressionFunctions
{
    public static ExpressionValue? Resolve(
        Shape shape,
        string functionName,
        IReadOnlyList<ExpressionValue> arguments,
        Func<int, Shape> transformShape = null)
    {
        var normalizedName = functionName.ToLowerInvariant();
        if (normalizedName is not (
            "shape_len"
            or "shape_area"
            or "shape_perimeter"
            or "shape_centroid_x"
            or "shape_centroid_y"))
        {
            return null;
        }

        if (arguments.Count > 1)
        {
            throw new ExpressionEvaluationException(
                $"Function '{functionName}' expects 0 or 1 argument(s), but got {arguments.Count}",
                0);
        }

        if (shape is null)
        {
            return ExpressionValue.Null;
        }

        if (arguments.Count == 1)
        {
            var number = arguments[0].NumberValue(0);
            if (number != Math.Truncate(number) || number is <= 0 or > Int32.MaxValue)
            {
                throw new ExpressionEvaluationException(
                    $"Function '{functionName}' requires a positive integer SRefId",
                    0);
            }

            var targetSRefId = (int)number;
            if (targetSRefId != shape.SrsId)
            {
                if (transformShape is null)
                {
                    throw new ExpressionEvaluationException(
                        $"Function '{functionName}' cannot transform the shape to SRefId {targetSRefId}",
                        0);
                }

                shape = transformShape(targetSRefId);
            }
        }

        return ShapeMetrics.GetNumericMetric(normalizedName, shape) is double value
            ? ExpressionValue.From(value)
            : ExpressionValue.Null;
    }
}
