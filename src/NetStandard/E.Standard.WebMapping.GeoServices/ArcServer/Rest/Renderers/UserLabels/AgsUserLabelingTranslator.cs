#nullable enable

using System;
using System.Globalization;
using System.Linq;

using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Renderer.UserLabels;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers.UserLabels;

/// <summary>
/// Translates the service-neutral <see cref="UserLabeling"/> into ArcGIS Server REST labeling
/// (drawingInfo.labelingInfo of a dynamicLayer).
/// </summary>
public static class AgsUserLabelingTranslator
{
    public const float DefaultFontSize = 10f;
    public const string DefaultFontColor = "#000000";
    public const int HaloSize = 2;

    /// <summary>
    /// Translates the expression into the AGS (legacy) label expression syntax:
    /// "Fläche: [FLAECHE] m²" => "Fläche: " CONCAT [FLAECHE] CONCAT " m²"
    /// Returns null for an empty expression.
    /// </summary>
    /// <param name="encodeString">
    /// Optional encoding for user defined strings.
    /// Needed, if the json is not url encoded as a whole in the request body.
    /// </param>
    public static string? ToAgsLabelExpression(string? expression, Func<string, string>? encodeString = null)
    {
        encodeString ??= s => s;

        var parts = UserLabelExpression.Parse(expression)
            .Select(t => t.Type switch
            {
                UserLabelExpressionTokenType.Field => $"[{encodeString(t.Value)}]",
                UserLabelExpressionTokenType.NewLine => "NEWLINE",
                _ => $"\"{encodeString(t.Value)}\""
            })
            .ToArray();

        return parts.Length == 0
            ? null
            : String.Join(" CONCAT ", parts);
    }

    /// <summary>
    /// Returns the AGS labelingInfo or null, if the labeling has no custom labels (mode none)
    /// or can't be translated. The labeling should be validated before.
    /// </summary>
    public static LabelingInfo? ToAgsLabelingInfo(UserLabeling? labeling,
                                                  LayerType geometryType,
                                                  double minScale,
                                                  double maxScale,
                                                  string? where,
                                                  Func<string, string>? encodeString = null)
    {
        if (labeling is null || labeling.Mode != UserLabeling.ModeCustom)
        {
            return null;
        }

        var placement = LabelingInfo.DefaultLabelPlacement(geometryType);
        var expression = ToAgsLabelExpression(labeling.Expression, encodeString);

        if (String.IsNullOrEmpty(placement) || String.IsNullOrEmpty(expression))
        {
            return null;
        }

        return new LabelingInfo()
        {
            LabelPlacement = placement,
            LabelExpression = expression,
            UseCodedValues = true,
            MinScale = (int)maxScale,  // AGS minScale: the smallest scale (largest denominator)
            MaxScale = (int)minScale,
            Where = String.IsNullOrWhiteSpace(where) ? null : where,
            Symbol = ToAgsTextSymbol(labeling)
        };
    }

    /// <summary>
    /// Show the labels of the dynamicLayer?
    /// null => labeling of the service is not changed
    /// </summary>
    public static bool? ShowLabels(UserLabeling? labeling)
        => labeling?.Mode switch
        {
            UserLabeling.ModeNone => false,
            UserLabeling.ModeCustom => true,
            _ => null
        };

    #region Helper

    internal static TextSymbol ToAgsTextSymbol(UserLabeling labeling)
    {
        var style = labeling.FontStyle ?? UserLabeling.FontStyleRegular;

        var symbol = new TextSymbol()
        {
            Color = ToAgsColor(labeling.FontColor ?? DefaultFontColor),
            VerticalAlignment = "bottom",
            HorizontalAlignment = "center",
            Kerning = true,
            Font = new Font()
            {
                Family = SystemInfo.DefaultFontName,
                Size = (int)Math.Round(labeling.FontSize ?? DefaultFontSize),
                Style = style is UserLabeling.FontStyleItalic or UserLabeling.FontStyleBoldItalic ? "italic" : "normal",
                Weight = style is UserLabeling.FontStyleBold or UserLabeling.FontStyleBoldItalic ? "bold" : "normal",
                Decoration = "none"
            }
        };

        if (!String.IsNullOrEmpty(labeling.HaloColor))
        {
            symbol.HaloColor = ToAgsColor(labeling.HaloColor!);
            symbol.HaloSize = HaloSize;
        }

        return symbol;
    }

    internal static int[] ToAgsColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6 ||
            !int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) ||
            !int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) ||
            !int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
        {
            return [0, 0, 0, 255];
        }

        return [r, g, b, 255];
    }

    #endregion
}
