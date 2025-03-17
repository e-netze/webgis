using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using System;

namespace E.Standard.WebMapping.Core;

public class SphericHelper
{
    private static double _R = 6378137.0;

    public static double CalcScale(IMap map, Envelope extent = null)
    {
        try
        {
            if (extent == null)
            {
                extent = map.Extent;
            }

            double W, H;

            if (map.SpatialReference != null && map.SpatialReference.Id == 3857) // WebMercator
            {
                Point w1 = WebMercator_2_WGS84(new Point(extent.MinX, extent.CenterPoint.Y)),
                      w2 = WebMercator_2_WGS84(new Point(extent.MaxX, extent.CenterPoint.Y)),
                      h1 = WebMercator_2_WGS84(new Point(extent.CenterPoint.X, extent.MinY)),
                      h2 = WebMercator_2_WGS84(new Point(extent.CenterPoint.X, extent.MaxY));

                //W = SphericDistance(w1, w2);
                // Distanz am Breitenkreis
                Point c = WebMercator_2_WGS84(extent.CenterPoint);
                double R_phi = _R * Math.Cos(c.Y * Math.PI / 180.0);
                W = Math.Abs(w1.X - w2.X) * Math.PI / 180.0 * R_phi;

                H = SphericDistance(h1, h2);
            }
            else
            {

                W = map.SpatialReference == null || map.SpatialReference.IsProjective ? extent.Width : extent.SphericWidth(_R);
                H = map.SpatialReference == null || map.SpatialReference.IsProjective ? extent.Height : extent.SphericHeight(_R);

            }

            double dpm = map.Dpi / 0.0254;
            double mapScale = Math.Max(
                               W / map.ImageWidth * dpm,
                               H / map.ImageHeight * dpm);

            return mapScale;
        }
        catch
        {
            return double.NaN;
        }
    }

    public static Envelope CalcExtent(IMap map, Geometry.Point center, double scale)
    {
        double dpm = map.Dpi / 0.0254;

        double w, h;

        w = (map.ImageWidth / dpm) * scale;
        h = (map.ImageHeight / dpm) * scale;

        if (map.SpatialReference != null && map.SpatialReference.Id == 3857)
        {
            Geometry.Point c = WebMercator_2_WGS84(center);

            double rPhi = _R * Math.Cos(c.Y * Math.PI / 180.0);

            Geometry.Point w1 = new Geometry.Point(c.X - (w / 2D / rPhi) * 180.0 / Math.PI, c.Y);
            Geometry.Point w2 = new Geometry.Point(c.X + (w / 2D / rPhi) * 180.0 / Math.PI, c.Y);
            Geometry.Point h1 = new Geometry.Point(c.X, c.Y - (h / 2D / _R) * 180.0 / Math.PI);
            Geometry.Point h2 = new Geometry.Point(c.X, c.Y + (h / 2D / _R) * 180.0 / Math.PI);

            if (w1.X < -180D)
            {
                w1.X = -180D;
            }
            else if (w1.X > 180D)
            {
                w1.X = 180D;
            }

            if (w2.X < -180D)
            {
                w2.X = -180D;
            }
            else if (w2.X > 180D)
            {
                w2.X = 180D;
            }

            if (h1.Y < -85D)
            {
                h1.Y = -85D;
            }
            else if (h1.Y > 85D)
            {
                h1.Y = 85D;
            }

            if (h2.Y < -85D)
            {
                h2.Y = -85D;
            }
            else if (h2.Y > 85D)
            {
                h2.Y = 85D;
            }

            w1 = WGS84_2_WebMercator(w1);
            w2 = WGS84_2_WebMercator(w2);
            h1 = WGS84_2_WebMercator(h1);
            h2 = WGS84_2_WebMercator(h2);

            w = Math.Abs(w2.X - w1.X);
            h = Math.Abs(h2.Y - h1.Y);

            ////w = 2.0 * SphericDistance(c, w2);
            //// Distanz am Breitenkreis
            //w = 2.0 * Math.Abs(c.X - w2.X) * Math.PI / 180.0 * rPhi;
            //h = 2.0 * SphericDistance(c, h2);

        }
        else
        {
            if (map.SpatialReference != null && !map.SpatialReference.IsProjective)
            {
                //double phi = ToRad(center.Y);
                //w = (w / (_R * Math.Cos(phi))) * 180.0 / Math.PI;

                // ******************************************************************************************************************************************************************************
                // Hier nicht mehr mit COS(phi) multiplizieren, weil es sonst nicht mehr zusammen stimmt, wenn 
                // man den Dienst als WMS einbindet. Beim Zoom To und Umrechnen ergibt sich ein falscher Extent und erscheint so, als ob man beim immer ein leicht verschobenes Ergebnis bekommt
                // ******************************************************************************************************************************************************************************

                w = (w / _R) * 180.0 / Math.PI;
                h = (h / _R) * 180.0 / Math.PI;
            }
        }

        return new Envelope(center.X - w / 2, center.Y - h / 2, center.X + w / 2, center.Y + h / 2);
    }

