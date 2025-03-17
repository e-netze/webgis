using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.OGC.GML;

public class GeometryTranslator
{
    private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
    private static SpatialReferenceCollection _sRefs;

    public static void Init()
    {
        _sRefs = CoreApiGlobals.SRefStore.SpatialReferences;
    }

    #region Geometry from GML
    public static Shape GML2Geometry(string gml, GmlVersion version, out string srsName, bool interpretSrsAxis = true)
    {
        try
        {
            gml = gml.Replace("<gml:", "<").Replace("</gml:", "</");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(gml);

            XmlNode geomNode = doc.ChildNodes[0];

            srsName = (geomNode.Attributes["srsName"] != null ? geomNode.Attributes["srsName"].Value : String.Empty);

            SpatialReference sRef = null;
            if ((int)version > 2 && !String.IsNullOrEmpty(srsName) && interpretSrsAxis == true)
            {
                sRef = FromSrsName(srsName);
            }

            switch (geomNode.Name)
            {
                case "Point":
                    return GML2Point(geomNode, sRef);
                case "pointProperty":
                    return GML2Point(geomNode.SelectSingleNode("Point"), sRef);
                case "MultiPoint":
                    MultiPoint mp = new MultiPoint();
                    foreach (XmlNode pointNode in geomNode.SelectNodes("pointMember/Point"))
                    {
                        mp.AddPoint(GML2Point(pointNode, sRef));
                    }

                    if (mp.PointCount == 1)
                    {
                        return mp[0];
                    }

                    return mp;
                case "Box":
                case "Envelope":
                    return GML2Envelope(geomNode, sRef);
                case "LineString":
                    Path path = GML2Path(geomNode, sRef);
                    if (path == null)
                    {
                        return null;
                    }

                    Polyline polyline = new Polyline();
                    polyline.AddPath(path);
                    return polyline;
                case "MultiLineString":
                    return GML2Polyline(geomNode, sRef);
                case "curveProperty":
                    return GML2Polyline(geomNode, sRef);
                case "Polygon":
                    return GML2Polygon(geomNode, sRef);
                case "MultiPolygon":
                    return GML2MultiPolygon(geomNode, sRef);
                case "MultiSurface":
                    return GmlMultiSurface(geomNode, version, interpretSrsAxis);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            string err = ex.Message;
            srsName = String.Empty;
            return null;
        }
    }

    private static Point GML2Point(XmlNode pointNode, SpatialReference sRef)
    {
        if (pointNode == null)
        {
            return null;
        }

        XmlNode coordinatesNode = pointNode.SelectSingleNode("coordinates");
        if (coordinatesNode != null)
        {

            string[] xy = coordinatesNode.InnerText.Trim().Split(',');
            if (xy.Length < 2)
            {
                return null;
            }

            Point p = GmlPoint(new Point(
                double.Parse(xy[0], _nhi),
                double.Parse(xy[1], _nhi)), sRef);

            return p;
        }
        XmlNode posNode = pointNode.SelectSingleNode("pos");
        if (posNode != null)
        {
            string[] xy = posNode.InnerText.Trim().Split(' ');
            if (xy.Length < 2)
            {
                return null;
            }

            Point p = GmlPoint(new Point(
                double.Parse(xy[0], _nhi),
                double.Parse(xy[1], _nhi)), sRef);

            return p;
        }
        XmlNode coordNode = pointNode.SelectSingleNode("coord");
        if (coordNode != null)
        {
            XmlNode x = coordNode.SelectSingleNode("X");
            XmlNode y = coordNode.SelectSingleNode("Y");
            if (x != null && y != null)
            {
                return GmlPoint(new Point(double.Parse(x.InnerText, _nhi), double.Parse(y.InnerText, _nhi)), sRef);
            }
        }
        return null;
    }

    private static void CoordinatesToPointCollection(XmlNode coordinates, PointCollection pColl, SpatialReference sRef)
    {
        CoordinatesToPointCollection(coordinates, pColl, ' ', ',', sRef);
    }
    private static void CoordinatesToPointCollection(XmlNode coordinates, PointCollection pColl, char pointSplitter, char coordSplitter, SpatialReference sRef)
    {
        if (coordinates == null)
        {
            return;
        }

        string[] coords = coordinates.InnerText.Trim().Split(pointSplitter);

        foreach (string coord in coords)
        {
            string[] xy = coord.Split(coordSplitter);
            if (xy.Length < 2)
            {
                return;
            }

            pColl.AddPoint(GmlPoint(new Point(
                double.Parse(xy[0], _nhi),
                double.Parse(xy[1], _nhi)), sRef));
        }
    }

    private static void PosListToPointCollection(XmlNode posList, PointCollection pColl, SpatialReference sRef)
    {
        PosListToPointCollection(posList, pColl, ' ', sRef);
    }
    private static void PosListToPointCollection(XmlNode posList, PointCollection pColl, char splitter, SpatialReference sRef)
    {
        if (posList == null)
        {
            return;
        }

        string[] coords = posList.InnerText.Trim().Split(splitter);

        if (coords.Length % 2 != 0)
        {
            return;
        }

        for (int i = 0; i < coords.Length - 1; i += 2)
        {
            pColl.AddPoint(GmlPoint(new Point(
                double.Parse(coords[i], _nhi),
                double.Parse(coords[i + 1], _nhi)), sRef));
        }
    }

    private static Path GML2Path(XmlNode lineStringNode, SpatialReference sRef)
    {
        if (lineStringNode == null)
        {
            return null;
        }

        XmlNode coordNode = lineStringNode.SelectSingleNode("coordinates");
        if (coordNode != null)
        {
            Path path = new Path();
            CoordinatesToPointCollection(coordNode, path, sRef);
            return path;
        }
        XmlNode posNode = lineStringNode.SelectSingleNode("posList");
        if (posNode != null)
        {
            Path path = new Path();
            PosListToPointCollection(posNode, path, sRef);
            return path;
        }
        return null;
    }

    private static Polyline GML2Polyline(XmlNode multiLineStringNode, SpatialReference sRef)
    {
        if (multiLineStringNode == null)
        {
            return null;
        }

        Polyline polyline = new Polyline();
        if (multiLineStringNode.Name == "curveProperty")
        {
            foreach (XmlNode lineStringNode in multiLineStringNode.SelectNodes("LineString"))
            {
                Path path = GML2Path(lineStringNode, sRef);
                if (path != null)
                {
                    polyline.AddPath(path);
                }
            }
        }
        else
        {
            foreach (XmlNode lineStringNode in multiLineStringNode.SelectNodes("lineStringMember/LineString"))
            {
                Path path = GML2Path(lineStringNode, sRef);
                if (path != null)
                {
                    polyline.AddPath(path);
                }
            }
        }
        return polyline;
    }

    private static Polygon GML2Polygon(XmlNode polygonNode, SpatialReference sRef)
    {
        if (polygonNode == null)
        {
            return null;
        }

        Polygon polygon = new Polygon();
        if (polygonNode.Name == "Polygon")
        {
            foreach (XmlNode coordintes in polygonNode.SelectNodes("outerBoundaryIs/LinearRing/coordinates"))
            {
                Ring ring = new Ring();
                CoordinatesToPointCollection(coordintes, ring, sRef);
                if (ring.PointCount > 0)
                {
                    polygon.AddRing(ring);
                }
            }
            foreach (XmlNode coordintes in polygonNode.SelectNodes("innerBoundaryIs/LinearRing/coordinates"))
            {
                Hole hole = new Hole();
                CoordinatesToPointCollection(coordintes, hole, sRef);
                if (hole.PointCount > 0)
                {
                    polygon.AddRing(hole);
                }
            }

            // Kann es auch hier geben!! 
            foreach (XmlNode coordintes in polygonNode.SelectNodes("exterior/LinearRing/posList"))
            {
                Ring ring = new Ring();
                PosListToPointCollection(coordintes, ring, sRef);
                if (ring.PointCount > 0)
                {
                    polygon.AddRing(ring);
                }
            }
            foreach (XmlNode coordintes in polygonNode.SelectNodes("interior/LinearRing/posList"))
            {
                Hole hole = new Hole();
                PosListToPointCollection(coordintes, hole, sRef);
                if (hole.PointCount > 0)
                {
                    polygon.AddRing(hole);
                }
            }
        }
        else if (polygonNode.Name == "PolygonPatch")
        {
            foreach (XmlNode coordintes in polygonNode.SelectNodes("exterior/LinearRing/posList"))
            {
                Ring ring = new Ring();
                PosListToPointCollection(coordintes, ring, sRef);
                if (ring.PointCount > 0)
                {
                    polygon.AddRing(ring);
                }
            }
            foreach (XmlNode coordintes in polygonNode.SelectNodes("interior/LinearRing/posList"))
            {
                Hole hole = new Hole();
                PosListToPointCollection(coordintes, hole, sRef);
                if (hole.PointCount > 0)
                {
                    polygon.AddRing(hole);
                }
            }
        }
        return polygon;
    }

    private static Polygon GML2MultiPolygon(XmlNode multiPolygonNode, SpatialReference sRef)
    {
        Polygon ret = new Polygon();

        foreach (XmlNode polygonNode in multiPolygonNode.SelectNodes("polygonMember/Polygon"))
        {
            Polygon p = GML2Polygon(polygonNode, sRef);
            if (p == null || p.RingCount == 0)
            {
                continue;
            }

            for (int i = 0; i < p.RingCount; i++)
            {
                ret.AddRing(p[i]);
            }
        }

        return ret;
    }

    private static Envelope GML2Envelope(XmlNode envNode, SpatialReference sRef)
    {
        if (envNode == null)
        {
            return null;
        }

        PointCollection pColl = new PointCollection();

        XmlNode coordinates = envNode.SelectSingleNode("coordinates");
        if (coordinates != null)
        {
            //<gml:Box srsName="epsg:31467">
            //<gml:coordinates>3517721.548714,5522386.484305 3548757.845502,5556110.794487</gml:coordinates>
            //</gml:Box>

            CoordinatesToPointCollection(coordinates, pColl, sRef);
        }
        else
        {
            //<gml:Envelope srsName="_FME_0" srsDimension="2">
            //<gml:lowerCorner>-58461.5054920442 209579.200729251</gml:lowerCorner>
            //<gml:upperCorner>-17656.9734282536 265744.953950753</gml:upperCorner>
            //</gml:Envelope>

            XmlNode lowerCorner = envNode.SelectSingleNode("lowerCorner");
            XmlNode upperCorner = envNode.SelectSingleNode("upperCorner");

            CoordinatesToPointCollection(lowerCorner, pColl, '|', ' ', sRef);  // | dummy Seperator
            CoordinatesToPointCollection(upperCorner, pColl, '|', ' ', sRef);
        }

        if (pColl.PointCount == 2)
        {
            return new Envelope(pColl[0].X, pColl[0].Y, pColl[1].X, pColl[1].Y);
        }
        else
        {
            return null;
        }
    }

    private static Point GmlPoint(Point p, SpatialReference sRef)
    {
        if (sRef == null)
        {
            return p;
        }

        double X = p.X, Y = p.Y;
        switch (sRef.AxisX)
        {
            case AxisDirection.North:
                p.X = Y;
                break;
            case AxisDirection.South:
                p.X = -Y;
                break;
            case AxisDirection.West:
                p.X = -X;
                break;
            case AxisDirection.East:
                p.X = X;
                break;
        }
        switch (sRef.AxisY)
        {
            case AxisDirection.North:
                p.Y = Y;
                break;
            case AxisDirection.South:
                p.Y = -Y;
                break;
            case AxisDirection.West:
                p.Y = -X;
                break;
            case AxisDirection.East:
                p.Y = X;
                break;
        }
        return p;
    }

    private static Shape GmlMultiSurface(XmlNode node, GmlVersion version, bool interpretSrsAxis)
    {
        AggregateShape ret = new AggregateShape();

        foreach (XmlNode shapeNode in node.SelectNodes("surfaceMember/*"))
        {
            string srsName = String.Empty;
            Shape shape = GML2Geometry(shapeNode.OuterXml, version, out srsName, interpretSrsAxis);
            if (shape != null)
            {
                ret.AddShape(shape);
            }
        }

        if (ret.CountShapes == 1)
        {
            return ret[0];
        }

        return ret;
    }
    #endregion

    #region GML from Geometry
    public static string Geometry2GML(Shape geometry, string srsName, GmlVersion version, bool ignoreAxisDirection = false)
    {
        SpatialReference sRef = (int)version > 2 ? FromSrsName(srsName) : null;

        if (geometry is Envelope)
        {
            return Envelope2GML(geometry as Envelope, srsName, sRef, ignoreAxisDirection);
        }
        else if (geometry is Point)
        {
            return Point2GML(geometry as Point, srsName, sRef, ignoreAxisDirection, version);
        }
        else if (geometry is MultiPoint)
        {
            return MultiPoint2GML(geometry as MultiPoint, srsName, sRef, ignoreAxisDirection, version);
        }
        else if (geometry is Polyline)
        {
            return Polyline2GML(geometry as Polyline, srsName, sRef, ignoreAxisDirection, version);
        }
        else if (geometry is Polygon)
        {
            return Polygon2GML(geometry as Polygon, srsName, sRef, ignoreAxisDirection, version);
        }
        return "";
    }

    private static string CoordinateString(Point point, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (sRef != null && ignoreAxisDirection == false)
        {
            StringBuilder sb = new StringBuilder();
            switch (sRef.AxisX)
            {
                case AxisDirection.North:
                case AxisDirection.South: sb.Append(point.Y.ToString(_nhi)); break;
                case AxisDirection.East:
                case AxisDirection.West: sb.Append(point.X.ToString(_nhi)); break;
            }
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    sb.Append(" ");
                    break;
                default:
                    sb.Append(",");
                    break;

            }

            switch (sRef.AxisY)
            {
                case AxisDirection.North:
                case AxisDirection.South: sb.Append(point.Y.ToString(_nhi)); break;
                case AxisDirection.East:
                case AxisDirection.West: sb.Append(point.X.ToString(_nhi)); break;
            }
            return sb.ToString();
        }
        else
        {
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    return point.X.ToString(_nhi) + " " + point.Y.ToString(_nhi);
                default:
                    return point.X.ToString(_nhi) + "," + point.Y.ToString(_nhi);
            }
        }
    }
    private static string CoordinatesString(PointCollection pColl, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (pColl == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("<gml:posList>"); //sb.Append(@"<gml:coordinates>");
        for (int i = 0; i < pColl.PointCount; i++)
        {
            if (i != 0)
            {
                sb.Append(" ");
            }
            //sb.Append(pColl[i].X.ToString(_nhi) + "," + pColl[i].Y.ToString(_nhi));
            sb.Append(CoordinateString(pColl[i], sRef, ignoreAxisDirection, gmlVersion));
        }
        sb.Append("</gml:posList>"); //sb.Append(@"</gml:coordinates>");
        return sb.ToString();
    }
    private static string Envelope2GML(Envelope envelope, string srsName, SpatialReference sRef, bool ignoreAxisDirection)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(@"<gml:Envelope srsName=""" + srsName + @""">");
        sb.Append(@"<gml:lowerCorner>" +
            envelope.MinX.ToString(_nhi) + " " + envelope.MinY.ToString(_nhi) + "</gml:lowerCorner>");
        sb.Append(@"<gml:upperCorner>" +
            envelope.MaxX.ToString(_nhi) + " " + envelope.MaxY.ToString(_nhi) + "</gml:upperCorner>");
        sb.Append(@"</gml:Envelope>");

        //sb.Append(@"<gml:Box srsName=""" + srsName + @""">");
        //sb.Append(@"<gml:coordinates>");
        //sb.Append(CoordinateString(new Point(envelope.MinX, envelope.MinY), sRef) + " ");
        //sb.Append(CoordinateString(new Point(envelope.MaxX, envelope.MaxY), sRef));
        //sb.Append(@"</gml:coordinates>");
        //sb.Append(@"</gml:Box>");

        return sb.ToString();
    }

    private static string Point2GML(Point point, string srsName, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(@"<gml:Point srsName=""" + srsName + @""">");
        sb.Append(@"<gml:coordinates>");
        sb.Append(CoordinateString(point, sRef, ignoreAxisDirection, GmlVersion.v1));
        sb.Append(@"</gml:coordinates>");
        sb.Append(@"</gml:Point>");
        return sb.ToString();
    }

    private static string MultiPoint2GML(MultiPoint multiPoint, string srsName, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(@"<gml:MultiPoint srsName=""" + srsName + @""">");

        for (int i = 0, to = multiPoint.PointCount; i < to; i++)
        {
            sb.Append(@"<gml:pointMember>");
            sb.Append("<gml:Point>");
            sb.Append(@"<gml:coordinates>");
            sb.Append(CoordinateString(multiPoint[i], sRef, ignoreAxisDirection, GmlVersion.v1));
            sb.Append(@"</gml:coordinates>");
            sb.Append(@"</gml:Point>");
            sb.Append("</gml:pointMember>");
        }

        sb.Append(@"</gml:MultiPoint>");

        return sb.ToString();
    }

    private static string Path2GML(Path path, string srsName, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (path == null || path.PointCount == 0)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(@"<gml:LineString");
        if (srsName != String.Empty)
        {
            sb.Append(@" srsName=""" + srsName + @""">");
        }
        else
        {
            sb.Append(">");
        }

        sb.Append(CoordinatesString(path, sRef, ignoreAxisDirection, gmlVersion));

        sb.Append(@"</gml:LineString>");
        return sb.ToString();
    }
    private static string Polyline2GML(Polyline polyline, string srsName, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (polyline == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        if (polyline.PathCount == 1)
        {
            sb.Append(Path2GML(polyline[0], srsName, sRef, ignoreAxisDirection, gmlVersion));
        }
        else
        {
            sb.Append(@"<gml:MultiLineString srsName=""" + srsName + @""">");
            for (int i = 0; i < polyline.PathCount; i++)
            {
                sb.Append(Path2GML(polyline[i], String.Empty, sRef, ignoreAxisDirection, gmlVersion));
            }

            sb.Append(@"</gml:MultiLineString>");
        }
        return sb.ToString();
    }
    private static string Ring2GML(Ring ring, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (ring == null || ring.PointCount == 0)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        if (ring is Hole)
        {
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    sb.Append("<gml:interior>");
                    break;
                default:
                    sb.Append(@"<gml:innerBoundaryIs>");
                    break;
            }
        }
        else
        {
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    sb.Append("<gml:exterior>");
                    break;
                default:
                    sb.Append(@"<gml:outerBoundaryIs>");
                    break;
            }
        }

        sb.Append(@"<gml:LinearRing>");

        sb.Append(CoordinatesString(ring, sRef, ignoreAxisDirection, gmlVersion));

        sb.Append(@"</gml:LinearRing>");

        if (ring is Hole)
        {
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    sb.Append("</gml:interior>");
                    break;
                default:
                    sb.Append(@"</gml:innerBoundaryIs>");
                    break;
            }
        }
        else
        {
            switch (gmlVersion)
            {
                case GmlVersion.v3:
                    sb.Append("</gml:exterior>");
                    break;
                default:
                    sb.Append(@"</gml:outerBoundaryIs>");
                    break;
            }
        }

        return sb.ToString();
    }
    private static string Polygon2GML(Polygon polygon, string srsName, SpatialReference sRef, bool ignoreAxisDirection, GmlVersion gmlVersion)
    {
        if (polygon == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        polygon.CloseAllRings();

        sb.Append(@"<gml:Polygon srsName=""" + srsName + @""">");
        for (int i = 0; i < polygon.RingCount; i++)
        {
            sb.Append(Ring2GML(polygon[i], sRef, ignoreAxisDirection, gmlVersion));
        }

        sb.Append(@"</gml:Polygon>");
        return sb.ToString();
    }
    #endregion

    #region Helpers

    public static SpatialReference FromSrsName(string srsName)
    {
        if (String.IsNullOrEmpty(srsName))
        {
            return null;
        }

        SpatialReference sRef = null;
        try
        {
            if (_sRefs != null)
            {
                int epsg;

                if ((srsName.ToLower().StartsWith("http://") || srsName.ToLower().StartsWith("https://")) &&   // OGC!!
                    srsName.Contains("#"))
                {
                    int pos = srsName.LastIndexOf("#");
                    srsName = srsName.Substring(pos + 1, srsName.Length - pos - 1);
                }
                else
                {
                    int pos = srsName.LastIndexOf(":");
                    if (pos != -1)
                    {
                        srsName = srsName.Substring(pos + 1, srsName.Length - pos - 1);
                    }
                }
                if (int.TryParse(srsName, out epsg))
                {
                    sRef = _sRefs.ById(epsg);
                }
            }
        }
        catch { }

        return sRef;
    }

    #endregion
}
