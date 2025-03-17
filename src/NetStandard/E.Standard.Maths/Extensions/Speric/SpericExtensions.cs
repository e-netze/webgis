using E.Standard.Maths.Primitives;
using System;
using System.Collections.Generic;

namespace E.Standard.Maths.Extensions.Speric;

static public class SpericExtensions
{
    static public IEnumerable<GeoLocation> ToSphericCirclePoints(this GeoLocation center, double radius_meter, double stepWidth = 0.01)
    {
        List<GeoLocation> circlePoints = new List<GeoLocation>();

        double alpha = radius_meter / 6371000, lat = 90.0 - center.Latitude, lng = 90.0 - center.Longitude;

        double sin_a = Math.Sin(alpha), cos_a = Math.Cos(alpha);
        double sin_lat = Math.Sin(lat * Math.PI / 180.0), cos_lat = Math.Cos(lat * Math.PI / 180.0);
        double sin_lng = Math.Sin(lng * Math.PI / 180.0), cos_lng = Math.Cos(lng * Math.PI / 180.0);

        for (double t = 0; t < System.Math.PI * 2.0; t += stepWidth)
        {
            // https://math.stackexchange.com/questions/643130/circle-on-sphere

            double x = (sin_a * cos_lat * cos_lng) * Math.Cos(t) + (sin_a * sin_lng) * Math.Sin(t) - (cos_a * sin_lat * cos_lng);
            double y = -(sin_a * cos_lat * sin_lng) * Math.Cos(t) + (sin_a * cos_lng) * Math.Sin(t) + (cos_a * sin_lat * sin_lng);
            double z = (sin_a * sin_lat) * Math.Cos(t) + cos_a * cos_lat;

            double lat_ = Math.Asin(z) * 180.0 / Math.PI;
            double lng_ = -Math.Atan2(x, y) * 180.0 / Math.PI;

            circlePoints.Add(new GeoLocation(lng_, lat_));
        }

        return circlePoints;
    }
}
