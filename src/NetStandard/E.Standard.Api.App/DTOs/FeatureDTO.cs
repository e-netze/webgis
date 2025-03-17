using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class FeatureDTO : IHtml
{
    public FeatureDTO() { }

    public FeatureDTO(WebMapping.Core.Feature feature)
    {
        this.properties = new ExpandoObject();
        var dict = (IDictionary<string, object>)this.properties;

        foreach (var attr in feature.Attributes)
        {
            dict.Add(attr.Name, E.Standard.WebMapping.Core.Attribute.GeoJsonFeatureValue(attr.Value));
        }

        if (feature.HasDragAttributes)
        {
            this.drag_properties = new ExpandoObject();
            var dragDict = (IDictionary<string, object>)this.drag_properties;

            foreach (var attr in feature.DragAttributes)
            {
                dragDict.Add(attr.Name, attr.Value);
            }
        }

        geometry = ToGeometry(feature.Shape);
        if (feature.ZoomEnvelope != null)
        {
            bounds = new double[] { feature.ZoomEnvelope.MinX, feature.ZoomEnvelope.MinY, feature.ZoomEnvelope.MaxX, feature.ZoomEnvelope.MaxY };
        }
        if (feature.HoverShape != null)
        {
            HoverGeometry = ToGeometry(feature.HoverShape);
        }

        if (!String.IsNullOrWhiteSpace(feature.GlobalOid))
        {
            this.oid = feature.GlobalOid;
        }
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string oid { get; set; }
    public string type { get { return "Feature"; } set { } }
    public JsonGeometry geometry { get; set; }
    public object properties { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object drag_properties { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double[] bounds { get; set; }

    [JsonProperty("hover_geometry", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("hover_geometry")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonGeometry HoverGeometry { get; set; }

    public object this[string propertyName]
    {
        get
        {
            if (_properties == null && properties != null)
            {
                _properties = JSerializer.Deserialize<IDictionary<string, object>>(this.properties.ToString());
            }

            if (this._properties != null)
            {
                if (!this._properties.ContainsKey(propertyName))
                {
                    return null;
                }

                return (this._properties)[propertyName];
            }

            return null;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    private IDictionary<string, object> _properties { get; set; }

    public T GetPropery<T>(string propertyName)
    {
        object val = this[propertyName];
        if (val == null)
        {
            return default(T);
        }

        if (val.GetType() == typeof(string) && typeof(T) == typeof(float))
        {
            return (T)Convert.ChangeType(val.ToString().ToPlatformFloat(), typeof(T));
        }
        if (val.GetType() == typeof(string) && typeof(T) == typeof(double))
        {
            return (T)Convert.ChangeType(val.ToString().ToPlatformDouble(), typeof(T));
        }

        return (T)Convert.ChangeType(val, typeof(T));
    }



    #region Geometry

    public static JsonGeometry ToGeometry(Shape shape)
    {
        if (shape is Point)
        {
            return new JsonPointGeometry()
            {
                coordinates = new double[] { ((Point)shape).X, ((Point)shape).Y }
            };
        }

        if (shape is MultiPoint)
        {
            JsonMultiPointGeometry multiPoint = new JsonMultiPointGeometry();
            AppendCollectionToCoordinatesArray((MultiPoint)shape, multiPoint.CoordinatesArray);
            return multiPoint;
        }

        if (shape is Polyline)
        {
            List<JsonLineStringGeometry> lineStrings = new List<JsonLineStringGeometry>();
            for (int p = 0, to = ((Polyline)shape).PathCount; p < to; p++)
            {
                JsonLineStringGeometry lineString = new JsonLineStringGeometry();
                AppendCollectionToCoordinatesArray(((Polyline)shape)[p], lineString.CoordinatesArray);
                lineStrings.Add(lineString);
            }
            if (lineStrings.Count == 0)
            {
                return null;
            }

            if (lineStrings.Count == 1)
            {
                return lineStrings[0];
            }

            var multiLineString = new JsonMultiLineStringGeometry();
            foreach (var lineString in lineStrings)
            {
                multiLineString.CoorinatesListArray.Add(lineString);
            }
            return multiLineString;
        }

        if (shape is Polygon)
        {
            JsonPolygonGeometry polygon = new JsonPolygonGeometry();
            for (int p = 0, to = ((Polygon)shape).RingCount; p < to; p++)
            {
                polygon.CoorinatesListArray.Add(new JsonMultiPointGeometry());
                AppendCollectionToCoordinatesArray(((Polygon)shape)[p], polygon.CoorinatesListArray[p].CoordinatesArray);
            }

            return polygon;
        }

        return null;
    }

    private static void AppendCollectionToCoordinatesArray(PointCollection coll, List<double[]> array)
    {
        if (coll != null)
        {
            for (int i = 0, to = coll.PointCount; i < to; i++)
            {
                array.Add(new double[] { coll[i].X, coll[i].Y });
            }
        }
    }

    public Shape ToShape()
    {
        switch (this.geometry?.type?.ToString().ToLower())
        {
            case "point":
                double[] coordinates = JSerializer.Deserialize<double[]>(this.geometry.coordinates.ToString());
                return new Point(coordinates[0], coordinates[1]);
            case "linestring":
                double[][] lineString = JSerializer.Deserialize<double[][]>(this.geometry.coordinates.ToString());
                Polyline line = new Polyline();
                Path path = new Path();
                line.AddPath(path);

                for (int i = 0, to = lineString.GetLength(0); i < to; i++)
                {
                    path.AddPoint(new Point(lineString[i][0], lineString[i][1]));
                }

                return line;
            case "multilinestring":
                double[][][] paths = JSerializer.Deserialize<double[][][]>(this.geometry.coordinates.ToString());

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
            case "polygon":
                object polygonString = null;
                try
                {
                    polygonString = JSerializer.Deserialize<double[][]>(this.geometry.coordinates.ToString());
                }
                catch
                {
                    polygonString = JSerializer.Deserialize<double[][][]>(this.geometry.coordinates.ToString());
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
                    for (int r = 0, to = rings.GetLength(0); r < to; r++)
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

                return polygon;
        }

        return null;
    }

    #region Classes

    public class JsonGeometry
    {
        virtual public string type { get; set; }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        virtual public object coordinates { get; set; }

        public override bool Equals(object obj)
        {
            if (this is JsonPointGeometry && obj is JsonPointGeometry)
            {
                var jsonGeometry = (JsonPointGeometry)obj;

                if (!(this.coordinates is double[]) || !(jsonGeometry.coordinates is double[]))
                {
                    return false;
                }

                double[] p1 = (double[])this.coordinates, p2 = (double[])jsonGeometry.coordinates;
                if (p1.Length < 2 || p2.Length < 2)
                {
                    return false;
                }

                return
                    Math.Abs(p1[0] - p2[0]) < Shape.Epsilon &&
                    Math.Abs(p1[1] - p2[1]) < Shape.Epsilon;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
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

    public class JsonMultiLineStringGeometry : JsonCoordinatesListGeometry
    {
        override public string type { get { return "MultiLineString"; } set { } }

        [JsonProperty("coordinates", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public override object coordinates
        {
            get
            {
                List<object> paths = new List<object>();
                foreach (var r in CoorinatesListArray)
                {
                    var path = new List<object>();
                    for (int c = 0; c < r.CoordinatesArray.Count; c++)
                    {
                        path.Add(r.CoordinatesArray[c]);
                    }
                    paths.Add(path);
                }

                return paths.ToArray();
            }

            set
            {
            }
        }
    }

    //public class JsonMultiPolygonGeometry : JsonGeometry
    //{
    //    public JsonMultiPolygonGeometry(JsonPolygonGeometry[] polygons)
    //    {
    //        coordinates = polygons;
    //    }

    //    override public string type { get { return "MultiLineString"; } set { } }
    //    override public /*JsonPolygonGeometry[]*/ object coordinates { get; set; }
    //}

    #endregion

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToHeader("Feature", HtmlHelper.HeaderType.h3));
        sb.Append(HtmlHelper.ToTable((IDictionary<string, object>)this.properties));

        return sb.ToString();
    }

    #endregion
}