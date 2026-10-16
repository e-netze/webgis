#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Renderer.UserRenderers;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers.UserRenderers;

/// <summary>
/// Translates the service-neutral <see cref="UserRenderer"/> into an ArcGIS Server REST renderer
/// (drawingInfo.renderer of a dynamicLayer). The result is a plain dictionary tree that can be
/// serialized with Newtonsoft and System.Text.Json.
/// Sizes and widths are in pt (same unit as AGS symbols).
/// </summary>
public static class AgsUserRendererTranslator
{
    public const string DefaultColor = "#808080";
    public const string DefaultOutlineColor = "#000000";
    public const float DefaultMarkerSize = 8f;
    public const float DefaultLineWidth = 1f;
    public const float DefaultOutlineWidth = 1f;

    /// <summary>
    /// Returns the AGS renderer object or null, if the renderer can't be translated
    /// (unsupported type or geometry type). The renderer should be validated before.
    /// </summary>
    /// <param name="encodeString">
    /// Optional encoding for user defined strings (field, values, labels).
    /// Needed, if the renderer json is not url encoded as a whole in the request body.
    /// </param>
    public static Dictionary<string, object>? ToAgsRenderer(UserRenderer? renderer, LayerType geometryType, Func<string, string>? encodeString = null)
    {
        if (renderer is null || !IsSupportedGeometryType(geometryType))
        {
            return null;
        }

        encodeString ??= s => s;

        if (renderer.Type == UserRenderer.TypeSimple)
        {
            return new Dictionary<string, object>()
            {
                ["type"] = "simple",
                ["symbol"] = ToAgsSymbol(renderer.Symbol, geometryType)
            };
        }

        if (renderer.Type == UserRenderer.TypeUniqueValue && !String.IsNullOrEmpty(renderer.Field))
        {
            var agsRenderer = new Dictionary<string, object>()
            {
                ["type"] = "uniqueValue",
                ["field1"] = encodeString(renderer.Field!),
                ["uniqueValueInfos"] = (renderer.Items ?? [])
                    .Select(item => new Dictionary<string, object>()
                    {
                        ["value"] = encodeString(item.Value ?? ""),
                        ["label"] = encodeString(String.IsNullOrEmpty(item.Label) ? item.Value ?? "" : item.Label!),
                        ["symbol"] = ToAgsSymbol(item.Symbol, geometryType)
                    })
                    .ToArray()
            };

            // no default symbol => other values are not drawn
            if (renderer.DefaultSymbol is not null)
            {
                agsRenderer["defaultSymbol"] = ToAgsSymbol(renderer.DefaultSymbol, geometryType);
                agsRenderer["defaultLabel"] = encodeString(renderer.DefaultLabel ?? "");
            }

            return agsRenderer;
        }

        if (renderer.Type == UserRenderer.TypeClassBreaks && !String.IsNullOrEmpty(renderer.Field))
        {
            var classBreaks = renderer.ClassBreaks ?? [];
            double minValue = renderer.MinValue ?? (classBreaks.Count > 0 ? classBreaks[0].MaxValue : 0d);

            var agsRenderer = new Dictionary<string, object>()
            {
                ["type"] = "classBreaks",
                ["field"] = encodeString(renderer.Field!),
                ["minValue"] = minValue,
                ["classBreakInfos"] = classBreaks
                    .Select((classBreak, index) =>
                    {
                        double classMinValue = index == 0 ? minValue : classBreaks[index - 1].MaxValue;
                        string label = String.IsNullOrEmpty(classBreak.Label)
                            ? $"{FormatNumber(classMinValue)} - {FormatNumber(classBreak.MaxValue)}"
                            : classBreak.Label!;

                        return new Dictionary<string, object>()
                        {
                            ["classMinValue"] = classMinValue,
                            ["classMaxValue"] = classBreak.MaxValue,
                            ["label"] = encodeString(label),
                            ["symbol"] = ToAgsSymbol(classBreak.Symbol, geometryType)
                        };
                    })
                    .ToArray()
            };

            // no default symbol => values outside the classes are not drawn
            if (renderer.DefaultSymbol is not null)
            {
                agsRenderer["defaultSymbol"] = ToAgsSymbol(renderer.DefaultSymbol, geometryType);
                agsRenderer["defaultLabel"] = encodeString(renderer.DefaultLabel ?? "");
            }

            return agsRenderer;
        }

        return null;
    }

