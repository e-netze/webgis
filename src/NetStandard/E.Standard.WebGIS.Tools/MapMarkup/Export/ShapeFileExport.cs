using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using E.Standard.Esri.Shapefile;
using E.Standard.Esri.Shapefile.IO;
using E.Standard.GeoJson;
using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Tools.MapMarkup.Export;

class ShapeFileExport : IExport
{
    private readonly MemoryStream _memoryStream;
    private readonly IStreamProvider _streamProvider;
    private readonly IBridge _bridge;
    private int _targetEpsgCode;
    private int _featuresCount = 0;

    public ShapeFileExport(IBridge bridge, int targetEpsgCode)
    {
        _memoryStream = new MemoryStream();
        _streamProvider = new ZipArchiveStreamProvider(_memoryStream);

        _bridge = bridge;
        _targetEpsgCode = targetEpsgCode;
    }

    public int FeatureCount => _featuresCount;

    public void AddFeatures(GeoJsonFeatures features)
    {
        var toolTypes = features.Features
                                .Select(f => f.GetPropery<string>("_meta.tool"))
                                .Distinct();

        #region Read Prj File

        byte[] prjFileBytes = null;
        try
        {
            prjFileBytes = System.Text.Encoding.Default.GetBytes(File.ReadAllText($"{_bridge.AppEtcPath}/prj/{_targetEpsgCode}.prj"));
        }
        catch { }

        #endregion

        using (var transformer = _bridge.GeometryTransformer(4326, _targetEpsgCode))
        {
            foreach (var toolType in toolTypes)
            {
                if (!Mapping.ShapeGeometryMapping.ContainsKey(toolType) ||
                   !Mapping.ShapePropertyMapping.ContainsKey(toolType))
                {
                    continue;
                }

                ShapeFile.geometryType geometryType = Mapping.ShapeGeometryMapping[toolType];
                var propertyMapping = Mapping.ShapePropertyMapping[toolType];

                List<IField> fields = new List<IField>();
                fields.Add(new Field("ID", FieldType.ID));
                fields.AddRange(propertyMapping.Keys
                                               .Select(k => new Field(k)));

                var shapeFile = ShapeFile.Create(_streamProvider, toolType, geometryType, fields);
                if (shapeFile == null)
                {
                    throw new Exception($"Can't create shape file {toolType}");
                }

                foreach (var feature in features.Features
                                                .Where(f => f.GetPropery<string>("_meta.tool") == toolType))
                {
                    Feature shapeFeature = new Feature();

                    var shape = feature.ToShape();

                    // measure_grid: one polygon per cell (frame only, if there are too many cells)
                    if (toolType == "measure_grid")
                    {
                        foreach (var cell in MeasureGridCells(feature, shape))
                        {
                            transformer.Transform(cell.polygon);

                            var cellFeature = new Feature() { Shape = cell.polygon };
                            foreach (var fieldName in propertyMapping.Keys)
                            {
                                string value = fieldName switch
                                {
                                    "LABEL" => cell.label,
                                    "COL" => cell.col,
                                    "ROW" => cell.row,
                                    _ => feature.GetPropery<string>(propertyMapping[fieldName])
                                };
                                cellFeature.Attributes.Add(new WebMapping.Core.Attribute(fieldName, value));
                            }

                            shapeFile.WriteShape(cellFeature);
                            _featuresCount++;
                        }

                        continue;
                    }

                    // object_distance: one polyline per connection line (touching objects => no line)
                    if (toolType == "object_distance")
                    {
                        foreach (var line in ObjectDistanceLines(feature))
                        {
                            transformer.Transform(line.polyline);

                            var lineFeature = new Feature() { Shape = line.polyline };
                            foreach (var fieldName in propertyMapping.Keys)
                            {
                                string value = fieldName switch
                                {
                                    "NR" => line.nr,
                                    "LENGTH_M" => line.length,
                                    "LAYER_A" => line.layerA,
                                    "OID_A" => line.oidA,
                                    "LAYER_B" => line.layerB,
                                    "OID_B" => line.oidB,
                                    _ => feature.GetPropery<string>(propertyMapping[fieldName])
                                };
                                lineFeature.Attributes.Add(new WebMapping.Core.Attribute(fieldName, value));
                            }

                            shapeFile.WriteShape(lineFeature);
                            _featuresCount++;
                        }

                        continue;
                    }

                    // buffer_zone: export the buffer polygon (WGS84, calculated by the client or recalculated here)
                    if (toolType == "buffer_zone")
                    {
                        shape = BufferZonePolygon(feature, shape);
                        if (shape is null)
                        {
                            continue;
                        }
                    }

                    // circle_arc: export the arc itself (WGS84, the 3 construction points are not exported)
                    if (toolType == "circle_arc")
                    {
                        shape = CircleArcCalculator.ArcPolylineWgs84(SpatialAlgorithms.ShapePoints(shape, false), feature.GetPropery<int>("_calcCrs"))
                                ?? shape;  // collinear => the line
                    }

                    transformer.Transform(shape);

                    // intersection_angle: vertices 0,1 => line 1, vertices 2,3 => line 2 => two parts
                    if (toolType == "intersection_angle" && shape is Polyline anglePolyline
                        && anglePolyline.PathCount == 1 && anglePolyline[0].PointCount == 4)
                    {
                        var path = anglePolyline[0];
                        shape = new Polyline(new[]
                        {
                            new E.Standard.WebMapping.Core.Geometry.Path(new List<Point>() { path[0], path[1] }),
                            new E.Standard.WebMapping.Core.Geometry.Path(new List<Point>() { path[2], path[3] })
                        });
                    }

                    shapeFeature.Shape = shape;

                    foreach (var fieldName in propertyMapping.Keys)
                    {
                        shapeFeature.Attributes.Add(new WebMapping.Core.Attribute(fieldName, feature.GetPropery<string>(propertyMapping[fieldName])));
                    }

                    shapeFile.WriteShape(shapeFeature);
                    _featuresCount++;
                }

                #region Prj File

                if (prjFileBytes != null)
                {
                    _streamProvider[$"{toolType}.prj"].Write(prjFileBytes, 0, prjFileBytes.Length);
                }

                #endregion
            }
        }
    }

