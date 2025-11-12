using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Security;
using E.Standard.GeoJson;
using E.Standard.Gpx;
using E.Standard.Gpx.Schema;
using E.Standard.Json;
using E.Standard.OGC.Schema;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebGIS.Tools.Extensions;

public static class ApiToolEventFileExtensions
{
    static public GeoJsonFeatures GetFeatures(
                    this ApiToolEventArguments.ApiToolEventFile file,
                    ApiToolEventArguments e,
                    bool coordinatesToDoubleArray = false,
                    bool setNameProperty = false)
    {
        GeoJsonFeatures geoJsonFeatures = null;

        if (file != null)
        {
            if (file.FileName.ToLower().EndsWith(".gpx"))
            {
                geoJsonFeatures = ParseGpx(e, Encoding.UTF8.GetString(file.Data), setNameProperty);
            }
            else if (file.FileName.ToLower().EndsWith(".json"))
            {
                geoJsonFeatures = ParseGeoJson(e, Encoding.UTF8.GetString(file.Data), coordinatesToDoubleArray, setNameProperty);
            }
            else
            {
                throw new Exception($"Ungültige Dateiendung: {(file.FileName.Contains(".") ? file.FileName.Substring(file.FileName.LastIndexOf(".")) : "unknown")}");
            }
        }

        return geoJsonFeatures;
    }

    static public double IdentifyTolerance(this ApiToolEventArguments e, IQueryBridge query)
    {
        double pixelTolerance = e.GetConfigDouble("tolerance", 15.0);

        //string queryGlobalId = query.QueryGlobalId.Replace(":", "@"),
        //       queryId = query.Id;

        //if (e.HasConfigValue($"tolerance-for-{ queryGlobalId }"))
        //{
        //    pixelTolerance = e.GetConfigDouble($"tolerance-for-{ queryGlobalId }", pixelTolerance);
        //} 
        //else if(e.HasConfigValue($"tolerance-for-{ queryId }"))
        //{
        //    pixelTolerance = e.GetConfigDouble($"tolerance-for-{ queryId }", pixelTolerance);
        //}
        //else
        {
            switch (query.GetLayerType())
            {
                case LayerType.point:
                    pixelTolerance = e.GetConfigDouble("tolerance-for-point-layers", pixelTolerance);
                    break;
                case LayerType.line:
                    pixelTolerance = e.GetConfigDouble("tolerance-for-line-layers", pixelTolerance);
                    break;
                case LayerType.polygon:
                    pixelTolerance = e.GetConfigDouble("tolerance-for-polygon-layers", pixelTolerance);
                    break;
                default:
                    break;
            }
        }

        return pixelTolerance;
    }

    #region Helper

    static public GeoJsonFeatures ParseGpx(ApiToolEventArguments e, string gpxXml, bool setNameProperty)
    {
        gpxType gpx = null;
        Serializer<gpxType> ser = new Serializer<gpxType>();

        try
        {
            gpxXml = gpxXml.Trim();
            gpx = ser.FromString(gpxXml, Encoding.UTF8);
        }
        catch
        {
            gpxXml = gpxXml.Replace("http://www.topografix.com/GPX/1/0", "http://www.topografix.com/GPX/1/1");
            gpx = ser.FromString(gpxXml, Encoding.UTF8);
        }

        List<GeoJsonFeature> features = new List<GeoJsonFeature>();

        if (gpx.wpt != null && gpx.wpt.Length > 0)
        {
            PointCollection pColl = GpxHelper.ToPointCollection(gpx, "wpt");
            for (int i = 0; i < pColl.PointCount; i++)
            {
                PointM point = (PointM)pColl[i];
                string wayPointName = point.M?.ToString();

                var feature = new GeoJsonFeature();
                feature.FromShape(point);
                feature.Properties = DefaultSymbolJsonProperties(e, point.M?.ToString(), "upload-gpx", setNameProperty);

                features.Add(feature);
            }
        }

        if (gpx.rte != null && gpx.rte.Length > 0)
        {
            for (int i = 0; i < gpx.rte.Length; i++)
            {
                PointCollection pColl = GpxHelper.ToPointCollection(gpx, "rte:" + i);
                Polyline pLine = new Polyline(new Path());
                for (int p = 0; p < pColl.PointCount; p++)
                {
                    pLine[0].AddPoint(pColl[p]);
                }

                var feature = new GeoJsonFeature();

                feature.FromShape(pLine);
                feature.Properties = DefaultLineGeoJsonProperties(e, gpx.rte[i].name, "upload-gpx", setNameProperty);

                features.Add(feature);
            }
        }

        if (gpx.trk != null && gpx.trk.Length > 0)
        {
            for (int i = 0; i < gpx.trk.Length; i++)
            {
                for (int j = 0; j < gpx.trk[i].trkseg.Length; j++)
                {
                    PointCollection pColl = GpxHelper.ToPointCollection(gpx, $"trk:{i}:{j}");
                    Polyline pLine = new Polyline(new Path());
                    for (int p = 0; p < pColl.PointCount; p++)
                    {
                        pLine[0].AddPoint(pColl[p]);
                    }

                    var feature = new GeoJsonFeature();

                    feature.FromShape(pLine);
                    feature.Properties = DefaultLineGeoJsonProperties(e, gpx.trk[i].name, "upload-gpx", setNameProperty);

                    features.Add(feature);
                }
            }
        }

        return new GeoJsonFeatures()
        {
            Features = features.ToArray()
        };
    }

