using E.Standard.Gpx.Schema;
using E.Standard.WebMapping.Core.Geometry;
using System;

namespace E.Standard.Gpx;

public class GpxHelper
{
    #region FromShape
    static public gpxType FromShape(Shape shape, SpatialReference from, SpatialReference to)
    {
        gpxType gpx = new gpxType();
        gpx.version = "1.1";
        #region Metadata
        gpx.metadata = new metadataType();
        #endregion

        GeometricTransformer.Transform2D(shape, from.Proj4, !from.IsProjective, to.Proj4, !to.IsProjective);

        if (shape is Polyline)
        {
            FromPolyline(gpx, (Polyline)shape);
        }
        else if (shape is Polygon)
        {
            FromPolygon(gpx, (Polygon)shape);
        }
        return gpx;
    }

    static public wptType FromPoint(Point point)
    {
        if (point == null)
        {
            return null;
        }

        wptType wpt = new wptType();
        if (point != null)
        {
            wpt.lon = (decimal)point.X;
            wpt.lat = (decimal)point.Y;
        }
        return wpt;
    }
    static private trksegType FromPath(trkType trkType, Path path)
    {
        trksegType trkseg = new trksegType();
        trkseg.trkpt = new wptType[path.PointCount];

        for (int i = 0; i < path.PointCount; i++)
        {
            trkseg.trkpt[i] = FromPoint(path[i]);
        }

        return trkseg;
    }
    static private trksegType FromRing(trkType trkType, Ring ring)
    {
        ring.Close();
        return FromPath(trkType, ring);
    }
    static public trkType FromPolyline(gpxType gpxType, Polyline polyline)
    {
        if (polyline == null)
        {
            return null;
        }

        trkType trk = CreateTrack(gpxType);

        trk.trkseg = new trksegType[polyline.PathCount];
        for (int i = 0; i < polyline.PathCount; i++)
        {
            trk.trkseg[i] = FromPath(trk, polyline[i]);
        }

        //AppendTrack(gpxType, trk);
        return trk;
    }

    static private trkType FromPolygon(gpxType gpxType, Polygon polygon)
    {
        if (polygon == null)
        {
            return null;
        }

        trkType trk = CreateTrack(gpxType);

        trk.trkseg = new trksegType[polygon.RingCount];
        for (int i = 0; i < polygon.RingCount; i++)
        {
            trk.trkseg[i] = FromRing(trk, polygon[i]);
        }

        //AppendTrack(gpxType, trk);
        return trk;
    }

    #region Helper
    static private trkType CreateTrack(gpxType gpxType)
    {
        trkType trk = new trkType();
        trk.name = gpxType.trk == null ? "Track 1" : "Track " + (gpxType.trk.Length + 1).ToString();
        return trk;
    }
    static public void AppendTrack(gpxType gpxType, trkType trk)
    {
        if (gpxType.trk == null)
        {
            gpxType.trk = new trkType[] { trk };
        }
        else
        {
            trkType[] trks = gpxType.trk;
            Array.Resize<trkType>(ref trks, gpxType.trk.Length + 1);
            trks[trks.Length - 1] = trk;
            gpxType.trk = trks;
        }
    }
    #endregion

    #endregion

    #region ToShape

    static public PointCollection ToPointCollection(gpxType gpx, string type, SpatialReference from = null, SpatialReference to = null)
    {
        PointCollection pColl = new PointCollection();

        if (type == "wpt")
        {
            if (gpx.wpt != null)
            {
                foreach (wptType wpt in gpx.wpt)
                {
                    pColl.AddPoint(new PointM((double)wpt.lon, (double)wpt.lat, wpt.name));
                }
            }
        }
        else if (type.StartsWith("rte:"))
        {
            int index = int.Parse(type.Split(':')[1]);
            if (gpx.rte != null && gpx.rte.Length > index)
            {
                foreach (wptType wpt in gpx.rte[index].rtept)
                {
                    pColl.AddPoint(new Point((double)wpt.lon, (double)wpt.lat));
                }
            }
        }
        else if (type.StartsWith("trk:"))
        {
            int index = int.Parse(type.Split(':')[1]);
            int seg = int.Parse(type.Split(':')[2]);

            if (gpx.trk != null && gpx.trk.Length > index &&
                gpx.trk[index].trkseg != null && gpx.trk[index].trkseg.Length > seg)
            {
                foreach (wptType wpt in gpx.trk[index].trkseg[seg].trkpt)
                {
                    pColl.AddPoint(new Point((double)wpt.lon, (double)wpt.lat));
                }
            }
        }

        if (from != null && to != null)
        {
            GeometricTransformer.Transform2D(pColl, from.Proj4, !from.IsProjective, to.Proj4, !to.IsProjective);
        }
        return pColl;
    }

    #endregion
}