    static private Polygon BufferZonePolygon(GeoJsonFeature feature, Shape source)
    {
        double[][][] rings = null;
        var bufferJson = feature.GetPropery<string>("bz-buffer");
        if (!String.IsNullOrEmpty(bufferJson))
        {
            try { rings = JSerializer.Deserialize<double[][][]>(bufferJson); } catch { }
        }

        rings ??= BufferZoneCalculator.CalcBufferRings(
                    SpatialAlgorithms.ShapePoints(source, false),
                    feature.GetPropery<string>("bz-source-type"),
                    feature.GetPropery<double>("bz-width"),
                    feature.GetPropery<int>("_calcCrs"),
                    BufferZoneCalculator.ParsePartStarts(feature.GetPropery<string>("bz-parts")));

        return BufferZoneCalculator.ToPolygon(rings);
    }

    // WGS84 connection lines from the stored od-lines (see webgis.map.graphics.js), numbers like the labels (index + 1)
    static private IEnumerable<(Polyline polyline, string nr, string length, string layerA, string oidA, string layerB, string oidB)> ObjectDistanceLines(GeoJsonFeature feature)
    {
        var lines = new List<(Polyline, string, string, string, string, string, string)>();

        static string Value(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
                ? value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => String.Empty,
                    _ => value.GetRawText()
                }
                : String.Empty;

        static Point LngLat(JsonElement element, string name)
            => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() >= 2
                ? new Point(value[0].GetDouble(), value[1].GetDouble())
                : null;

        try
        {
            string layerA = String.Empty, oidA = String.Empty;
            var aJson = feature.GetPropery<string>("od-a");
            if (!String.IsNullOrEmpty(aJson))
            {
                using (var a = JsonDocument.Parse(aJson))
                {
                    layerA = Value(a.RootElement, "layer");
                    oidA = Value(a.RootElement, "oid");
                }
            }

            var linesJson = feature.GetPropery<string>("od-lines");
            if (String.IsNullOrEmpty(linesJson))
            {
                return lines;
            }

            using (var doc = JsonDocument.Parse(linesJson))
            {
                int index = 0;
                foreach (var line in doc.RootElement.EnumerateArray())
                {
                    index++;

                    Point from = LngLat(line, "from"), to = LngLat(line, "to");
                    if (from is null || to is null
                        || (line.TryGetProperty("touching", out var touching) && touching.ValueKind == JsonValueKind.True))
                    {
                        continue;
                    }

                    double length = line.TryGetProperty("length", out var lengthElement) && lengthElement.ValueKind == JsonValueKind.Number
                        ? lengthElement.GetDouble()
                        : 0;

                    lines.Add((new Polyline(new[] { new E.Standard.WebMapping.Core.Geometry.Path(new List<Point>() { from, to }) }),
                               index.ToString(),
                               Math.Round(length, 2).ToString(System.Globalization.CultureInfo.InvariantCulture),
                               layerA, oidA,
                               Value(line, "layer"), Value(line, "oid")));
                }
            }
        }
        catch { }

        return lines;
    }

    // WGS84 cell polygons, same grid as the print (MeasureGridElement) and the client (L.MeasureGrid)
    static private IEnumerable<(Polygon polygon, string label, string col, string row)> MeasureGridCells(GeoJsonFeature feature, Shape source)
    {
        var cells = new List<(Polygon, string, string, string)>();

        using (var grid = MeasureGridCalculator.Calc(
                    SpatialAlgorithms.ShapePoints(source, false),
                    feature.GetPropery<int>("_calcCrs"),
                    feature.GetPropery<double>("mg-cell-width"),
                    feature.GetPropery<double>("mg-cell-height"),
                    feature.GetPropery<bool>("mg-align")))
        {
            if (grid is null)
            {
                return cells;
            }

            Polygon CellPolygon(double c0, double r0, double c1, double r1)
                => new Polygon(new[]
                {
                    new Ring(new List<Point>()
                    {
                        grid.LngLat(c0, r0), grid.LngLat(c1, r0), grid.LngLat(c1, r1), grid.LngLat(c0, r1), grid.LngLat(c0, r0)
                    })
                });

            if (grid.TooMany)
            {
                cells.Add((CellPolygon(0, 0, grid.Cols, grid.Rows), String.Empty, String.Empty, String.Empty));
                return cells;
            }

            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    cells.Add((CellPolygon(c, r, c + 1, r + 1),
                               MeasureGridCalculator.CellName(c, r),
                               MeasureGridCalculator.ColumnName(c),
                               (r + 1).ToString()));
                }
            }
        }

        return cells;
    }

    public byte[] GetBytes(bool throwExcetionIfEmpty)
    {
        if (throwExcetionIfEmpty && _featuresCount == 0)
        {
            throw new Exception("Für den Shape Export wurden keine Objekte gefunden.");
        }

        _streamProvider.FlushAndReleaseStreams();

        return _memoryStream.ToArray();
    }
}
