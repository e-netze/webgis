using E.Standard.Platform;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.Core.Extensions;

static public class WKTExtensions
{
    public readonly static System.Globalization.NumberFormatInfo _nhi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

    #region ToWKT

    public static string WKTFromShape(this Shape geometry)
    {
        StringBuilder sb = new StringBuilder();
        if (geometry is Point)
        {
            sb.Append("POINT(");
            AppendPoint(sb, (Point)geometry);
            sb.Append(")");
        }
        else if (geometry is MultiPoint)
        {
            sb.Append("MULTIPOINT(");
            bool first = true;
            for (int i = 0; i < ((MultiPoint)geometry).PointCount; i++)
            {
                Point mPoint = ((MultiPoint)geometry)[i];
                if (mPoint == null)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(",");
                }

                sb.Append("(");
                AppendPoint(sb, mPoint);
                sb.Append(")");
                first = false;
            }
            sb.Append(")");
        }
        else if (geometry is Polyline)
        {
            sb.Append("MULTILINESTRING(");
            AppendPolyline(sb, (Polyline)geometry);
            sb.Append(")");
        }
        else if (geometry is Polygon)
        {
            switch (((Polygon)geometry).RingCount)
            {
                case 0:
                    sb.Append("MULTIPOLYGON EMPTY");
                    break;
                case 1:
                    sb.Append("POLYGON(");
                    AppendPolygon(sb, (Polygon)geometry);
                    sb.Append(")");
                    break;
                default:
                    // ToDo: Inner, outer Rings...
                    sb.Append("MULTIPOLYGON((");
                    AppendPolygon(sb, (Polygon)geometry);
                    sb.Append("))");
                    //throw new Exception("Can't handle complex features...");
                    break;
            }
        }
        else if (geometry is Envelope)
        {
            Envelope env = (Envelope)geometry;
            sb.Append("POLYGON((");
            sb.Append(env.MinX.ToString(_nhi) + " ");
            sb.Append(env.MinY.ToString(_nhi) + ",");

            sb.Append(env.MaxX.ToString(_nhi) + " ");
            sb.Append(env.MinY.ToString(_nhi) + ",");

            sb.Append(env.MaxX.ToString(_nhi) + " ");
            sb.Append(env.MaxY.ToString(_nhi) + ",");

            sb.Append(env.MinX.ToString(_nhi) + " ");
            sb.Append(env.MaxY.ToString(_nhi) + ",");

            sb.Append(env.MinX.ToString(_nhi) + " ");
            sb.Append(env.MinY.ToString(_nhi) + "))");
        }
        return sb.ToString();
    }

    private static void AppendPoint(StringBuilder sb, Point point)
    {
        if (point == null)
        {
            return;
        }

        sb.Append(point.X.ToString(_nhi) + " " + point.Y.ToString(_nhi));
    }

    private static void AppendPointCollection(StringBuilder sb, PointCollection pColl)
    {
        if (pColl == null || pColl.PointCount == 0)
        {
            return;
        }

        sb.Append("(");
        bool first = true;
        for (int i = 0; i < pColl.PointCount; i++)
        {
            Point p = pColl[i];
            if (p != null)
            {
                if (!first)
                {
                    sb.Append(",");
                }

                AppendPoint(sb, p);
                first = false;
            }
        }
        sb.Append(")");
    }

    private static void AppendPolyline(StringBuilder sb, Polyline pLine)
    {
        if (pLine == null || pLine.PathCount == 0)
        {
            return;
        }

        bool first = true;
        for (int i = 0; i < pLine.PathCount; i++)
        {
            Path p = pLine[i];
            if (p != null && p.PointCount > 1)
            {
                if (!first)
                {
                    sb.Append(",");
                }

                AppendPointCollection(sb, p);
                first = false;
            }
        }
    }

    private static void AppendPolygon(StringBuilder sb, Polygon poly)
    {
        if (poly == null || poly.RingCount == 0)
        {
            return;
        }

        bool first = true;
        for (int i = 0; i < poly.RingCount; i++)
        {
            Ring r = poly[i];
            if (r != null && r.PointCount > 2)
            {
                if (!first)
                {
                    sb.Append(",");
                }

                r.ClosePath();
                AppendPointCollection(sb, r);
                first = false;
            }
        }
    }

    #endregion

    #region FromWKT

    public static Shape ShapeFromWKT(this string wkt)
    {
        wkt = Trim(wkt);

        List<string> pathStrings = new List<string>();
        ExtractPaths(wkt, pathStrings);
        if (pathStrings.Count == 0)
        {
            throw new Exception($"No geometry found in WKT: {wkt}");
        }

        if (wkt.StartsWith("POINT(", StringComparison.OrdinalIgnoreCase))
        {
            string[] xy = pathStrings[0].Split(' ');
            return new Point(xy[0].ToPlatformDouble(), xy[1].ToPlatformDouble());
        }
        else if (wkt.StartsWith("MULTIPOINT(", StringComparison.OrdinalIgnoreCase))
        {
            var multiPoint = ReadMultiPoint(pathStrings[0]);
            if (multiPoint != null && multiPoint.PointCount == 1)
            {
                return multiPoint[0];
            }

            return multiPoint;
        }
        else if (wkt.StartsWith("LINESTRING(", StringComparison.OrdinalIgnoreCase))
        {
            return ReadMultiLineString(pathStrings);
        }
        else if (wkt.StartsWith("MULTILINESTRING(", StringComparison.OrdinalIgnoreCase))
        {
            return ReadMultiLineString(pathStrings);
        }
        else if (wkt.StartsWith("POLYGON(", StringComparison.OrdinalIgnoreCase))
        {
            return ReadMultiPolygon(pathStrings);
        }
        else if (wkt.StartsWith("MULTIPOLYGON(", StringComparison.OrdinalIgnoreCase))
        {
            return ReadMultiPolygon(pathStrings);
        }
        return null;
    }

    private static string Trim(string str)
    {
        str = str.Trim();
        while (str.Contains(" ,"))
        {
            str = str.Replace(" ,", ",");
        }

        while (str.Contains(", "))
        {
            str = str.Replace(", ", ",");
        }

        while (str.Contains(" ("))
        {
            str = str.Replace(" (", "(");
        }

        while (str.Contains(") "))
        {
            str = str.Replace(") ", "(");
        }

        return str;
    }

    private static List<string> ExtractOutermostParenthesesContent(string input)
    {
        List<string> results = new List<string>();
        int openParenthesesCount = 0;
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '(')
            {
                if (openParenthesesCount == 0)
                {
                    start = i + 1;
                }
                openParenthesesCount++;
            }
            else if (input[i] == ')')
            {
                openParenthesesCount--;
                if (openParenthesesCount == 0)
                {
                    results.Add(input.Substring(start, i - start));
                }
            }
        }

        return results;
    }

    private static void ExtractPaths(string input, List<string> paths)
    {
        if (!AreParenthesesBalanced(input))
        {
            throw new Exception($"Sytax error in WKT: {input}");
        }

        if (input.Contains("("))
        {
            foreach (var content in ExtractOutermostParenthesesContent(input))
            {
                ExtractPaths(content, paths);
            }
        }
        else
        {
            paths.Add(input);
        }
    }

    private static bool AreParenthesesBalanced(string input)
    {
        int openParenthesesCount = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '(')
            {
                openParenthesesCount++;
            }
            else if (input[i] == ')')
            {
                openParenthesesCount--;
            }

            if (openParenthesesCount < 0)
            {
                return false; // Schließende Klammer ohne passende öffnende Klammer
            }
        }

        return openParenthesesCount == 0;
    }

    private static PointCollection ReadPointCollection(string pathString)
    {
        PointCollection pColl = new PointCollection();

        foreach (string coords in pathString.Split(','))
        {
            string[] xy = coords.Split(' ');
            int pointSrsId = 0;
            Point newPoint = null;

            if (xy.Length == 2)
            {
                pColl.AddPoint(newPoint = new Point(xy[0].ToPlatformDouble(), xy[1].ToPlatformDouble()));
            }
            else if (xy.Length > 2)
            {
                object meta = null, mValue = null, m2Value = null;
                double zValue = 0.0;
                bool hasZ = false;

                for (int i = 2; i < xy.Length; i++)
                {
                    if (xy[i].StartsWith("meta:"))
                    {
                        meta = xy[i];
                    }
                    else if (xy[i].StartsWith("m:"))
                    {
                        mValue = xy[i].Substring("m:".Length);
                        if (mValue.ToString().TryToPlatformDouble(out double doubleMValue))
                        {
                            mValue = doubleMValue;
                        }
                    }
                    else if (xy[i].StartsWith("m2:"))
                    {
                        m2Value = xy[i].Substring("m2:".Length);
                        if (m2Value.ToString().TryToPlatformDouble(out double doubleM2Value))
                        {
                            m2Value = doubleM2Value;
                        }
                    }
                    else if (xy[i].StartsWith("z:"))
                    {
                        hasZ = (xy[i].Substring(2).TryToPlatformDouble(out zValue));
                    }
                    else if (xy[i].StartsWith("srs:"))
                    {
                        pointSrsId = int.Parse(xy[i].Substring(4));
                    }
                }

                if (meta != null)
                {
                    pColl.AddPoint(newPoint = new PointM3(xy[0].ToPlatformDouble(),
                                               xy[1].ToPlatformDouble(),
                                               zValue,
                                               mValue, m2Value, meta));
                }
                else if (m2Value != null)
                {
                    pColl.AddPoint(newPoint = new PointM2(xy[0].ToPlatformDouble(),
                                               xy[1].ToPlatformDouble(),
                                               zValue,
                                               mValue, m2Value));
                }
                else if (mValue != null)
                {
                    pColl.AddPoint(newPoint = new PointM(xy[0].ToPlatformDouble(),
                                              xy[1].ToPlatformDouble(),
                                              zValue,
                                              mValue));
                }
                else if (hasZ)
                {
                    pColl.AddPoint(newPoint = new Point(xy[0].ToPlatformDouble(),
                                             xy[1].ToPlatformDouble(),
                                             zValue));
                }
                else if (pointSrsId > 0)
                {
                    pColl.AddPoint(newPoint = new Point(xy[0].ToPlatformDouble(), xy[1].ToPlatformDouble()));
                }
                else
                {
                    throw new Exception("Syntax error:" + pathString);
                }
            }
            //else if(xy.Length == 3 && xy[2].StartsWith("meta:"))
            //{
            //    pColl.AddPoint(new PointM(xy[0].ToPlatformDouble(), xy[1].ToPlatformDouble(), xy[2]));
            //}
            else
            {
                throw new Exception("Syntax error:" + pathString);
            }

            if (newPoint != null && pointSrsId > 0)
            {
                newPoint.SrsId = pointSrsId;
            }
        }
        return pColl;
    }

    private static MultiPoint ReadMultiPoint(string pathString)
    {
        MultiPoint mp = new MultiPoint(ReadPointCollection(pathString));

        return mp;
    }

    private static Polyline ReadMultiLineString(List<string> pathStrings)
    {
        Polyline pline = new Polyline();
        foreach (string pathString in pathStrings)
        {
            Path path = new Path(ReadPointCollection(pathString));
            pline.AddPath(path);
        }
        return pline;
    }

    private static Polygon ReadMultiPolygon(List<string> pathStrings)
    {
        Polygon poly = new Polygon();

        foreach (string pathString in pathStrings)
        {
            Ring ring = new Ring(ReadPointCollection(pathString));
            poly.AddRing(ring);
        }
        return poly;
    }

    #endregion
}
