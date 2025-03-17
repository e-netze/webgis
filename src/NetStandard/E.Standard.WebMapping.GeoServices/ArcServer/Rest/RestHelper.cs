using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class RestHelper
{
    public static FieldType FType(string parsedFieldTypeString)
    {
        switch (parsedFieldTypeString)
        {
            case "esriFieldTypeBlob":
                return FieldType.Unknown;
            case "esriFieldTypeDate":
                return FieldType.Date;
            case "esriFieldTypeDouble":
                return FieldType.Double;
            case "esriFieldTypeGeometry":
                return FieldType.Shape;
            case "esriFieldTypeGlobalID":
                return FieldType.GlobalId;
            case "esriFieldTypeGUID":
                return FieldType.GUID;
            case "esriFieldTypeInteger":
                return FieldType.Interger;
            case "esriFieldTypeOID":
                return FieldType.ID;
            case "esriFieldTypeRaster":
                return FieldType.Unknown;
            case "esriFieldTypeSingle":
                return FieldType.Float;
            case "esriFieldTypeSmallInteger":
                return FieldType.SmallInteger;
            case "esriFieldTypeString":
                return FieldType.String;
            case "esriFieldTypeXML":
                return FieldType.Unknown;
        }
        return FieldType.Unknown;
    }

    public static string ConvertGeometryToJson(Shape shape, int spatialReferenceId, bool hasZ = false, bool hasM = false)
    {
        string geometryType = GetGeometryTypeString(shape);
        switch (geometryType)
        {
            case "esriGeometryPoint":
                return Convert2DPointToJsonString((Point)shape, spatialReferenceId, hasZ, hasM);
            case "esriGeometryMultiPoint":
                return Convert2DMultiPointToJsonString((MultiPoint)shape, spatialReferenceId, hasZ, hasM);
            case "esriGeometryPolyline":
                return Convert2DPolylineToJsonString((Polyline)shape, spatialReferenceId, hasZ, hasM);
            case "esriGeometryPolygon":
                return Convert2DPolygonToJsonString((Polygon)shape, spatialReferenceId, hasZ, hasM);
            case "esriGeometryEnvelope":
                return Convert2DEnvelopeToJsonString((Envelope)shape, spatialReferenceId);
            default:
                throw new NotImplementedException("Passed in GeometryType is not implemented yet.");
        }
    }

    public static string GetGeometryTypeString(Shape shape)
    {
        if (shape is Point)
        {
            return "esriGeometryPoint";
        }
        else if (shape is MultiPoint)
        {
            return "esriGeometryMultiPoint";
        }
        else if (shape is Polyline)
        {
            return "esriGeometryPolyline";
        }
        else if (shape is Polygon)
        {
            return "esriGeometryPolygon";
        }
        else if (shape is Envelope)
        {
            return "esriGeometryEnvelope";
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static string Convert2DPointToJsonString(Point point, int spatialReferenceId, bool hasZ, bool hasM)
    {
        if (hasZ == false && hasM == false)
        {
            return
                @"{""x"":" + point.X.ToPlatformNumberString() +
                @",""y"":" + point.Y.ToPlatformNumberString() +
                @",""spatialReference"":{""wkid"":" + spatialReferenceId + "}" +
                "}";
        }

        StringBuilder sb = new StringBuilder();

        sb.Append(@"{""x"":" + point.X.ToPlatformNumberString() +
                  @",""y"":" + point.Y.ToPlatformNumberString());

        if (hasZ)
        {
            sb.Append(@",""z"":" + point.Z.ToPlatformNumberString());
        }
        if (hasM)
        {
            double mv = point is PointM && ((PointM)point).M != null ? Convert.ToDouble(((PointM)point).M) : 0.0D;
            sb.Append(@",""m"":" + ((double)mv).ToPlatformNumberString());
        }

        sb.Append(@",""spatialReference"":{""wkid"":" + spatialReferenceId + "}}");

        return sb.ToString();
    }

    private static string Convert2DEnvelopeToJsonString(Envelope envelope, int spatialReferenceId)
    {
        return
        @"{""xmin"":" + envelope.MinX.ToPlatformNumberString() +
        @",""ymin"":" + envelope.MinY.ToPlatformNumberString() +
        @",""xmax"":" + envelope.MaxX.ToPlatformNumberString() +
        @",""ymax"":" + envelope.MaxY.ToPlatformNumberString() +
        @",""spatialReference"":{""wkid"":" + spatialReferenceId + "}" +
        "}";
    }

    private static string Convert2DMultiPointToJsonString(MultiPoint multiPoint, int spatialReferenceId, bool hasZ, bool hasM)
    {
        PointCollection pointCollection;
        int pathCount = multiPoint.PointCount;
        PointCollection[] pointCollectionArray = new PointCollection[pathCount];
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Point point = multiPoint[pathIndex];
            //int pointCount = point.PointCount;

            pointCollection = new PointCollection();

            pointCollection.AddPoint(new Point(point.X, point.Y));

            pointCollectionArray[pathIndex] = pointCollection;
        }

        return Convert2DPointCollectionToJsonString(pointCollectionArray, PointCollectionParent.points, spatialReferenceId, hasZ, hasM);
    }

    private enum PointCollectionParent
    {
        paths,
        rings,
        points
    }
    private static string Convert2DPolylineToJsonString(Polyline polyline, int spatialReferenceId, bool hasZ, bool hasM)
    {
        PointCollection pointCollection;
        int pathCount = polyline.PathCount;
        PointCollection[] pointCollectionArray = new PointCollection[pathCount];
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Path path = polyline[pathIndex];
            int pointCount = path.PointCount;

            pointCollection = new PointCollection();

            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                pointCollection.AddPoint(Clone(path[pointIndex]));
            }
            pointCollectionArray[pathIndex] = pointCollection;
        }

        return Convert2DPointCollectionToJsonString(pointCollectionArray, PointCollectionParent.paths, spatialReferenceId, hasZ, hasM);
    }
    private static string Convert2DPolygonToJsonString(Polygon polygon, int spatialReferenceId, bool hasZ, bool hasM)
    {
        PointCollection pointCollection;
        int ringCount = polygon.RingCount;
        PointCollection[] pointCollectionArray = new PointCollection[ringCount];
        for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
        {
            Ring ring = polygon[ringIndex];
            int pointCount = ring.PointCount;

            pointCollection = new PointCollection();

            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                pointCollection.AddPoint(Clone(ring[pointIndex]));
            }
            pointCollectionArray[ringIndex] = pointCollection;
        }

        return Convert2DPointCollectionToJsonString(pointCollectionArray, PointCollectionParent.rings, spatialReferenceId, hasZ, hasM);
    }
    //private static string Convert2DPointCollectionToJsonString(PointCollection [] pointCollectionArray, PointCollectionParent parent, int spatialReferenceId)
    //{
    //    string jsonReturnStartHelper = @"{""" + parent + @""":[[";

    //    string jsonReturnInnerHelper = "";
    //    for (int arrayIndex = 0; arrayIndex < pointCollectionArray.Length; arrayIndex++)
    //    {
    //        int pointCount = pointCollectionArray[arrayIndex].PointCount;
    //        if (arrayIndex > 0)
    //        {
    //            jsonReturnInnerHelper += ",["; // nth array of points of single path/ring
    //        }
    //        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
    //        {
    //            jsonReturnInnerHelper +=
    //            "[" +
    //                pointCollectionArray[arrayIndex][pointIndex].X.ToString(webgisCMS.Core.Globals.Nhi) +
    //                "," +
    //                pointCollectionArray[arrayIndex][pointIndex].Y.ToString(webgisCMS.Core.Globals.Nhi) +
    //            "]";
    //            if ( ((pointIndex + 1) < pointCount) && (pointCollectionArray.Length > 1) && (arrayIndex == 0) )
    //            {
    //                jsonReturnInnerHelper += ",";
    //            }

    //            if (pointIndex < (pointCount - 1))
    //            {
    //                if (arrayIndex == (pointCollectionArray.Length - 1))
    //                {
    //                    jsonReturnInnerHelper += ",";
    //                }
    //            }
    //            else
    //            {
    //                jsonReturnInnerHelper += "]"; // end of points of single path/ring
    //                if (((arrayIndex + 1) < pointCollectionArray.Length) && (pointIndex < (pointCount - 1)))
    //                {
    //                    jsonReturnInnerHelper += ","; // append comma if there are more paths/rings left...
    //                }
    //            }
    //        }
    //        //if (arrayIndex > 0)
    //        //{
    //        //    jsonReturnInnerHelper += "]"; // nth array of points of single path/ring
    //        //}
    //    }

    //    string jsonReturnEndHelper = @"],""spatialReference"":{""wkid"":" + spatialReferenceId + "}}";


    //    return jsonReturnStartHelper + jsonReturnInnerHelper + jsonReturnEndHelper;        
    //}

    private static string Convert2DPointCollectionToJsonString(PointCollection[] pointCollectionArray, PointCollectionParent parent, int spatialReferenceId, bool hasZ, bool hasM)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(@"{");
        if (hasZ == true)
        {
            sb.Append(@"""hasZ"":true,");
        }

        if (hasM == true)
        {
            sb.Append(@"""hasM"":true,");
        }

        sb.Append(@"""" + parent + @""":[");

        for (int arrayIndex = 0; arrayIndex < pointCollectionArray.Length; arrayIndex++)
        {
            if (arrayIndex > 0)
            {
                sb.Append(",");
            }

            if (parent.ToString() != "points")
            {
                sb.Append("[");
            }

            for (int pointIndex = 0, to = pointCollectionArray[arrayIndex].PointCount; pointIndex < to; pointIndex++)
            {
                if (pointIndex > 0)
                {
                    sb.Append(",");
                }

                StringBuilder zm = new StringBuilder();
                if (hasZ)
                {
                    zm.Append("," + pointCollectionArray[arrayIndex][pointIndex].Z.ToPlatformNumberString());
                }
                if (hasM)
                {
                    double m = 0D;
                    try
                    {
                        if (pointCollectionArray[arrayIndex][pointIndex] is PointM)
                        {
                            m = Convert.ToDouble(((PointM)pointCollectionArray[arrayIndex][pointIndex]).M);
                        }
                    }
                    catch { }

                    zm.Append("," + m.ToPlatformNumberString());
                }

                sb.Append(
                "[" +
                    pointCollectionArray[arrayIndex][pointIndex].X.ToPlatformNumberString() +
                    "," +
                    pointCollectionArray[arrayIndex][pointIndex].Y.ToPlatformNumberString() +
                    zm.ToString() +
                "]");
            }

            if (parent.ToString() != "points")
            {
                sb.Append("]");
            }
        }

        sb.Append(@"],""spatialReference"":{""wkid"":" + spatialReferenceId + "}}");
        return sb.ToString();
    }

    #region Helper

    static private Point Clone(Point point)
    {
        if (point is PointM3)
        {
            return new PointM3(point, ((PointM3)point).M, ((PointM3)point).M2, ((PointM3)point).M3);
        }
        else if (point is PointM2)
        {
            return new PointM2(point, ((PointM2)point).M, ((PointM2)point).M2);
        }
        else if (point is PointM)
        {
            return new PointM(point, ((PointM)point).M);
        }

        return new Point(point);
    }

    #endregion
}
