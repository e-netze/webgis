using E.Standard.Json;
using E.Standard.Maths.Extensions.Speric;
using E.Standard.Maths.Primitives;
using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.GeoJson;

public class GeoJsonFeature
{
    [JsonProperty("oid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("oid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Oid { get; set; }

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get { return "Feature"; } set { } }

    [JsonProperty("geometry")]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    public JsonGeometry Geometry { get; set; }

    [JsonProperty("properties")]
    [System.Text.Json.Serialization.JsonPropertyName("properties")]
    public object Properties { get; set; }

    public Shape ToShape()
    {
        switch (this.Geometry?.type?.ToString().ToLower())
        {
            case "point":
                double[] coordinates = this.Geometry.coordinates is double[]?
                    (double[])this.Geometry.coordinates :
                    JSerializer.Deserialize<double[]>(this.Geometry.coordinates.ToString());

                switch (this.GetPropery<string>("_meta.tool"))
                {
                    case "circle":
                        var radius = this.GetPropery<double>("circle-radius");

                        GeoLocation center = new GeoLocation(coordinates[0], coordinates[1]);
                        var ring = new Ring();
                        ring.AddPoints(center.ToSphericCirclePoints(radius)
                                             .Select(location => new Point(location.Longitude, location.Latitude)));
                        var circlePolygon = new Polygon();
                        circlePolygon.AddRing(ring);
                        return circlePolygon;
                    case "distance_circle":
                        var dc_radius = this.GetPropery<double>("dc-radius");
                        var dc_steps = Math.Max(1, this.GetPropery<int>("dc-steps"));

                        GeoLocation dc_center = new GeoLocation(coordinates[0], coordinates[1]);
                        var dc_polygon = new Polygon();

                        for (int i = dc_steps; i >= 1; i--)
                        {
                            var dc_ring = new Ring();
                            dc_ring.AddPoints(dc_center.ToSphericCirclePoints((dc_radius / dc_steps) * i)
                                                       .Select(location => new Point(location.Longitude, location.Latitude)));
                            dc_polygon.AddRing(dc_ring);
                        }

                        return dc_polygon;
                    default:
                        return new Point(coordinates[0], coordinates[1]);
                }
            case "linestring":
                double[][] lineString = this.Geometry.coordinates is double[][]?
                    (double[][])this.Geometry.coordinates :
                    JSerializer.Deserialize<double[][]>(this.Geometry.coordinates.ToString());


                Polyline line = new Polyline();
                Path linePath = new Path();
                line.AddPath(linePath);

                for (int i = 0, to = lineString.GetLength(0); i < to; i++)
                {
                    linePath.AddPoint(new Point(lineString[i][0], lineString[i][1]));
                }

                return line;
            case "multilinestring":
                double[][][] paths = this.Geometry.coordinates is double[][][]?
                    (double[][][])this.Geometry.coordinates :
                    JSerializer.Deserialize<double[][][]>(this.Geometry.coordinates.ToString());

                Polyline multiLine = new Polyline();
                for (int p = 0, to = paths.GetLength(0); p < to; p++)
                {
                    Path multiLinePath = new Path();
                    multiLine.AddPath(multiLinePath);

                    var coords = paths[p];
                    for (int i = 0, to2 = coords.GetLength(0); i < to2; i++)
                    {
                        multiLinePath.AddPoint(new Point(coords[i][0], coords[i][1]));
                    }
                }
                return multiLine;
            case "multipolygon":
            case "polygon":
                object polygonString = this.Geometry.coordinates;
                if (!(polygonString is double[][])
                    && !(polygonString is double[][][])
                    && !(polygonString is double[][][][]))
                {
                    try
                    {
                        polygonString = JSerializer.Deserialize<double[][]>(this.Geometry.coordinates.ToString());
                    }
                    catch
                    {
                        try
                        {
                            polygonString = JSerializer.Deserialize<double[][][]>(this.Geometry.coordinates.ToString());
                        }
                        catch
                        {
                            polygonString = JSerializer.Deserialize<double[][][][]>(this.Geometry.coordinates.ToString());
                        }
                    }
                }
                Polygon polygon = new Polygon();

                if (polygonString is double[][])
                {
                    Ring ring = new Ring();
                    polygon.AddRing(ring);

                    var coords = (double[][])polygonString;
                    for (int i = 0, to = coords.GetLength(0); i < to; i++)
                    {
                        ring.AddPoint(new Point(coords[i][0], coords[i][1]));
                    }
                }
                else if (polygonString is double[][][])
                {
                    var rings = (double[][][])polygonString;
                    for (int r = 0, to_r = rings.GetLength(0); r < to_r; r++)
                    {
                        Ring ring = new Ring();
                        polygon.AddRing(ring);

                        var coords = rings[r];
                        for (int i = 0, to_i = coords.GetLength(0); i < to_i; i++)
                        {
                            ring.AddPoint(new Point(coords[i][0], coords[i][1]));
                        }
                    }
                }
                else if (polygonString is double[][][][]) // Multipolygon
                {
                    var subPolygons = (double[][][][])polygonString;

                    for (int p = 0, to_p = subPolygons.GetLength(0); p < to_p; p++)
                    {
                        var rings = subPolygons[p];
                        for (int r = 0, to_r = rings.GetLength(0); r < to_r; r++)
                        {
                            Ring ring = new Ring();
                            polygon.AddRing(ring);

                            var coords = rings[r];
                            for (int i = 0, to2 = coords.GetLength(0); i < to2; i++)
                            {
                                ring.AddPoint(new Point(coords[i][0], coords[i][1]));
                            }
                        }
                    }
                }

                return polygon;
        }

        return null;
    }

    public void FromShape(Shape shape)
    {
        if (shape is Point)
        {
            this.Geometry = new JsonGeometry()
            {
                type = "Point",
                coordinates = new double[] { ((Point)shape).X, ((Point)shape).Y }
            };
        }
        else if (shape is MultiPoint multiPoint && multiPoint.PointCount == 1)
        {
            this.Geometry = new JsonGeometry()
            {
                type = "Point",
                coordinates = new double[] { multiPoint[0].X, multiPoint[0].Y }
            };
        }
        else if (shape is Polyline)
        {
            var polyline = (Polyline)shape;
            if (polyline.PathCount == 1)
            {
                this.Geometry = new JsonGeometry()
                {
                    type = "LineString",
                    coordinates = polyline[0]
                        .ToArray()
                        .Select(point => new double[] { point.X, point.Y })
                        .ToArray()
                };
            }
            else if (polyline.PathCount > 1)
            {
                this.Geometry = new JsonGeometry()
                {
                    type = "MultiLineString",
                    coordinates = polyline
                        .ToArray()  // Paths
                        .Select(path =>
                                            path
                                                .ToArray()  // Points
                                                .Select(point => new double[] { point.X, point.Y })
                                                .ToArray())
                        .ToArray()
                };
            }
        }
        else if (shape is Polygon)
        {
            var polygon = (Polygon)shape;

            this.Geometry = new JsonGeometry()
            {
                type = "Polygon",
                coordinates = polygon
                    .ToArray()  // Rings
                    .Select(ring =>
                                            ring
                                                .ToArray() // Points
                                                .Select(point => new double[] { point.X, point.Y })
                                                .ToArray())
                    .ToArray()
            };
        }
    }

    public T GetPropery<T>(string propertyName)
    {
        object val = this[propertyName];
        if (val == null)
        {
            return default(T);
        }

        return (T)Convert.ChangeType(val, typeof(T));
    }

    public object GetValue(string propertyName)
        => this[propertyName];

    public object this[string propertyName]
    {
        get
        {
            var properties = this.Properties;
            object result = null;

            if (JSerializer.IsJsonElement(properties))
            {
                result = JSerializer.GetJsonElementValue(properties, propertyName);
            }
            else
            {
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

            return result;
        }
    }

    public object GetFirstOrDefault(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var result = this[propertyName];
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public bool HasProperty(string propertyName)
    {
        return this[propertyName] != null;
    }

    public void PropertiesToDict()
        => this.Properties = this.PropertiesAsDict();

    public IDictionary<string, object> PropertiesAsDict()
    {
        var resultDict = this.Properties switch
        {
            _ when JSerializer.IsJsonElement(this.Properties) => JSerializer.Deserialize<Dictionary<string, object>>(this.Properties.ToString()),
            IDictionary<string, object> dict => dict,
            null => new Dictionary<string, object>(),
            _ => throw new Exception("Properties is not a Dictionary<string, object>")
        };

        foreach (string key in resultDict.Keys)
        {
            resultDict[key] = JSerializer.AsValueIfJsonValueType(resultDict[key]);
        }

        return resultDict;
    }

    public IDictionary<string, object> PropertiesAsDictResursive()
    {
        var dict = PropertiesAsDict();

        DictionaryValuesToDictionary(dict);

        return dict;
    }

    #region Helper

    private void DictionaryValuesToDictionary(IDictionary<string, object> dict)
    {
        foreach (var key in dict.Keys)
        {
            if (JSerializer.IsJsonElement(dict[key]))
            {
                try
                {
                    dict[key] = JSerializer.Deserialize<Dictionary<string, object>>(dict[key].ToString());
                    DictionaryValuesToDictionary((Dictionary<string, object>)dict[key]);
                }
                catch
                {
                    try
                    {
                        dict[key] = JSerializer.Deserialize<object[]>(dict[key].ToString());
                    }
                    catch
                    {
                        dict[key] = dict[key].ToString();
                    }
                }
            }
        }
    }

    #endregion

    public void SetProperty(string propertyName, object val)
    {
        var propertyNames = propertyName.Split('.');

        if (propertyNames.Length > 1)
        {
            PropertiesAsDictResursive();
        }
        else if (JSerializer.IsJsonElement(this.Properties))
        {
            PropertiesToDict();
        }

        if (this.Properties is IDictionary<string, object>)
        {
            var dict = (IDictionary<string, object>)this.Properties;

            foreach (var property in propertyNames.Take(propertyNames.Length - 1))
            {
                dict = dict != null
                     ? dict[property] as Dictionary<string, object>
                     : null;
            }

            if (dict != null)
            {
                dict[propertyName.Split('.').Last()] = val;
            }
        }
        else
        {
            throw new Exception("Properties is not a Dictionary<string, object>");
        }
    }

    public bool TrySetProperty(string propertyName, object val, bool ignorNull)
    {
        if (ignorNull && val == null)
        {
            return true;
        }

        try
        {
            SetProperty(propertyName, val);

            return true;
        }
        catch
        {
            return false;
        }
    }

    #region Classes

    public class JsonGeometry
    {
        virtual public string type { get; set; }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        virtual public object coordinates { get; set; }
    }

    public class JsonPointGeometry : JsonGeometry
    {
        override public string type { get { return "Point"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        override public /*double[]*/object coordinates { get; set; }
    }

    public class JsonMultiPointGeometry : JsonGeometry
    {
        override public string type { get { return "MultiPoint"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        override public /*double[][]*/ object coordinates { get { return CoordinatesArray.ToArray(); } set { } }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<double[]> CoordinatesArray = new List<double[]> { };
    }

    public class JsonCoordinatesListGeometry : JsonGeometry
    {
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<JsonMultiPointGeometry> CoorinatesListArray = new List<JsonMultiPointGeometry>();
    }

    public class JsonLineStringGeometry : JsonMultiPointGeometry
    {
        override public string type { get { return "LineString"; } set { } }
    }

    public class JsonPolygonGeometry : JsonCoordinatesListGeometry
    {
        override public string type { get { return "Polygon"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public override object coordinates
        {
            get
            {
                List<object> rings = new List<object>();
                foreach (var r in CoorinatesListArray)
                {
                    var ring = new List<object>();
                    for (int c = 0; c < r.CoordinatesArray.Count; c++)
                    {
                        ring.Add(r.CoordinatesArray[c]);
                    }
                    rings.Add(ring);
                }

                return rings.ToArray();
            }

            set
            {
            }
        }
    }

    public class JsonMultiLineStringGeometry : JsonGeometry
    {
        public JsonMultiLineStringGeometry(JsonLineStringGeometry[] lineStrings)
        {
            this.coordinates = lineStrings;
        }

        override public string type { get { return "MultiLineString"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        override public /*JsonLineStringGeometry[]*/ object coordinates { get; set; }
    }

    public class JsonMultiPolygonGeometry : JsonGeometry
    {
        public JsonMultiPolygonGeometry(JsonPolygonGeometry[] polygons)
        {
            coordinates = polygons;
        }

        override public string type { get { return "MultiLineString"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        override public /*JsonPolygonGeometry[]*/ object coordinates { get; set; }
    }

    #endregion
}
