#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.CMS.Core;
using E.Standard.Parsing;
using E.Standard.Parsing.SimpleExpressions;
using E.Standard.Parsing.StructuredExpressions;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.CMS.Expressions;

public static class ExpressionEvaluator
{
    public static string EvaluateAutoValue(
        WebMapping.Core.Feature feature,
        string autoValue,
        Func<int, string, Shape>? transformShape = null)
    {
        ArgumentNullException.ThrowIfNull(autoValue);
        autoValue = autoValue.Trim();
        if (!autoValue.StartsWith('='))
        {
            throw new ArgumentException(
                "An AutoValue expression must start with '='.",
                nameof(autoValue));
        }

        var expression = autoValue[1..];
        if (ExpressionClassifier.Classify(expression) == ExpressionSyntax.StructuredExpression)
        {
            return EvaluateStructuredExpression(
                new Parsing.StructuredExpressions.ExpressionEvaluator().Compile(expression),
                feature,
                transformShape);
        }

        return EvaluateLegacyAutoValue(feature, expression);
    }

    public static string EvaluateTableColumn(
        WebMapping.Core.Feature feature,
        string expression,
        Func<int, string, Shape>? transformShape = null)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (ExpressionClassifier.Classify(expression) == ExpressionSyntax.StructuredExpression)
        {
            return EvaluateStructuredExpression(
                new Parsing.StructuredExpressions.ExpressionEvaluator().Compile(expression),
                feature,
                transformShape);
        }

        return EvaluateLegacyTableColumn(feature, expression);
    }

    public static string EvaluateStructuredExpression(
        CompiledExpression expression,
        WebMapping.Core.Feature feature,
        Func<int, string, Shape>? transformShape = null)
        => expression
            .Evaluate(
                fieldName => ResolveField(feature, fieldName),
                (functionName, arguments) => ShapeExpressionFunctions.Resolve(
                    feature?.Shape,
                    functionName,
                    arguments,
                    targetSRefId => transformShape?.Invoke(targetSRefId, functionName)))
            .ToInvariantString();

    public static string EvaluateLegacyTableColumn(
        WebMapping.Core.Feature feature,
        string expression,
        IReadOnlyList<string>? legacyParameters = null,
        bool? containsLegacyEvalExpression = null)
    {
        var value = Globals.SolveExpression(
            feature,
            expression,
            legacyParameters ?? Helper.GetKeyParameters(expression));

        return (containsLegacyEvalExpression ?? expression.Contains('$'))
            ? Eval.ParseEvalExpression(value)
            : value;
    }

    private static string EvaluateLegacyAutoValue(
        WebMapping.Core.Feature feature,
        string expression)
    {
        var keys = TemplateScanner.Scan(expression)
            .Aggregate(
                String.Empty,
                (current, parameter) => current.Length == 0
                    ? parameter.Key
                    : current + ";" + parameter.Key)
            .Split(';');

        if (keys.Length == 1 && keys[0].Length == 0)
        {
            return expression.Trim();
        }

        var value = expression;
        var keyValue = String.Empty;
        foreach (var key in keys)
        {
            if (key is ":shape_len" or ":shape_len_int" or ":shape_area" or ":shape_area_int")
            {
                keyValue = FormatNumericShapeMetric(key[1..], feature?.Shape) ?? String.Empty;
            }
            else if (feature is not null && feature[key] is string featureValue)
            {
                keyValue = featureValue;
            }

            value = value.Replace($"[{key}]", keyValue);
        }

        return value.Trim();
    }

    private static ExpressionValue? ResolveField(
        WebMapping.Core.Feature feature,
        string fieldName)
    {
        var attribute = feature?.Attributes?[fieldName];
        if (attribute is null)
        {
            return null;
        }

        var value = attribute.Value;
        if (Boolean.TryParse(value, out var boolean))
        {
            return ExpressionValue.From(boolean);
        }

        return ExpressionValue.From(value);
    }

    public static string? FormatNumericShapeMetric(string shapeValueName, Shape? shape)
    {
        var asInteger = shapeValueName is "shape_len_int" or "shape_area_int";
        var metricName = asInteger
            ? shapeValueName[..^"_int".Length]
            : shapeValueName;

        if (ShapeMetrics.GetNumericMetric(metricName, shape) is not double value)
        {
            return null;
        }

        if (asInteger)
        {
            return Math.Round(value, 0).ToString();
        }

        return metricName is "shape_len" or "shape_area" or "shape_perimeter"
            ? Math.Round(value, 2).ToPlatformNumberString()
            : value.ToPlatformNumberString();
    }
}