    static public GeoJsonFeatures ParseGeoJson(ApiToolEventArguments e, string geoJsonFeaturesString, bool coordinatesToDoubleArray, bool setNameProperty)
    {
        var geoJsonFeatures = JSerializer.Deserialize<GeoJsonFeatures>(geoJsonFeaturesString);

        if (geoJsonFeatures?.Features != null)
        {
            Dictionary<string, int> geometryTypeCounter = new Dictionary<string, int>();

            foreach (var geoJsonFeature in geoJsonFeatures.Features)
            {
                if (setNameProperty == true)
                {
                    if (!geometryTypeCounter.ContainsKey(geoJsonFeature.Geometry?.type))
                    {
                        geometryTypeCounter[geoJsonFeature.Geometry?.type] = 1;
                    }
                    else
                    {
                        geometryTypeCounter[geoJsonFeature.Geometry?.type] = geometryTypeCounter[geoJsonFeature.Geometry?.type] + 1;
                    }

                    geoJsonFeature.PropertiesToDict();
                    geoJsonFeature.SetProperty(
                        "name",
                        (geoJsonFeature.GetFirstOrDefault(new string[] { "TEXT", "Text", "text", "NAME", "Name", "name", "ID", "Id", "id" })?.ToString() ?? String.Empty).OrTake($"{geoJsonFeature.Geometry?.type} {geometryTypeCounter[geoJsonFeature.Geometry?.type]}"));
                }

                if (coordinatesToDoubleArray && geoJsonFeature.Geometry != null)
                {
                    geoJsonFeature.Geometry.coordinates = ToDoubleArray(geoJsonFeature.Geometry.coordinates);
                }

                if (geoJsonFeature.Geometry.type?.Contains("line", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    if (!geoJsonFeature.HasProperty("stroke"))
                    {
                        geoJsonFeature.TrySetProperty("stroke",
                            geoJsonFeature.GetFirstOrDefault(new string[] { "COLOR", "Color", "color", "BORDERCOL", "BorderCol", "bordercol", "BORDERCOLOR", "BorderColor", "bordercolor" })?.ToString().ToValidHexColor("#ff0000"),
                            true);
                    }

                    if (!geoJsonFeature.HasProperty("stroke-width"))
                    {
                        geoJsonFeature.TrySetProperty(
                            "stroke-width",
                            geoJsonFeature.GetFirstOrDefault(new string[] { "WIDTH", "Width", "width" }),
                            true);
                    }
                }

                if (geoJsonFeature.Geometry.type?.Contains("polygon", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    if (!geoJsonFeature.HasProperty("fill"))
                    {
                        geoJsonFeature.TrySetProperty(
                            "fill",
                            geoJsonFeature.GetFirstOrDefault(new string[] { "FILL", "Fill", "FILLCOL", "FillCol", "fillcol", "FILLCOLOR", "FillColor", "fillcolor" })?.ToString().ToValidHexColor("#ffff00"),
                            true);
                    }

                    if (!geoJsonFeature.HasProperty("fill-opacity"))
                    {
                        geoJsonFeature.TrySetProperty(
                            "fill-opacity",
                            geoJsonFeature.GetFirstOrDefault(new string[] { "TRANSP", "Transp", "transp", "OPACITY", "Opacity", "opacity" })?.ToString().ToValidOpacity(0.7f),
                            true);
                    }
                }

                if ("point".Equals(geoJsonFeature.Geometry.type, StringComparison.InvariantCultureIgnoreCase))
                {
                    string tool = geoJsonFeature.GetValue("_meta.tool")?.ToString();

                    if (String.IsNullOrEmpty(tool)
                        || "symbol".Equals(tool, StringComparison.InvariantCultureIgnoreCase))
                    {

                        if (!geoJsonFeature.HasProperty("symbol"))
                        {
                            geoJsonFeature.TrySetProperty("symbol", "graphics/markers/hotspot0.gif".ReplaceLegacySymbols(), false);
                        }


                        string symbol = geoJsonFeature.GetPropery<string>("symbol")?.ToString() ?? "";
                        if (symbol != symbol.ReplaceLegacySymbols())
                        {
                            geoJsonFeature.SetProperty("symbol", symbol.ReplaceLegacySymbols());
                        }
                        else if (symbol.IsUrlOrContainsBacks())
                        {
                            //
                            // absolute Urls are not allowed!!
                            // to avoid CSRF (Cross-Site Request Forgery)
                            // => this url is send back to the client and client tries to call this url!
                            //
                            geoJsonFeature.SetProperty("symbol", "graphics/markers/hotspot0.gif".ReplaceLegacySymbols());
                        }

                        var metaSymbolId = GetValue(geoJsonFeature.Properties, "_meta.symbol.id")?.ToString() ?? "";
                        if (metaSymbolId != metaSymbolId.ReplaceLegacySymbols())
                        {
                            geoJsonFeature.SetProperty("_meta.symbol.id", metaSymbolId.ReplaceLegacySymbols());
                        }
                        else if (metaSymbolId.IsUrlOrContainsBacks() == true)
                        {
                            geoJsonFeature.SetProperty("_meta.symbol.id", "graphics/markers/hotspot0.gif".ReplaceLegacySymbols());
                        }

                        var metaSymbolIcon = GetValue(geoJsonFeature.Properties, "_meta.symbol.icon")?.ToString() ?? "";
                        if (metaSymbolIcon != metaSymbolIcon.ReplaceLegacySymbols())
                        {
                            geoJsonFeature.SetProperty("_meta.symbol.icon", metaSymbolIcon.ReplaceLegacySymbols());
                        }
                        else if (metaSymbolIcon.IsUrlOrContainsBacks() == true)
                        {
                            geoJsonFeature.SetProperty("_meta.symbol.icon", "graphics/markers/hotspot0.gif".ReplaceLegacySymbols());
                        }


                    }
                }

                if (!geoJsonFeature.HasProperty("_meta"))  // Wenns kein Meta gibt => Text trotzdem propieren zu setzen
                {
                    geoJsonFeature.TrySetProperty("__metaText", geoJsonFeature.GetFirstOrDefault(new string[] { "TEXT", "Text", "text", "NAME", "Name", "name", "ID", "Id", "id" })?.ToString(), true);
                }
            }
        }

        return geoJsonFeatures;
    }

    static private string ReplaceLegacySymbols(this string symbol)
    => symbol switch
    {
        "graphics/markers/hotspot0.gif" => "graphics/markers/hotspot03.gif",
        "graphics/markers/hotspot1.gif" => "graphics/markers/hotspot04.gif",
        "graphics/markers/hotspot2.gif" => "graphics/markers/hotspot05.gif",
        "graphics/markers/hotspot3.gif" => "graphics/markers/hotspot06.png",
        "graphics/markers/hotspot4.gif" => "graphics/markers/hotspot07.png",
        "graphics/markers/hotspot5.gif" => "graphics/markers/hotspot08.png",
        "graphics/markers/hotspot6.gif" => "graphics/markers/hotspot02.gif",
        "graphics/markers/marker_red.gif" => "graphics/markers/hotspot01.gif",
        "graphics/markers/pin1.png" => "graphics/markers/hotspot00.png",
        _ => symbol
    };

    static public object DefaultPointGeoJsonProperties(this ApiToolEventArguments e, string text, string source, bool setNameProperty = false)
    {
        var dict = new Dictionary<string, object>()
                            {
                                { "point-color", e?.GetString("mapmarkup-pointcolor") ?? "#ff0000" },
                                { "point-size", e?.GetDouble("mapmarkup-pointsize") ?? 10 },
                                { "_meta",  new
                                        {
                                            tool = "point",
                                            text = text ?? String.Empty,
                                            source = source
                                        }
                                }
                            };

        if (setNameProperty)
        {
            dict.Add("name", text);
        }

        return dict;
    }

    static public object DefaultLineGeoJsonProperties(this ApiToolEventArguments e, string text, string source, bool setNameProperty = false)
    {
        var dict = new Dictionary<string, object>()
                            {
                                { "stroke", e?.GetString("mapmarkup-color") ?? "#ff0000" },
                                { "stroke-opacity", e?.GetDouble("mapmarkup-opacity") ?? 0.8},
                                { "stroke-width", e?.GetInt("mapmarkup-lineweight") ?? 3 },
                                { "stroke-style", e?.GetString("mapmarkup-linestyle") ?? "1" },
                                { "_meta",  new
                                        {
                                            tool = "line",
                                            text = text ?? String.Empty,
                                            source = source
                                        }
                                }
                            };

        if (setNameProperty)
        {
            dict.Add("name", text);
        }

        return dict;
    }

    static public object DefaultPolygonGeoJsonProperties(this ApiToolEventArguments e, string text, string source, bool setNameProperty = false)
    {
        var dict = new Dictionary<string, object>()
                            {
                                { "stroke", e?.GetString("mapmarkup-color").OrTake("#ff0000") },
                                { "stroke-opacity", e?.GetDouble("mapmarkup-opacity") ?? 0.8 },
                                { "stroke-width", e?.GetInt("mapmarkup-lineweight") ?? 4 },
                                { "stroke-style", e?.GetString("mapmarkup-linestyle").OrTake("1") },
                                { "fill", e?.GetString("mapmarkup-fillcolor").OrTake("#ffff00")  },
                                { "fill-opacity", e?.GetDouble("mapmarkup-fillopacity") ?? 0.2 },
                                { "_meta",  new
                                        {
                                            tool = "polygon",
                                            text = text ?? String.Empty,
                                            source = source
                                        }
                                }
                            };

        if (setNameProperty)
        {
            dict.Add("name", text);
        }

        return dict;
    }

    static public object DefaultSymbolJsonProperties(this ApiToolEventArguments e, string text, string source, bool setNameProperty = false)
    {
        var dict = new Dictionary<string, object>()
                            {
                                { "symbol", e?.GetString("mapmarkup-symbol").OrTake("graphics/markers/pin1.png") },
                                { "_meta",  new
                                        {
                                            tool = "symbol",
                                            text = text ?? String.Empty,
                                            source = source
                                        }
                                }
                            };
        if (setNameProperty)
        {
            dict.Add("name", text);
        }

        return dict;
    }

    static public object DefaultTextJsonProperties(this ApiToolEventArguments e, string text, string source, bool setNameProperty = false)
    {
        var dict = new Dictionary<string, object>()
                            {
                                { "font-color", e?.GetString("mapmarkup-fontcolor").OrTake("#000") },
                                { "font-style", e?.GetString("mapmarkup-fontstyle").OrTake("regular") },
                                { "font-size", e?.GetString("mapmarkup-fontsize").OrTake("12") },
                                { "_meta",  new
                                        {
                                            tool = "text",
                                            text = text ?? String.Empty,
                                            source = source
                                        }
                                }
                            };
        if (setNameProperty)
        {
            dict.Add("name", text);
        }

        return dict;
    }

    static private object ToDoubleArray(object coordinates)
    {
        if (coordinates == null)
        {
            return null;
        }

        string coordinatesString = coordinates.ToString();

        return TryConvert<double[]>(coordinatesString)
            ?? (object)TryConvert<double[][]>(coordinatesString)
            ?? (object)TryConvert<double[][][]>(coordinatesString)
            ?? TryConvert<double[][][][]>(coordinatesString);
    }

    static private T TryConvert<T>(string json)
    {
        try
        {
            return JSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default(T);
        }
    }

    static public object GetValue(object propertiesElement, string propertyName)
    {
        var names = propertyName.Split('.');
        object result = null;

        if (JSerializer.IsJsonElement(propertiesElement))
        {
            result = JSerializer.GetJsonElementValue(propertiesElement, propertyName);
        }
        else if (propertiesElement is IDictionary<string, object>)
        {
            object properties = propertiesElement as IDictionary<string, object>;

            foreach (var name in propertyName.Split('.'))
            {
                if (JSerializer.IsJsonElement(properties))
                {
                    result = JSerializer.GetJsonElementValue(properties, name);
                    properties = result;
                }
                else if (properties is IDictionary<string, object> dict && dict.ContainsKey(name))
                {
                    result = ((IDictionary<string, object>)properties)[name];
                    properties = result;
                }
                else if (properties?.GetType().GetProperty(name) != null)
                {
                    result = properties.GetType().GetProperty(name).GetValue(properties);
                    properties = result;
                }
                else
                {
                    return null;
                }
            }
        }

        return result?.ToString();
    }

    #endregion
}
