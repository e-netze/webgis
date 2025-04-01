#nullable enable

using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.FeatureServer;
using System;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
static internal class EsriFeaturesExtensions
{
    static public Shape? AsAggregatedShape(this EsriFeatures esriFeatures, int srsId, bool hasZ, bool hasM)
    {
        if (esriFeatures?.Features == null || esriFeatures.Features.Count() == 0 || esriFeatures.Features.First().Geometry == null)
        {
            return null;
        }

        Shape? shape = null;

        #region Geometry Type aus erstem Feature

        var anyShape = esriFeatures.Features
                                      .Select(f => f.Geometry)
                                      .FirstOrDefault(s => s != null);
        if (anyShape is null)
        {
            return null;
        }

        if (anyShape.Rings != null)
        {
            shape = new Polygon()
            {
                SrsId = srsId
            };
        }
        else if (anyShape.Paths != null)
        {
            shape = new Polyline()
            {
                SrsId = srsId
            };
        }
        else if (anyShape.X.HasValue == true
            && anyShape.Y.HasValue == true)
        {
            shape = new Point()
            {
                SrsId = srsId
            };
        }

        #endregion

        if (shape is Point)
        {
            ((Point)shape).X = anyShape.X!.Value;
            ((Point)shape).Y = anyShape.Y!.Value;
            if (hasZ)
            {
                ((Point)shape).Z = anyShape.Z.HasValue ? anyShape.Z.Value : 0;
            }
            if (hasM)
            {
                shape = new PointM((Point)shape, anyShape.M);
            }
        }
        else if (shape is Polyline)
        {
            foreach (var feature in esriFeatures.Features)
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
            foreach (var feature in esriFeatures.Features)
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

    static private Point CreatePoint(double?[,] coords, int coordIndex, bool hasZ, bool hasM)
    {
        int index = 0;

        var point = new Point(coords[coordIndex, index++]!.Value,
                              coords[coordIndex, index++]!.Value);

        if (hasZ && index < coords.GetLength(1))
        {
            point.Z = coords[coordIndex, index].HasValue
                ? coords[coordIndex, index]!.Value
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
