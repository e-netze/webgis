#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Renderer.UserLabels;

public class UserLabelingValidator
{
    public const int MaxExpressionLength = 200;
    public const int MaxExpressionLines = 3;
    public const float MinFontSize = 6f, MaxFontSize = 48f;

    private static readonly Regex HexColorRegex = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a user labeling for a layer.
    /// </summary>
    /// <param name="labeling">The labeling to validate</param>
    /// <param name="geometryType">Geometry type of the layer (point, line, polygon)</param>
    /// <param name="allowedFields">Fields allowed in the expression (QueryBuilder fields)</param>
    /// <returns>A list of errors. Empty list => valid</returns>
    public IReadOnlyList<string> Validate(UserLabeling? labeling, LayerType geometryType, IEnumerable<string>? allowedFields)
    {
        var errors = new List<string>();

        if (labeling is null)
        {
            errors.Add("labeling is null");
            return errors;
        }

        if (labeling.Version != UserLabeling.CurrentVersion)
        {
            errors.Add($"unsupported labeling version {labeling.Version}");
        }

        if (geometryType != LayerType.point &&
            geometryType != LayerType.line &&
            geometryType != LayerType.polygon)
        {
            errors.Add($"geometry type {geometryType} is not supported");
            return errors;
        }

        switch (labeling.Mode)
        {
            case UserLabeling.ModeNone:
                break;
            case UserLabeling.ModeCustom:
                ValidateCustom(labeling, allowedFields, errors);
                break;
            default:
                errors.Add($"unsupported labeling mode '{labeling.Mode}'");
                break;
        }

        return errors;
    }

    public bool IsValid(UserLabeling? labeling, LayerType geometryType, IEnumerable<string>? allowedFields)
        => Validate(labeling, geometryType, allowedFields).Count == 0;

    #region Helper

    private static void ValidateCustom(UserLabeling labeling, IEnumerable<string>? allowedFields, List<string> errors)
    {
        var tokens = UserLabelExpression.Parse(labeling.Expression);

        if (String.IsNullOrWhiteSpace(labeling.Expression) ||
            !tokens.Any(t => t.Type != UserLabelExpressionTokenType.NewLine && !String.IsNullOrWhiteSpace(t.Value)))
        {
            errors.Add("expression is required");
        }
        else
        {
            if (labeling.Expression!.Length > MaxExpressionLength)
            {
                errors.Add($"expression exceeds {MaxExpressionLength} characters");
            }
            if (UserLabelExpression.LineCount(labeling.Expression) > MaxExpressionLines)
            {
                errors.Add($"expression exceeds {MaxExpressionLines} lines");
            }

            foreach (var field in UserLabelExpression.Fields(labeling.Expression))
            {
                if (allowedFields?.Any(f => f.Equals(field, StringComparison.OrdinalIgnoreCase)) != true)
                {
                    errors.Add($"field '{field}' is not allowed");
                }
            }
        }

        if (labeling.FontSize.HasValue &&
            (float.IsNaN(labeling.FontSize.Value) || labeling.FontSize < MinFontSize || labeling.FontSize > MaxFontSize))
        {
            errors.Add($"fontSize must be between {MinFontSize} and {MaxFontSize}");
        }

        if (labeling.FontStyle is not null && !UserLabeling.FontStyles.Contains(labeling.FontStyle))
        {
            errors.Add($"unsupported fontStyle '{labeling.FontStyle}'");
        }

        ValidateColor(labeling.FontColor, "fontColor", errors);
        ValidateColor(labeling.HaloColor, "haloColor", errors);
    }

    private static void ValidateColor(string? color, string name, List<string> errors)
    {
        if (color is not null && !HexColorRegex.IsMatch(color))
        {
            errors.Add($"{name} must be a color like #rrggbb");
        }
    }

    #endregion
}
