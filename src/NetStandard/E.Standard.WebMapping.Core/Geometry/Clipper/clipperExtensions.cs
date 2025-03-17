using System.Collections.Generic;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry.Clipper;

using ClipperPolygons = List<List<IntPoint>>;

public static class ClipperExtensions
{
    #region ToClipper

    static public ClipperPolygons ToClipperPolygons(this Polygon polygon, CancellationTokenSource cts)
    {
        Clipper c = new Clipper();
        ClipperPolygons polygons = new ClipperPolygons(), result = new ClipperPolygons();

        for (int r = 0, r_to = polygon.RingCount; r < r_to; r++)
        {
            var ring = polygon[r];
            ring.Close();

            var ringPoints = new List<IntPoint>();
            for (int p = 0, p_to = ring.PointCount; p < p_to; p++)
            {
                ringPoints.Add(ring[p].ToClipperPointer());
            }
            polygons.Add(ringPoints);
        }

        c.AddPaths(polygons, PolyType.ptSubject, true);
        if (c.Execute(ClipType.ctUnion, result, cts) == true)
        {
            return result;
        }

        return null;
    }

    static public int Acc = 1000;
    static public IntPoint ToClipperPointer(this Point point)
    {
        return new IntPoint((long)(point.X * Acc), (long)(point.Y * Acc));
    }

    static public ClipperPolygons Buffer(this ClipperPolygons polygons, double distance, CancellationTokenSource cts)
    {
        ClipperPolygons result = new ClipperPolygons();

        ClipperOffset co = new ClipperOffset();
        co.AddPaths(polygons, JoinType.jtRound, EndType.etClosedPolygon, cts);
        co.Execute(ref result, distance * Acc, cts);

        return result;
    }

    #endregion

    #region From Clipper

    static public Polygon ToPolygon(this ClipperPolygons clipperPolygons)
    {
        Polygon result = new Polygon();

        foreach (var clipperRing in clipperPolygons)
        {
            var ring = new Ring();

            foreach (var clipperPoint in clipperRing)
            {
                ring.AddPoint(clipperPoint.ToPoint());
            }
            result.AddRing(ring);
        }

        return result;
    }

    static public Point ToPoint(this IntPoint clipperPoint)
    {
        return new Point((double)clipperPoint.X / Acc, (double)clipperPoint.Y / Acc);
    }

    #endregion

    #region Operations

    static public Polygon Merge(this List<Polygon> polygons, CancellationTokenSource cts)
    {
        Clipper c = new Clipper();

        foreach (var polygon in polygons)
        {
            var clipperPolygons = polygon.ToClipperPolygons(cts);
            if (clipperPolygons != null)
            {
                c.AddPaths(clipperPolygons, PolyType.ptSubject, true);
            }
            //else  // => für Testzwecke => wann kann das passieren usw.
            //{
            //    throw new Exception("???");
            //}
        }

        ClipperPolygons result = new ClipperPolygons();
        if (c.Execute(ClipType.ctUnion, result, cts, PolyFillType.pftPositive) == true)
        {
            return result.ToPolygon();
        }

        return null;
    }

    static public Polygon Clip(this Polygon clippee, Envelope clipper, CancellationTokenSource cts)
    {
        var clippeePolygons = clippee?.ToClipperPolygons(cts);
        var clipperPolygons = clipper?.ToPolygon()?.ToClipperPolygons(cts);

        if (clippeePolygons == null || clipperPolygons == null)
        {
            return clippee;
        }

        var c = new Clipper();
        c.AddPaths(clippeePolygons, PolyType.ptSubject, true);
        c.AddPaths(clipperPolygons, PolyType.ptClip, true);

        ClipperPolygons result = new ClipperPolygons();
        if (c.Execute(ClipType.ctIntersection, result, cts) == true)
        {
            return result.ToPolygon();
        }

        return clippee;
    }

    static public IEnumerable<Polygon> Clip(this Polygon polygon, Polygon cutter, ClipType clipType, CancellationTokenSource cts)
    {
        var cutPolygon = polygon?.ToClipperPolygons(cts);
        var cutterPolygon = cutter?.ToClipperPolygons(cts);

        if (cutPolygon == null || cutterPolygon == null)
        {
            return null;
        }

        var c = new Clipper();
        c.AddPaths(cutPolygon, PolyType.ptSubject, true);
        c.AddPaths(cutterPolygon, PolyType.ptClip, true);

        List<Ring> resultRings = new List<Ring>();

        ClipperPolygons clipperResult = new ClipperPolygons();
        if (c.Execute(clipType, clipperResult, cts) == true)
        {
            resultRings.AddRange(clipperResult.ToPolygon().Rings);
        }

        var clippedPolygons = new List<Polygon>();

        clippedPolygons.Add(new Polygon(resultRings));

        //var resultPolygon = new Polygon(resultRings);
        //resultPolygon.VerifyHoles();
        //List<Hole> holes = new List<Hole>();

        //for (int r = 0; r < resultPolygon.RingCount; r++)
        //{
        //    var ring = resultPolygon[r];
        //    if (ring is Hole)
        //    {
        //        holes.Add((Hole)ring);
        //    }
        //    else
        //    {
        //        clippedPolygons.Add(new Polygon(ring));
        //    }
        //}

        //foreach(var hole in holes)
        //{
        //    foreach(var clippedPolygon in clippedPolygons)
        //    {
        //        if(SpatialAlgorithms.Jordan(clippedPolygon.Rings.First(), hole))
        //        {
        //            clippedPolygon.AddRing(hole);
        //            continue;
        //        }
        //    }
        // }

        return clippedPolygons;
    }

    #endregion
}
