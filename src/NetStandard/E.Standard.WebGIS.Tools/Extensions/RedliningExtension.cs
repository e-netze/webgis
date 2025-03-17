using E.Standard.GeoJson;
using E.Standard.Platform;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Extensions;

static internal class RedliningExtension
{
    static public string ToValidHexColor(this string hex, string defaultValue)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(hex) || hex.ToLower().Trim() == "none")
            {
                return defaultValue;
            }

            if (!hex.StartsWith("#"))
            {
                hex = $"#{hex}";
            }

            var color = ArgbColor.FromHexString(hex);

            return hex;
        }
        catch
        {
            return defaultValue;
        }
    }

    static public float ToValidOpacity(this string number, float defaultValue)
    {
        try
        {
            if (String.IsNullOrEmpty(number))
            {
                return defaultValue;
            }

            float value = number.ToPlatformFloat();

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1)
            {
                value /= 255f;
            }

            return Math.Min(1f, value);
        }
        catch
        {
            return defaultValue;
        }
    }

    static public IDictionary<string, object> DefaultProperties(this GeoJsonFeature geoJsonFeature)
        => geoJsonFeature.Geometry?.type?.ToLowerInvariant() switch
        {
            "point" => new Dictionary<string, object>()
            {
                { "point-color", "#ff0000" },
                { "point-size", 10 },
                { "_meta", new Dictionary<string, object>()
                    {
                        { "tool", "point" },
                        { "text", geoJsonFeature.DefaultTextFromProperties() }
                    }
                }
            },
            "line" or "multiline" => new Dictionary<string, object>()
            {
                { "stroke", "#ff0000" },
                { "stroke-opacity", 0.8 },
                { "stroke-width", 2 },
                { "stroke-style", "1" },
                { "_meta", new Dictionary<string, object>()
                    {
                        { "tool", "line" },
                        { "text", geoJsonFeature.DefaultTextFromProperties() }
                    }
                }
            },
            "polygon" or "multipolygon" => new Dictionary<string, object>()
            {
                { "stroke", "#ff0000" },
                { "stroke-opacity", 0.8 },
                { "stroke-width", 2 },
                { "stroke-style", "1" },
                { "fill", "#ffff00" },
                { "fill-opacity", 0.2 },
                { "_meta", new Dictionary<string, object>()
                    {
                        { "tool", "polygon" },
                        { "text", geoJsonFeature.DefaultTextFromProperties() }
                    }
                }
            },
            _ => geoJsonFeature.PropertiesAsDict()
        };

    static private string DefaultTextFromProperties(this GeoJsonFeature geoJsonFeature, int take = 3)
        => string.Join(", ", geoJsonFeature?.PropertiesAsDict()?.ToArray()
                                            .Take(take)
                                            .Select(kv => $"{kv.Key}: {kv.Value}"));
}
