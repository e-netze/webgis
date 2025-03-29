using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.FeatureServer;

public class EsriFeatures
{
    [JsonProperty(PropertyName = "features", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<EsriFeature> Features { get; set; }

    [JsonProperty("spatialReference", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonSpatialReference SpatialReference { get; set; }

    public Shape GetShape(int srsId, bool hasZ, bool hasM)
    {
        if (Features == null || Features.Count() == 0 || Features.First().Geometry == null)
        {
            return null;
        }

        Shape shape = null;

        #region Geometry Type aus erstem Feature

        var firstShape = Features.First().Geometry;
        if (firstShape.Rings != null)
        {
            shape = new Polygon()
            {
                SrsId = srsId
            };
        }
        else if (firstShape.Paths != null)
        {
            shape = new Polyline()
            {
                SrsId = srsId
            };
        }
        else if (firstShape.X.HasValue && firstShape.Y.HasValue)
        {
            shape = new Point()
            {
                SrsId = srsId
            };
        }

        #endregion

        if (shape is Point)
        {
            ((Point)shape).X = firstShape.X.Value;
            ((Point)shape).Y = firstShape.Y.Value;
            if (hasZ)
            {
                ((Point)shape).Z = firstShape.Z.HasValue ? firstShape.Z.Value : 0;
            }
            if (hasM)
            {
                shape = new PointM((Point)shape, firstShape.M);
            }
        }
        else if (shape is Polyline)
        {
            foreach (var feature in Features)
            {
                var geometry = feature.Geometry;
                if (geometry == null || geometry.Paths == null)
                {
                    continue;
                }

                for (var p = 0; p < geometry.Paths.Length; p++)
                {
                    var path = geometry.Paths[p];
                    var shapePath = new Path();
                    for (int i = 0; i < path.GetLength(0); i++)
                    {
                        if (!path[i, 0].HasValue || !path[i, 1].HasValue)
                        {
                            throw new Exception("Invalid geometry");
                        }

                        //shapePath.AddPoint(new Point(path[i, 0].Value, path[i, 1].Value));
                        shapePath.AddPoint(CreatePoint(path, i, hasZ, hasM));
                    }
                    ((Polyline)shape).AddPath(shapePath);
                }
            }
        }
        else if (shape is Polygon)
        {
            foreach (var feature in Features)
            {
                var geometry = feature.Geometry;
                if (geometry == null || geometry.Rings == null)
                {
                    continue;
                }

                for (var p = 0; p < geometry.Rings.Length; p++)
                {
                    var ring = geometry.Rings[p];
                    var shapeRing = new Ring();
                    for (int i = 0; i < ring.GetLength(0); i++)
                    {
                        if (!ring[i, 0].HasValue || !ring[i, 1].HasValue)
                        {
                            throw new Exception("Invalid geometry");
                        }

                        //shapeRing.AddPoint(new Point(ring[i, 0].Value, ring[i, 1].Value));
                        shapeRing.AddPoint(CreatePoint(ring, i, hasZ, hasM));
                    }
                    ((Polygon)shape).AddRing(shapeRing);
                }
            }
        }

        return shape;
    }

    #region Helper

    private Point CreatePoint(double?[,] coords, int coordIndex, bool hasZ, bool hasM)
    {
        int index = 0;

        var point = new Point(coords[coordIndex, index++].Value, coords[coordIndex, index++].Value);

        if (hasZ && index < coords.GetLength(1))
        {
            point.Z = coords[coordIndex, index].HasValue
                ? coords[coordIndex, index].Value
                : 0D;

            index++;
        }

        if (hasM && index < coords.GetLength(1))
        {
            point = new PointM(point, coords[coordIndex, index++]);
        }

        return point;
    }

    #endregion
}