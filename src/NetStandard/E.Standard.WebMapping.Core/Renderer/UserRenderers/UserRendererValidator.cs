#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Renderer.UserRenderers;

public class UserRendererValidator
{
    public const int DefaultMaxUniqueValueClasses = 100;

    public const float MinSize = 1f, MaxSize = 64f;
    public const float MinWidth = 0f, MaxWidth = 20f;
    public const int MaxTextLength = 256;

    private static readonly Regex HexColorRegex = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly int _maxUniqueValueClasses;

    public UserRendererValidator()
        : this(UserRendererSettings.MaxUniqueValueClasses)
    {
    }

    public UserRendererValidator(int maxUniqueValueClasses)
    {
        _maxUniqueValueClasses = maxUniqueValueClasses > 0
            ? maxUniqueValueClasses
            : DefaultMaxUniqueValueClasses;
    }

    /// <summary>
    /// Validates a user renderer for a layer.
    /// </summary>
    /// <param name="renderer">The renderer to validate</param>
    /// <param name="geometryType">Geometry type of the layer (point, line, polygon)</param>
    /// <param name="allowedFields">Fields allowed for the unique value renderer (QueryBuilder fields)</param>
    /// <returns>A list of errors. Empty list => valid</returns>
    public IReadOnlyList<string> Validate(UserRenderer? renderer, LayerType geometryType, IEnumerable<string>? allowedFields)
    {
        var errors = new List<string>();

        if (renderer is null)
        {
            errors.Add("renderer is null");
            return errors;
        }

        if (renderer.Version != UserRenderer.CurrentVersion)
        {
            errors.Add($"unsupported renderer version {renderer.Version}");
        }

        if (geometryType != LayerType.point &&
            geometryType != LayerType.line &&
            geometryType != LayerType.polygon)
        {
            errors.Add($"geometry type {geometryType} is not supported");
            return errors;
        }

        switch (renderer.Type)
        {
            case UserRenderer.TypeSimple:
                ValidateSymbol(renderer.Symbol, geometryType, "symbol", errors);
                break;
            case UserRenderer.TypeUniqueValue:
                ValidateUniqueValueRenderer(renderer, geometryType, allowedFields, errors);
                break;
            default:
                errors.Add($"unsupported renderer type '{renderer.Type}'");
                break;
        }

        return errors;
    }

    public bool IsValid(UserRenderer? renderer, LayerType geometryType, IEnumerable<string>? allowedFields)
        => Validate(renderer, geometryType, allowedFields).Count == 0;

    #region Helper

    private void ValidateUniqueValueRenderer(UserRenderer renderer, LayerType geometryType, IEnumerable<string>? allowedFields, List<string> errors)
    {
        if (String.IsNullOrWhiteSpace(renderer.Field))
        {
            errors.Add("field is required");
        }
        else if (allowedFields?.Any(f => f.Equals(renderer.Field, StringComparison.OrdinalIgnoreCase)) != true)
        {
            errors.Add($"field '{renderer.Field}' is not allowed");
        }

        var items = renderer.Items ?? [];
        if (items.Count == 0 && renderer.DefaultSymbol is null)
        {
            errors.Add("unique value renderer needs at least one item or a default symbol");
        }

        if (items.Count > _maxUniqueValueClasses)
        {
            errors.Add($"too many classes ({items.Count}), max: {_maxUniqueValueClasses}");
        }

        var values = new HashSet<string>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item is null)
            {
                errors.Add($"items[{i}] is null");
                continue;
            }

            if (item.Value is null || item.Value.Length > MaxTextLength)
            {
                errors.Add($"items[{i}].value is invalid");
            }
            else if (!values.Add(item.Value))
            {
                errors.Add($"items[{i}].value '{item.Value}' is not unique");
            }

            ValidateText(item.Label, $"items[{i}].label", errors);
            ValidateSymbol(item.Symbol, geometryType, $"items[{i}].symbol", errors);
        }

        if (renderer.DefaultSymbol is not null)
        {
            ValidateSymbol(renderer.DefaultSymbol, geometryType, "defaultSymbol", errors);
        }
        ValidateText(renderer.DefaultLabel, "defaultLabel", errors);
    }

    private static void ValidateSymbol(UserSymbol? symbol, LayerType geometryType, string name, List<string> errors)
    {
        if (symbol is null)
        {
            errors.Add($"{name} is required");
            return;
        }

        ValidateColor(symbol.Color, $"{name}.color", required: true, errors);

        switch (geometryType)
        {
            case LayerType.point:
                ValidateOneOf(symbol.Marker, UserSymbol.Markers, $"{name}.marker", errors);
                ValidateRange(symbol.Size, MinSize, MaxSize, $"{name}.size", errors);
                ValidateOpacity(symbol.Opacity, $"{name}.opacity", errors);
                ValidateColor(symbol.OutlineColor, $"{name}.outlineColor", required: false, errors);
                ValidateRange(symbol.OutlineWidth, MinWidth, MaxWidth, $"{name}.outlineWidth", errors);
                break;
            case LayerType.line:
                ValidateRange(symbol.Width, MinWidth, MaxWidth, $"{name}.width", errors);
                ValidateOneOf(symbol.LineStyle, UserSymbol.LineStyles, $"{name}.lineStyle", errors);
                break;
            case LayerType.polygon:
                ValidateOpacity(symbol.Opacity, $"{name}.opacity", errors);
                ValidateOneOf(symbol.FillStyle, UserSymbol.FillStyles, $"{name}.fillStyle", errors);
                ValidateColor(symbol.OutlineColor, $"{name}.outlineColor", required: false, errors);
                ValidateRange(symbol.OutlineWidth, MinWidth, MaxWidth, $"{name}.outlineWidth", errors);
                ValidateOneOf(symbol.OutlineStyle, UserSymbol.LineStyles, $"{name}.outlineStyle", errors);
                break;
        }
    }

    private static void ValidateColor(string? color, string name, bool required, List<string> errors)
    {
        if (color is null)
        {
            if (required)
            {
                errors.Add($"{name} is required");
            }
            return;
        }

        if (!HexColorRegex.IsMatch(color))
        {
            errors.Add($"{name} '{color}' is not a valid color (#rrggbb)");
        }
    }

    private static void ValidateRange(float? value, float min, float max, string name, List<string> errors)
    {
        if (value.HasValue && (float.IsNaN(value.Value) || value.Value < min || value.Value > max))
        {
            errors.Add($"{name} must be between {min} and {max}");
        }
    }

    private static void ValidateOpacity(float? value, string name, List<string> errors)
        => ValidateRange(value, 0f, 1f, name, errors);

    private static void ValidateOneOf(string? value, string[] allowed, string name, List<string> errors)
    {
        if (value is not null && !allowed.Contains(value))
        {
            errors.Add($"{name} '{value}' is not supported");
        }
    }

    private static void ValidateText(string? text, string name, List<string> errors)
    {
        if (text is not null && text.Length > MaxTextLength)
        {
            errors.Add($"{name} is too long (max {MaxTextLength})");
        }
    }

    #endregion
}