    public static double CalcShericalScale(IMap map, double scale)
    {
        try
        {
            if (map.SpatialReference != null && map.SpatialReference.Id == 3857) // WebMercator
            {
                Point c = WebMercator_2_WGS84(map.Extent.CenterPoint);

                scale *= Math.Cos(c.Y * Math.PI / 180.0);
            }
            else if (map.SpatialReference != null && map.SpatialReference.IsProjective == false)
            {
                scale *= Math.Cos(map.Extent.CenterPoint.Y * Math.PI / 180.0);
            }

            return scale;
        }
        catch
        {
            return double.NaN;
        }
    }

    public static double CalcTileResolution(IMap map)
    {
        try
        {
            double resolution = map.Resolution;

            //if (map.SpatialReference != null && map.SpatialReference.Id == 3857) // WebMercator
            //{
            //    Geometry.Point c = WebMercator_2_WGS84(map.Extent.CenterPoint);
            //    resolution *= Math.Cos(c.Y * Math.PI / 180.0);
            //}
            //else if (map.SpatialReference != null && map.SpatialReference.IsProjective == false)
            //{
            //    resolution /= Math.Cos(map.Extent.CenterPoint.Y * Math.PI / 180.0);
            //}

            return resolution;
        }
        catch
        {
            return double.NaN;
        }
    }

    public static bool IsSpheric(IMap map)
    {
        if (map.SpatialReference != null) // WebMercator
        {
            return map.SpatialReference.Id == 3857 || map.SpatialReference.IsProjective == false;
        }
        return false;
    }

    #region Web Mercator

    static internal Point WebMercator_2_WGS84(Point p)
    {
        double x = p.X, y = p.Y;

        if ((Math.Abs(x) > 20037508.3427892) || (Math.Abs(y) > 20037508.3427892))
        {
            return null;
        }

        double num3 = x / 6378137.0;
        double num4 = num3 * 57.295779513082323;
        double num6 = num4;
        double num7 = 1.5707963267948966 - (2.0 * Math.Atan(Math.Exp((-1.0 * y) / 6378137.0)));
        return new Geometry.Point(num6, num7 * 57.295779513082323);
    }

    static private Geometry.Point WGS84_2_WebMercator(Geometry.Point p)
    {
        double lon = p.X, lat = p.Y;

        if ((Math.Abs(lon) > 180 || Math.Abs(lat) > 90))
        {
            return null;
        }

        double num = lon * 0.017453292519943295;
        double x = 6378137.0 * num;
        double a = lat * 0.017453292519943295;

        return new Geometry.Point(x, 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a))));
    }

    #endregion

    #region Spherical Methods

    public static double SphericDistance(Point p1, Point p2)
    {
        double dLat = ToRad(p2.Y - p1.Y);
        double dLon = ToRad(p2.X - p1.X);
        double a =
          Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
          Math.Cos(ToRad(p1.Y)) * Math.Cos(ToRad(p2.Y)) *
          Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var d = _R * c; // Distance in m
        return d;
    }

    #endregion

    #region Helper

    private static double ToRad(double x)
    {
        return x * Math.PI / 180.0;
    }

    #endregion
}
