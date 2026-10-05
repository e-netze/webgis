#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

using E.Standard.GeoJson;
using E.Standard.GeoJson.Extensions;
using E.Standard.Json;
using E.Standard.Parsing;
using E.Standard.Parsing.StructuredExpressions;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.CMS.Expressions;

public enum CmsExpressionType
{
    AutoValue,
    TableColumn
}

public sealed record ExpressionPlaygroundResult(
    int Index,
    string? Result,
    string? Error);

public static class ExpressionPlaygroundService
{
    public const int MaxGeoJsonSizeBytes = 5 * 1024 * 1024;
    public const int MaxFeatureCount = 1000;

    private static readonly SpatialReferenceCollection SpatialReferences = new(useEmbeddedCsv: true);

    public static ExpressionSyntax ClassifySyntax(string expression, CmsExpressionType expressionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        if (!Enum.IsDefined(expressionType))
        {
            throw new ArgumentOutOfRangeException(nameof(expressionType));
        }

        var expressionSource = expression;
        if (expressionType == CmsExpressionType.AutoValue)
        {
            expressionSource = expression.Trim();
            if (!expressionSource.StartsWith('='))
            {
                throw new ArgumentException("An AutoValue expression must start with '='.");
            }

            expressionSource = expressionSource[1..];
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(expressionSource);
        return ExpressionClassifier.Classify(expressionSource);
    }

    public static IReadOnlyList<ExpressionPlaygroundResult> Evaluate(
        string geoJson,
        string expression,
        CmsExpressionType expressionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(geoJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        if (Encoding.UTF8.GetByteCount(geoJson) > MaxGeoJsonSizeBytes)
        {
            throw new ArgumentException(
                $"GeoJSON exceeds the maximum size of {MaxGeoJsonSizeBytes / (1024 * 1024)} MB.",
                nameof(geoJson));
        }

        if (!Enum.IsDefined(expressionType))
        {
            throw new ArgumentOutOfRangeException(nameof(expressionType));
        }

        using var document = JsonDocument.Parse(geoJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("GeoJSON must be a Feature or FeatureCollection.");
        }

        var type = typeElement.GetString();
        GeoJsonFeature[] features;
        var srefId = 4326;
        if (String.Equals(type, "Feature", StringComparison.OrdinalIgnoreCase))
        {
            ValidateFeature(root);
            features =
            [
                JSerializer.Deserialize<GeoJsonFeature>(geoJson)
                    ?? throw new FormatException("The GeoJSON Feature could not be read.")
            ];
        }
        else if (String.Equals(type, "FeatureCollection", StringComparison.OrdinalIgnoreCase))
        {
            if (!root.TryGetProperty("features", out var featuresElement)
                || featuresElement.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("The GeoJSON FeatureCollection has no 'features' array.");
            }

            var featureCount = 0;
            foreach (var featureElement in featuresElement.EnumerateArray())
            {
                featureCount++;
                if (featureCount > MaxFeatureCount)
                {
                    throw new ArgumentException(
                        $"FeatureCollection exceeds the maximum of {MaxFeatureCount} features.",
                        nameof(geoJson));
                }

                ValidateFeature(featureElement);
            }

            var collection = JSerializer.Deserialize<GeoJsonFeatures>(geoJson)
                ?? throw new FormatException("The GeoJSON FeatureCollection could not be read.");
            features = collection.Features
                ?? throw new FormatException("The GeoJSON FeatureCollection has no 'features' array.");
            var collectionCrs = collection.Crs;
            if (collectionCrs is not null
                && (collectionCrs.Properties is null
                    || !collectionCrs.Properties.TryGetValue("name", out var crsName)
                    || crsName is null))
            {
                throw new FormatException("The GeoJSON FeatureCollection has an unsupported CRS.");
            }

            var collectionSrefId = collectionCrs?.TryGetEpsg() ?? 0;
            if (collectionCrs is not null && collectionSrefId <= 0)
            {
                throw new FormatException("The GeoJSON FeatureCollection has an unsupported CRS.");
            }

            if (collectionSrefId > 0)
            {
                srefId = collectionSrefId;
            }
        }
        else
        {
            throw new FormatException("GeoJSON must be a Feature or FeatureCollection.");
        }

        var expressionSource = expressionType == CmsExpressionType.AutoValue
            ? expression.Trim()[1..]
            : expression;
        var syntax = ClassifySyntax(expression, expressionType);
        CompiledExpression? compiledExpression = null;
        if (syntax == ExpressionSyntax.StructuredExpression)
        {
            try
            {
                compiledExpression = new ExpressionEvaluator().Compile(expressionSource);
            }
            catch (ExpressionException exception)
            {
                return Enumerable.Range(1, features.Length)
                    .Select(index => new ExpressionPlaygroundResult(index, null, exception.Message))
                    .ToArray();
            }
        }

        var results = new List<ExpressionPlaygroundResult>(features.Length);
        for (var index = 0; index < features.Length; index++)
        {
            try
            {
                var feature = ToFeature(
                    features[index] ?? throw new FormatException("A FeatureCollection contains a null feature."),
                    srefId);
                var result = compiledExpression is not null
                    ? CmsExpressionEvaluator.EvaluateStructuredExpression(
                        compiledExpression,
                        feature,
                        (targetSrefId, functionName) => TransformShape(
                            feature.Shape,
                            targetSrefId,
                            functionName))
                    : expressionType switch
                {
                    CmsExpressionType.AutoValue => CmsExpressionEvaluator.EvaluateAutoValue(feature, expression),
                    CmsExpressionType.TableColumn => CmsExpressionEvaluator.EvaluateTableColumn(feature, expression),
                    _ => throw new ArgumentOutOfRangeException(nameof(expressionType))
                };
                results.Add(new ExpressionPlaygroundResult(index + 1, result, null));
            }
            catch (ExpressionException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (JsonException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (Newtonsoft.Json.JsonException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (FormatException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (ArgumentException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                results.Add(new ExpressionPlaygroundResult(index + 1, null, exception.Message));
            }
            catch (IndexOutOfRangeException)
            {
                results.Add(new ExpressionPlaygroundResult(
                    index + 1,
                    null,
                    "GeoJSON coordinates must contain at least two numbers."));
            }
        }

        return results;
    }

    private static void ValidateFeature(JsonElement featureElement)
    {
        if (featureElement.ValueKind != JsonValueKind.Object
            || !featureElement.TryGetProperty("type", out var featureTypeElement)
            || featureTypeElement.ValueKind != JsonValueKind.String
            || !String.Equals(
                featureTypeElement.GetString(),
                "Feature",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("A GeoJSON FeatureCollection may only contain Features.");
        }

        if (featureElement.TryGetProperty("properties", out var properties)
            && properties.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
        {
            throw new FormatException("GeoJSON Feature properties must be an object or null.");
        }

        if (!featureElement.TryGetProperty("geometry", out var geometry)
            || geometry.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (geometry.ValueKind != JsonValueKind.Object
            || !geometry.TryGetProperty("type", out var geometryTypeElement)
            || geometryTypeElement.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("GeoJSON Feature geometry must contain a geometry type.");
        }

        var geometryType = geometryTypeElement.GetString();
        if (geometryType is not ("Point" or "MultiPoint" or "LineString"
            or "MultiLineString" or "Polygon" or "MultiPolygon"))
        {
            throw new FormatException($"Unsupported GeoJSON geometry type '{geometryType}'.");
        }

        if (!geometry.TryGetProperty("coordinates", out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("GeoJSON geometry coordinates must be an array.");
        }
    }

    private static WebMapping.Core.Feature ToFeature(GeoJsonFeature geoJsonFeature, int srefId)
    {
        if (!String.Equals(geoJsonFeature.Type, "Feature", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Each GeoJSON item must have type 'Feature'.");
        }

        var attributes = geoJsonFeature.PropertiesAsDict()
            .Select(property => new WebMapping.Core.Attribute(
                property.Key,
                ToAttributeValue(property.Value)))
            .ToArray();
        var feature = new WebMapping.Core.Feature(attributes);

        if (geoJsonFeature.Geometry is not null)
        {
            feature.Shape = String.Equals(
                    geoJsonFeature.Geometry.type,
                    "MultiPoint",
                    StringComparison.OrdinalIgnoreCase)
                ? ToMultiPoint(geoJsonFeature.Geometry.coordinates)
                : geoJsonFeature.ToShape();

            if (feature.Shape is null)
            {
                throw new FormatException(
                    $"Unsupported GeoJSON geometry type '{geoJsonFeature.Geometry.type}'.");
            }

            feature.Shape.SrsId = srefId;
        }

        return feature;
    }

    private static Shape TransformShape(Shape source, int targetSrefId, string functionName)
    {
        if (source.SrsId <= 0)
        {
            throw new ExpressionEvaluationException(
                $"Function '{functionName}' cannot transform a shape without a source SRefId.",
                0);
        }

        var sourceSref = SpatialReferences.ById(source.SrsId);
        if (sourceSref is null)
        {
            throw new ExpressionEvaluationException(
                $"Function '{functionName}' cannot resolve source SRefId {source.SrsId}.",
                0);
        }

        var targetSref = SpatialReferences.ById(targetSrefId);
        if (targetSref is null)
        {
            throw new ExpressionEvaluationException(
                $"Function '{functionName}' cannot resolve target SRefId {targetSrefId}.",
                0);
        }

        return source.TransformedCopy(
            targetSrefId,
            (_, _) => new GeometricTransformerPro(sourceSref, targetSref));
    }

    private static MultiPoint ToMultiPoint(object? coordinates)
    {
        if (coordinates is null)
        {
            throw new FormatException("GeoJSON MultiPoint geometry has no coordinates.");
        }

        var points = JSerializer.Deserialize<double[][]>(coordinates.ToString() ?? String.Empty)
            ?? throw new FormatException("GeoJSON MultiPoint coordinates are invalid.");

        return new MultiPoint(points.Select(point =>
        {
            if (point is null || point.Length < 2)
            {
                throw new FormatException("GeoJSON coordinates must contain at least two numbers.");
            }

            return new Point(point[0], point[1]);
        }));
    }

    private static string? ToAttributeValue(object? value)
        => value switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement { ValueKind: JsonValueKind.True } => "true",
            JsonElement { ValueKind: JsonValueKind.False } => "false",
            JsonElement element => element.GetRawText(),
            string text => text,
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => JSerializer.Serialize(value)
        };
}