    public static Dictionary<string, object> ToAgsSymbol(UserSymbol? symbol, LayerType geometryType)
    {
        symbol ??= new UserSymbol();

        return geometryType switch
        {
            LayerType.point => new Dictionary<string, object>()
            {
                ["type"] = "esriSMS",
                ["style"] = MarkerStyle(symbol.Marker),
                ["color"] = ToAgsColor(symbol.Color ?? DefaultColor, symbol.Opacity ?? 1f),
                ["size"] = symbol.Size ?? DefaultMarkerSize,
                ["angle"] = 0,
                ["xoffset"] = 0,
                ["yoffset"] = 0,
                ["outline"] = new Dictionary<string, object>()
                {
                    ["color"] = ToAgsColor(symbol.OutlineColor ?? DefaultOutlineColor, 1f),
                    ["width"] = symbol.OutlineWidth ?? DefaultOutlineWidth
                }
            },
            LayerType.line => new Dictionary<string, object>()
            {
                ["type"] = "esriSLS",
                ["style"] = LineStyle(symbol.LineStyle),
                ["color"] = ToAgsColor(symbol.Color ?? DefaultColor, 1f),
                ["width"] = symbol.Width ?? DefaultLineWidth
            },
            LayerType.polygon => new Dictionary<string, object>()
            {
                ["type"] = "esriSFS",
                ["style"] = FillStyle(symbol.FillStyle),
                ["color"] = ToAgsColor(symbol.Color ?? DefaultColor, symbol.Opacity ?? 1f),
                ["outline"] = new Dictionary<string, object>()
                {
                    ["type"] = "esriSLS",
                    ["style"] = (symbol.OutlineWidth ?? DefaultOutlineWidth) <= 0f
                        ? "esriSLSNull"
                        : LineStyle(symbol.OutlineStyle),
                    ["color"] = ToAgsColor(symbol.OutlineColor ?? DefaultOutlineColor, 1f),
                    ["width"] = symbol.OutlineWidth ?? DefaultOutlineWidth
                }
            },
            _ => throw new ArgumentException($"geometry type {geometryType} is not supported")
        };
    }

    #region Helper

    private static string FormatNumber(double value)
        => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static bool IsSupportedGeometryType(LayerType geometryType)
        => geometryType == LayerType.point ||
           geometryType == LayerType.line ||
           geometryType == LayerType.polygon;

    public static int[] ToAgsColor(string? hexColor, float opacity)
    {
        int r = 128, g = 128, b = 128;

        if (hexColor?.Length == 7 && hexColor.StartsWith("#") &&
            Int32.TryParse(hexColor.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            r = (rgb >> 16) & 0xff;
            g = (rgb >> 8) & 0xff;
            b = rgb & 0xff;
        }

        int a = (int)Math.Round(Math.Clamp(opacity, 0f, 1f) * 255f);

        return [r, g, b, a];
    }

    private static string MarkerStyle(string? marker)
        => marker?.ToLowerInvariant() switch
        {
            "square" => "esriSMSSquare",
            "diamond" => "esriSMSDiamond",
            "triangle" => "esriSMSTriangle",
            "cross" => "esriSMSCross",
            "x" => "esriSMSX",
            _ => "esriSMSCircle"
        };

    private static string LineStyle(string? lineStyle)
        => lineStyle?.ToLowerInvariant() switch
        {
            "dash" => "esriSLSDash",
            "dot" => "esriSLSDot",
            "dashdot" => "esriSLSDashDot",
            _ => "esriSLSSolid"
        };

    private static string FillStyle(string? fillStyle)
        => fillStyle?.ToLowerInvariant() switch
        {
            "none" => "esriSFSNull",
            "horizontal" => "esriSFSHorizontal",
            "vertical" => "esriSFSVertical",
            "cross" => "esriSFSCross",
            "forwarddiagonal" => "esriSFSForwardDiagonal",
            "backwarddiagonal" => "esriSFSBackwardDiagonal",
            "diagonalcross" => "esriSFSDiagonalCross",
            _ => "esriSFSSolid"
        };

    #endregion
}
