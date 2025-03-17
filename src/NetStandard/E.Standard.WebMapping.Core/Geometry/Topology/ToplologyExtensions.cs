using E.Standard.WebMapping.Core.Exceptions;
using E.Standard.WebMapping.Core.Geometry.Clipper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry.Topology;

static public class ToplologyExtensions
{
    #region Polygon

    public static IEnumerable<Path> ToPaths(this Polygon polygon)
    {
        List<Path> paths = new List<Path>();

        for (int r = 0, to_r = polygon.RingCount; r < to_r; r++)
        {
            var ring = polygon[r];
            ring.Close();
            paths.Add(ring.ToPath());
        }

        return paths;
    }

    public static Path ToPath(this Ring ring)
    {
        return new Geometry.Path(ring);
    }

    public static IEnumerable<Path> Clip(this Polygon polygon, Path clipee, double tolerance = 0D)
    {
        return polygon?.ToPaths().Split(clipee, LineSplitResultType.Even, tolerance: tolerance) ?? new Path[0];
    }

    public static IEnumerable<Polygon> Cut(this Polygon polygon, Path cutter, double tolerance = 1e-7)
    {
        if (cutter.IsSelfIntersecting())
        {
            throw new TopologyException("Die Schnittline darf sich nicht selber schneiden");
        }

        polygon.CloseAllRings();
        polygon.VerifyHoles();

        var polygonPaths = polygon.ToPaths().ToArray();
        IEnumerable<PointM3> intersectionPoints;

        #region Cutter parts

        //
        //  Schnittlinie mit Polygon verschneiden. Das Ergebnis ist die geschnitte Schnittlinie
        //  Das Ergebnis sind die Schnittpunte (PointM3)
        //
        //  Zusätzlich kommen noch die einzeln Schnittpunkte (IntersectionPoints) zurück:
        //  M ... RingIndex
        //  M2 .. Stationierung des Schnittpunktes auf den Polygon Ring 
        //  M3 .. Stationierung des Schnittpunktes auf der Schnittlinie
        //

        var cutterParts = polygonPaths.Split(cutter, out intersectionPoints, LineSplitResultType.All, tolerance: tolerance).
            Where(p =>
            {
                if (p.Length < tolerance)
                {
                    return false;
                }

                var midPoint = p.MidPoint2D;
                if (midPoint != null)  // Nur die Teile übernehmen, die auch wirklich über dem Polygon liegen
                {
                    return SpatialAlgorithms.Jordan(polygon, midPoint.X, midPoint.Y);
                }
                return false;
            });

        if (intersectionPoints.Count() == 0)
        {
            throw new TopologyNoResultException("Keine Schnittpunkte gefunden");
        }

        #endregion

        #region Polygon Rings aufgrund der Schnittpunte schneiden

        //
        // PolygonParts sind die einzelnen geschnitten Pfade des Poylgons.
        // Daraus sollt dann unten die neuen Polygone erzeugt werden (Polygonize)
        //
        var polygonParts = new List<Path>();

        for (int p = 0, to = polygonPaths.Length; p < to; p++)
        {
            var pathIntersectPoints = intersectionPoints.Where(i => p.Equals(i.M)).ToList();

            #region Remove identic neighbours and order by M2 ( = stat on polygon part)

            pathIntersectPoints = pathIntersectPoints.RemoveIdenticNeighbours<PointM3>(tolerance: tolerance).ToList();
            pathIntersectPoints.Sort(new PointM2Comparerer<double>());

            #endregion

            var partPolyline = new Polyline(polygonPaths[p]);
            double stat = 0;
            foreach (var pathIntersectPoint in pathIntersectPoints)
            {
                if (Math.Abs(stat - (double)pathIntersectPoint.M2) > tolerance)
                {
                    var clippedLine = SpatialAlgorithms.PolylineSplit(partPolyline, stat, (double)pathIntersectPoint.M2);
                    if (clippedLine != null && clippedLine.PathCount == 1)
                    {
                        polygonParts.Add(clippedLine[0]);
                    }
                }

                stat = (double)pathIntersectPoint.M2;
            }
            if (stat < partPolyline.Length)
            {
                var clippedLine = SpatialAlgorithms.PolylineSplit(partPolyline, stat, partPolyline.Length);
                polygonParts.Add(clippedLine[0]);
            }
        }

        #endregion

        // 
        // inklusive der geschnitten Schnittlinie
        //
        polygonParts.AddRange(cutterParts);

        List<Polygon> resultPolygons = new List<Polygon>(), untouchedPolygons = new List<Polygon>();

        #region Polygonize

        foreach (var newPolygon in polygonParts.Polygonize(tolerance))  // aus Pfaden Polygone erzeugen
        {
            if (polygon.Rings.Where(h => h.ToPolygon().Equals(newPolygon)).Count() == 0)  // Polygone, die sich nicht geändert haben hier noch nicht übernehmen -> könnten Löcher sein
            {
                resultPolygons.Add(newPolygon);
            }
            else
            {
                untouchedPolygons.Add(newPolygon);
            }
        }

        if (resultPolygons.Count == 0)
        {
            throw new TopologyNoResultException("Der Verschnitt liefert kein Ergebnis");
        }

        #region Unberührute Polygone dem nächstgelegen Schnittpolygon hinzufügen

        foreach (var untouchedPolygon in untouchedPolygons)
        {
            untouchedPolygon.VerifyHoles();

            for (int r = 0; r < untouchedPolygon.RingCount; r++)
            {
                //
                // Löcher (Holes) des ürsprünglichen Polygons hier nicht übernehmen
                // Holes werden im nächsten Schritt übernommen
                //

                if (polygon.Holes.Where(h => h.ToPolygon().Equals(untouchedPolygon[r].ToPolygon())).FirstOrDefault() != null)
                {
                    continue;
                }

                int resultIndex = 0;
                double dist = double.MaxValue;
                var outouchedRingPolygon = untouchedPolygon[r].ToPolygon();
                for (int i = 0; i < resultPolygons.Count(); i++)
                {
                    var ringDistance = resultPolygons[i].Distance2D(outouchedRingPolygon);
                    if (ringDistance < dist)
                    {
                        resultIndex = i;
                        dist = ringDistance;
                    }
                }

                resultPolygons[resultIndex].AddRing(untouchedPolygon[r]);
                untouchedPolygon.RemoveRing(r);
                r--;
            }
        }

        #endregion

        #region Testen ob einens der Polygone Donat-Hole eines anderen ist...

        //
        //  Die restlichen unberührten Polygone sind dann wahrscheinlich Löcher der geschnitten Polygone  
        //  Wenn nicht sind sie wahrscheinlich Löcher aus dem ursprünglichen Polygon die nach dem schneiden nicht mehr existieren
        //

        foreach (var resultPolygon in resultPolygons.OrderBy(p => p.Area).ToArray())
        {
            foreach (var untoucedPolygon in untouchedPolygons.ToArray())
            {
                var untouchedRings = untoucedPolygon.Rings;

                foreach (var resultRing in resultPolygon.Rings)
                {
                    for (int r = 0; r < untoucedPolygon.RingCount; r++)
                    {
                        var untouchedRing = untoucedPolygon[r];
                        if (SpatialAlgorithms.Jordan(resultRing, untouchedRing))
                        {
                            resultPolygon.AddRing(untouchedRing);
                            untoucedPolygon.RemoveRing(r);
                            r--;
                        }
                    }
                }
            }
        }

        #endregion

        //#region alle übrigen Untouced Polygone wieder zu einem Polygon zusammenfassen (das sind Multipart Inseln, die nicht angegriffen wurden)

        //Polygon restPolygon = null;
        //foreach (var untoucedPolygon in untouchedPolygons.Where(p => p.RingCount > 0).OrderByDescending(p => p.Area).ToList())
        //{
        //    bool isHole = untoucedPolygon.RingCount == 1 &&
        //                  polygon.Holes.Where(h => new Polygon(new Ring(h)).Equals(untoucedPolygon)).FirstOrDefault() != null;

        //    if (restPolygon == null)
        //    {
        //        if (!isHole)
        //            restPolygon = untoucedPolygon;
        //    }
        //    else
        //    {
        //        if (!isHole)
        //        {
        //            restPolygon.AddRings(untoucedPolygon.Rings);
        //        }
        //        else if (restPolygon.Rings.Where(r => SpatialAlgorithms.Jordan(r, untoucedPolygon[0])).FirstOrDefault() != null)
        //        {
        //            restPolygon.AddRing(untoucedPolygon[0]);
        //        }
        //    }
        //}
        //if (restPolygon != null)
        //    resultPolygons.Add(restPolygon);

        //#endregion

        #endregion

        return resultPolygons;
    }

    public static IEnumerable<Polygon> TryCut(this Polygon polygon, Path cutter, double tolerance = 1e7)
    {
        try
        {
            return polygon.Cut(cutter, tolerance);
        }
        catch (TopologyNoResultException)
        {
            return null;
        }
    }

    public static IEnumerable<Polygon> Clip(this Polygon polygon, Polygon clipPolygon, ClipType clipType)
    {
        polygon.CloseAllRings();
        polygon.VerifyHoles();

        using (var cancellationToken = new System.Threading.CancellationTokenSource())
        {
            var intersectedPolygons = polygon.Clip(clipPolygon, clipType, cancellationToken);

            return intersectedPolygons.Where(p => p.Area > 0D);
        }
    }

    // experimental
    public static IEnumerable<Polygon> Cut2_experimental(this Polygon polygon, Path cutter, double tolerance = 1e-7)
    {
        polygon.CloseAllRings();
        polygon.VerifyHoles();

        #region Polygon Intersects points are used for Snapping later

        var polygonPaths = polygon.ToPaths().ToArray();
        IEnumerable<PointM3> intersectionPoints;

        var cutterParts = polygonPaths.Split(cutter, out intersectionPoints, LineSplitResultType.All, tolerance: tolerance).
            Where(p =>
            {
                if (p.Length < tolerance)
                {
                    return false;
                }

                var midPoint = p.MidPoint2D;
                if (midPoint != null)  // Nur die Teile übernehmen, die auch wirklich über dem Polygon liegen
                {
                    return SpatialAlgorithms.Jordan(polygon, midPoint.X, midPoint.Y);
                }
                return false;
            });

        if (intersectionPoints.Count() == 0)
        {
            throw new Exception("Keine Schnittpunkte gefunden");
        }

        #endregion

        #region Cut with difference by thin polygon (cutter-buffer)

        var cutTolerance = 5; // 1e-2;
        using (var cancellationToken = new System.Threading.CancellationTokenSource())
        {
            var cutPolyline = new Polyline(cutter);
            var cutPolygon = cutPolyline.CalcBuffer(cutTolerance, cancellationToken);

            var newPolygons = polygon.Clip(cutPolygon, ClipType.ctDifference, cancellationToken);

            #endregion

            #region Snap new Points back to intersectionPoints points

            foreach (var newPolygon in newPolygons)
            {
                var diffSet = SpatialAlgorithms.ShapePoints(polygon, false).DifferenceSet(SpatialAlgorithms.ShapePoints(newPolygon, false), cutTolerance);
                diffSet.SnapTo(intersectionPoints);
            }

            #endregion

            return newPolygons;
        }
    }

    public static IEnumerable<Polygon> Polygonize(this IEnumerable<Path> paths, double tolerance)
    {
        var pathArray = paths.ToArray();

        List<Polygon> results = new List<Polygon>();

        List<PointM> nodes = new List<PointM>();
        List<Edge> edges = new List<Edge>();

        #region Determine unique nodes & edges

        //
        // Im ersten Schritt wird ein Graph mit knoten und Kanten erzeuge.
        // Über die Verfolgung innerhalb dieses Graphs werden alle möglichen Polygone wieder zusammen gebaut.
        //

        for (int i = 0, to = pathArray.Length; i < to; i++)
        {
            var path = pathArray[i];
            PointM p1 = nodes.Where(p => p.Equals(path[0], tolerance)).FirstOrDefault();
            if (p1 == null)
            {
                p1 = new PointM(path[0], nodes.Count);
                nodes.Add(p1);
            }

            PointM p2 = p2 = nodes.Where(p => p.Equals(path[path.PointCount - 1], tolerance)).FirstOrDefault();
            if (p2 == null)
            {
                p2 = new PointM(path[path.PointCount - 1], nodes.Count);
                nodes.Add(p2);
            }

            edges.Add(new Edge()
            {
                Index = i,
                Node1 = (int)p1.M,
                Node2 = (int)p2.M
            });
        }

        #endregion

        #region Calc Edge Combinations

        //
        //  Alle möglichen Kombination inerhalb des Graphs suchen, mit denen ein Polygon erzeugt werden kann
        //
        List<EdgeIndices> combinations = new List<EdgeIndices>();

        //
        //  Kombinationen werden pro Kante (Path) ermittelt
        // 
        foreach (var edge in edges)
        {
            var polygonEdges = new List<Edge>();
            polygonEdges.Add(edge);

            List<EdgeIndices> edgeCombinations = new List<EdgeIndices>();
            edge.PolygonizeGraph(polygonEdges, edges, edgeCombinations, edge.Node2);
            foreach (var edgeIndices in edgeCombinations)
            {
                // nur die Kombinatinen übernehmen, die noch nicht schon über eine andere Kante ermittelt wurden.
                if (!combinations.Contains(edgeIndices))
                {
                    combinations.Add(edgeIndices);
                }
            }
        }

        #endregion

        #region Stitch Paths and determinte ring with minium area -> new Polygon

        //
        // Polygone wieder zusammen bauen. 
        // 
        // Dazu wird die möglichen Kombinationen nach der ersten Kante groupiert. 
        // Danach werden alle Polygone für diese Kante aus den Pfanden zusammengestellt (edgeRings)
        // 
        // Übernommen wird pro Kante nur das Polygon mit der kleinsten Fläche
        //
        List<int> startEdgeIndices = combinations.Select(c => c.First()).Distinct().ToList();

        foreach (var startEdgeIndex in startEdgeIndices)
        {
            var edgeRings = combinations.Where(c => c.First() == startEdgeIndex)
                                        .Select(edgeIndices => pathArray.StitchPaths(edgeIndices, tolerance))
                                        .OrderBy(ring => ring.Area)
                                        .ToArray();

            if (edgeRings.Length > 0 && edgeRings.Select(r => r.Area).Sum() > 0)
            {
                results.Add(new Polygon(edgeRings[0]));
            }
        }

        #endregion

        return results;
    }

    #region Polygonize Helper

    private static void PolygonizeGraph(this Edge currentEdge, List<Edge> polygonEdges, List<Edge> edges, List<EdgeIndices> combinations, int currentEndNode)
    {
        int targetNodeIndex = polygonEdges[0].Node1;

        if (currentEdge == polygonEdges[0] &&
            currentEdge.Node2 == targetNodeIndex)
        {
            // Simple Ring
            combinations.Add(new EdgeIndices(polygonEdges.Select(e => e.Index).ToList()));
        }
        else if (currentEdge != polygonEdges[0] &&
                (currentEdge.ContainsNode(targetNodeIndex)))
        {
            // Finished
            combinations.Add(new EdgeIndices(polygonEdges.Select(e => e.Index).ToList()));
        }
        else
        {
            var connectedEdges = currentEdge.ConnectedEdges(edges).Where(e => e.ContainsNode(currentEndNode));
            foreach (var edge in connectedEdges)
            {
                var currentPolygonEdges = new List<Edge>(polygonEdges);

                if (currentPolygonEdges.Contains(edge))
                {
                    continue;
                }

                var nextNode = edge.ContainsNode(targetNodeIndex) ?
                    targetNodeIndex : edge.UnusedNode(currentPolygonEdges);

                if (nextNode < 0)
                {
                    continue;
                }

                currentPolygonEdges.Add(edge);

                edge.PolygonizeGraph(currentPolygonEdges, edges, combinations, nextNode);
            }
        }
    }

    private static bool Contains(this List<Edge> edges, Edge edge)
    {
        return edges.Where(e => e.Index == edge.Index).FirstOrDefault() != null;
    }

    private static int UnusedNode(this Edge edge, IEnumerable<Edge> edges)
    {
        if (edges.Where(e => e.Node1 == edge.Node1 || e.Node2 == edge.Node1).FirstOrDefault() == null)
        {
            return edge.Node1;
        }

        if (edges.Where(e => e.Node1 == edge.Node2 || e.Node2 == edge.Node2).FirstOrDefault() == null)
        {
            return edge.Node2;
        }

        return -1;
    }

    private static bool ContainsNode(this Edge edge, int nodeIndex)
    {
        return edge.Node1 == nodeIndex || edge.Node2 == nodeIndex;
    }

    private static IEnumerable<Edge> ConnectedEdges(this Edge edge, IEnumerable<Edge> edges)
    {
        return edges.Where(e => e.Index != edge.Index &&
                                (e.Node1 == edge.Node1 || e.Node1 == edge.Node2 ||
                                 e.Node2 == edge.Node1 || e.Node2 == edge.Node2));
    }

    private static bool Contains(this IEnumerable<EdgeIndices> bag, EdgeIndices indices)
    {
        return bag.Where(i => i.Equals(indices)).FirstOrDefault() != null;
    }

    private static Ring StitchPaths(this Path[] paths, EdgeIndices edgeIndices, double tolerance)
    {
        Ring result = new Ring();

        foreach (var edgeIndex in edgeIndices)
        {
            var path = paths[edgeIndex];
            if (path.PointCount == 0)
            {
                continue;
            }

            if (result.PointCount == 0)
            {
                result.AddPoints(path.ToArray());
            }
            else
            {
                var lastPoint = result[result.PointCount - 1];
                if (lastPoint.Equals(path[0], tolerance))
                {
                    result.AddPoints(path.ToArray(1));
                }
                else if (lastPoint.Equals(path[path.PointCount - 1], tolerance))
                {
                    result.AddPoints(path.ToArray(1, true));
                }
                else
                {
                    throw new Exception("Can't stitch path to ring");
                }
            }
        }

        return result;
    }

    private static bool IsSelfIntersecting(this Path path)
    {
        if (path == null)
        {
            return false;
        }

        for (int i = 0; i < path.PointCount - 2; i++)
        {
            Point p11 = path[i], p12 = path[i + 1];

            for (int j = i + 2; j < path.PointCount - 1; j++)
            {
                Point p21 = path[j], p22 = path[j + 1];

                if (SpatialAlgorithms.IntersectLine(p11, p12, p21, p22, true) != null)
                {
                    return true;
                }
            }
        }


        return false;
    }

    #endregion

    #endregion

    #region Polyline

    public static IEnumerable<Path> ToPaths(this Polyline polyline)
    {
        List<Path> paths = new List<Path>();

        for (int r = 0, to_r = polyline.PathCount; r < to_r; r++)
        {
            var path = polyline[r];
            paths.Add(path);
        }

        return paths;
    }

    public static IEnumerable<Polyline> Cut(this Polyline polyline, Path cutter, double tolerance = 1e-7)
    {
        var polylinePaths = polyline.ToPaths().ToArray();
        IEnumerable<PointM3> intersectionPoints;

        var cutterParths = polylinePaths.Split(cutter, out intersectionPoints, LineSplitResultType.All, tolerance);

        #region Pfade aufgrund der Schnittpunkte schneiden

        //
        // PolygonParts sind die einzelnen geschnitten Pfade des Poylgons.
        // Daraus sollt dann unten die neuen Polygone erzeugt werden (Polygonize)
        //

        var newPolylineParts = new List<Path>();
        var untouchedParts = new List<Path>();

        for (int p = 0, to = polylinePaths.Length; p < to; p++)
        {
            var pathIntersectPoints = intersectionPoints.Where(i => p.Equals(i.M)).ToList();

            #region Remove identic neighbours and order by M2 ( = stat on polygon part)

            pathIntersectPoints = pathIntersectPoints.RemoveIdenticNeighbours<PointM3>(tolerance: tolerance).ToList();
            pathIntersectPoints.Sort(new PointM2Comparerer<double>());

            #endregion

            if (pathIntersectPoints.Count() == 0)
            {
                untouchedParts.Add(polylinePaths[p]);
                continue;
            }

            var partPolyline = new Polyline(polylinePaths[p]);
            double stat = 0;
            foreach (var pathIntersectPoint in pathIntersectPoints)
            {
                if (Math.Abs(stat - (double)pathIntersectPoint.M2) > tolerance)
                {
                    var clippedLine = SpatialAlgorithms.PolylineSplit(partPolyline, stat, (double)pathIntersectPoint.M2);
                    if (clippedLine != null && clippedLine.PathCount == 1)
                    {
                        newPolylineParts.Add(clippedLine[0]);
                    }
                }

                stat = (double)pathIntersectPoint.M2;
            }
            if (stat < partPolyline.Length)
            {
                var clippedLine = SpatialAlgorithms.PolylineSplit(partPolyline, stat, partPolyline.Length);
                newPolylineParts.Add(clippedLine[0]);
            }
        }

        #endregion

        if (newPolylineParts.Count() == 0)
        {
            throw new TopologyNoResultException("Keine Änderungen festgestellt");
        }

        var newPolylines = newPolylineParts.Select(p => new Polyline(p)).ToArray();

        foreach (var untoucedPart in untouchedParts)
        {
            int resultIndex = 0;
            double dist = double.MaxValue;
            for (int i = 0; i < newPolylines.Count(); i++)
            {
                double d = newPolylines[i].Distance2D(new Polyline(untoucedPart));
                if (d < dist)
                {
                    resultIndex = i;
                    dist = d;
                }
            }

            newPolylines[resultIndex].AddPath(untoucedPart);
        }

        return newPolylines;
    }

    public static IEnumerable<Polyline> TryCut(this Polyline polyline, Path cutter, double tolerance = 1e-7)
    {
        try
        {
            return polyline.Cut(cutter, tolerance);
        }
        catch (TopologyNoResultException)
        {
            return null;
        }
    }

    public static (Polyline intersect, Polyline difference) Clip(this Polyline polyline, Polygon clipPolygon, double tolerance = 1e-7)
    {
        List<Path> intersectedPaths = new List<Path>();

        #region Intersect all Paths 

        var polylinePathArray = polyline.ToArray();

        for (int p = 0; p < polylinePathArray.Length; p++)
        {
            List<PointM3> intersectionPoints = new List<PointM3>();

            for (int r = 0; r < clipPolygon.RingCount; r++)
            {
                var ring = clipPolygon[r];
                ring.ClosePath();

                intersectionPoints.AddRange(polylinePathArray[p].Intersect(ring, p, tolerance: tolerance));
            }

            #region Remove identic neighbours and order by M2 ( = stat on path)

            intersectionPoints = intersectionPoints.RemoveIdenticNeighbours<PointM3>(tolerance: tolerance).ToList();
            intersectionPoints.Sort(new PointM2Comparerer<double>());

            #endregion

            #region Split clipee

            double stat = 0D;
            Polyline pathPolyline = new Polyline(polylinePathArray[p]);

            foreach (var intersectPoint in intersectionPoints)
            {
                if (Math.Abs(stat - (double)intersectPoint.M2) > 1e-7)
                {
                    var clippedLine = SpatialAlgorithms.PolylineSplit(pathPolyline, stat, (double)intersectPoint.M2);
                    if (clippedLine != null && clippedLine.PathCount == 1)
                    {
                        intersectedPaths.Add(clippedLine[0]);
                    }
                }

                stat = (double)intersectPoint.M2;
            }
            if (stat < pathPolyline.Length)
            {
                var clippedLine = SpatialAlgorithms.PolylineSplit(pathPolyline, stat, pathPolyline.Length);
                if (clippedLine != null && clippedLine.PathCount == 1)
                {
                    intersectedPaths.Add(clippedLine[0]);
                }
            }

            #endregion
        }

        #endregion

        List<Path> insideRings = new List<Path>();
        List<Path> outsideRings = new List<Path>();

        #region Check interesected Path, if inside or outside

        foreach (var intersectedPath in intersectedPaths)
        {
            double length = intersectedPath.Length;
            if (length < tolerance)
            {
                continue;
            }

            var midPoint = SpatialAlgorithms.PolylinePoint(new Polyline(intersectedPath), length / 2D);
            if (SpatialAlgorithms.Jordan(clipPolygon, midPoint.X, midPoint.Y))  // Inside
            {
                insideRings.Add(intersectedPath);
            }
            else  // outside
            {
                outsideRings.Add(intersectedPath);
            }
        }

        #endregion

        return (
            intersect: insideRings.Count > 0 ? new Polyline(insideRings) : null,
            difference: outsideRings.Count > 0 ? new Polyline(outsideRings) : null);
    }

    public static IEnumerable<Polyline> MergeToSinglePart(this IEnumerable<Polyline> polylines, CancellationTokenSource cts, double tolerance = 1e-7)
    {
        #region Collect and cut paths

        var paths = new List<ShapeWrapper<Path, int>>();
        int polylineIndex = 0;

        foreach (var polyline in polylines)
        {
            paths.AddRange(polyline.ToPaths()
                                   .Select(p => new ShapeWrapper<Path, int>(p, polylineIndex++)));
        }

        CutPaths(paths, cts, tolerance);

        #endregion

        #region Build Graph Edges

        // pathIndex: from to
        // 1:         0    1
        // 2:         1    2
        // 3:         3    1
        // ...

        var startEndPoints = new UniquePointList(tolerance);
        var graphEdges = new List<GraphEdge>();

        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i].Shape;

            int startIndex = startEndPoints.TryAddPoint(path[0]);
            int endIndex = startEndPoints.TryAddPoint(path[path.PointCount - 1]);

            graphEdges.Add(new GraphEdge(i, startIndex, endIndex));
        }

        #endregion

        #region Create Graph & find pathes from every leaf-node

        var graph = new Graph(graphEdges);
        var allGraphPaths = graph.FindAllPathFromLeafNodes();

        #endregion

        List<Polyline> result = new List<Polyline>();

        foreach (var graphPath in allGraphPaths)
        {
            var path = new Path();
            var polygonIndexes = new List<int>();

            for (int i = 0; i < graphPath.Count() - 1; i++)
            {
                var graphEdge = graphEdges.Where(e => e.From == graphPath[i] && e.To == graphPath[i + 1]).FirstOrDefault();

                if (graphEdge != null)
                {
                    path.AddPoints(paths[graphEdge.Index].Shape.CopyToAray(fromIndex: path.PointCount == 0 ? 0 : 1));
                    polygonIndexes.Add(paths[graphEdge.Index].Data);
                }
                else
                {
                    graphEdge = graphEdges.Where(e => e.To == graphPath[i] && e.From == graphPath[i + 1]).FirstOrDefault();

                    if (graphEdge != null)
                    {
                        path.AddPoints(paths[graphEdge.Index].Shape.CopyToAray(fromIndex: path.PointCount == 0 ? 0 : 1, reverse: true));
                        polygonIndexes.Add(paths[graphEdge.Index].Data);
                    }
                }
            }

            if (path.Length > tolerance &&
                polygonIndexes.Distinct().Count() == polylines.Count())  // all polylines must be included
            {
                result.Add(new Polyline(path));
            }
        }

        return result;
    }

    private static void CutPaths<TData>(List<ShapeWrapper<Path, TData>> paths, CancellationTokenSource cts, double tolerance)
    {
        for (int i = 0; i < paths.Count(); i++)
        {
            cts.Token.ThrowIfCancellationRequested();

            var polyline = new Polyline(paths[i].Shape);

            for (int j = 0; j < paths.Count(); j++)
            {
                if (j == i)
                {
                    continue;
                }

                cts.Token.ThrowIfCancellationRequested();

                var cuttedPolylines = polyline.TryCut(paths[j].Shape, tolerance);

                if (cuttedPolylines != null && cuttedPolylines.Count() > 1)
                {
                    TData pathData = paths[i].Data;
                    paths.RemoveAt(i);
                    foreach (var cuttedPolyline in cuttedPolylines)
                    {
                        paths.AddRange(cuttedPolyline.ToPaths()
                                                     .Where(p => p.Length > tolerance)
                                                     .Select(p => new ShapeWrapper<Path, TData>(p, pathData)));
                    }

                    CutPaths(paths, cts, tolerance);
                    // end here
                    return;
                }
            }
        }
    }

    #endregion

    public static IEnumerable<Path> Split(this IEnumerable<Path> paths,
                                          Path clipee,
                                          LineSplitResultType resultType = LineSplitResultType.All,
                                          double tolerance = 1e-7)
    {
        IEnumerable<PointM3> dummy;

        return paths.Split(clipee, out dummy, resultType, tolerance: tolerance);
    }

    public static IEnumerable<Path> Split(this IEnumerable<Path> paths,
                                          Path clipee,
                                          out IEnumerable<PointM3> iPoints,
                                          LineSplitResultType resultType = LineSplitResultType.All,
                                          double tolerance = 1e-7)
    {
        List<Path> result = new List<Path>();

        #region Snap Clipee to paths

        clipee = new Path(clipee.Snap2DAndClean(paths, tolerance));

        #endregion

        #region Determine Points

        List<PointM3> intersectionPoints = new List<PointM3>();

        var pathArray = paths.ToArray();
        for (int i = 0, to = pathArray.Length; i < to; i++)
        {
            intersectionPoints.AddRange(pathArray[i].Intersect(clipee, i, tolerance: tolerance));
        }

        #endregion

        #region Remove identic neighbours and order by M3 ( = stat on clipee)

        intersectionPoints = intersectionPoints.RemoveIdenticNeighbours<PointM3>(tolerance: tolerance).ToList();
        intersectionPoints.Sort(new PointM3Comparerer<double>());

        #endregion

        #region Split clipee

        double stat = 0D;
        int counter = 0;
        Polyline clipeeLine = new Polyline(clipee);

        foreach (var intersectPoint in intersectionPoints)
        {
            if (Math.Abs(stat - (double)intersectPoint.M3) > 1e-7)
            {
                var clippedLine = SpatialAlgorithms.PolylineSplit(clipeeLine, stat, (double)intersectPoint.M3);
                if (clippedLine != null && clippedLine.PathCount == 1)
                {

                    if (resultType == LineSplitResultType.All ||
                        (resultType == LineSplitResultType.Even && counter % 2 == 1) ||
                        (resultType == LineSplitResultType.Odd && counter % 2 == 0))
                    {
                        result.Add(clippedLine[0]);
                    }
                }
            }

            counter++;
            stat = (double)intersectPoint.M3;
        }
        if (stat < clipeeLine.Length)
        {
            var clippedLine = SpatialAlgorithms.PolylineSplit(clipeeLine, stat, clipeeLine.Length);
            if (clippedLine != null && clippedLine.PathCount == 1)
            {
                if (resultType == LineSplitResultType.All ||
                    (resultType == LineSplitResultType.Even && counter % 2 == 1) ||
                    (resultType == LineSplitResultType.Odd && counter % 2 == 0))
                {
                    result.Add(clippedLine[0]);
                }
            }
        }

        #endregion

        iPoints = intersectionPoints;
        return result;
    }

    public static IEnumerable<PointM3> Intersect(this Path path1, Path path2, object M = null, double tolerance = 1e-7)
    {
        List<PointM3> points = new List<PointM3>();

        if (path1 == null || path2 == null)
        {
            return points;
        }

        int pointCount1 = path1.PointCount,
            pointCount2 = path2.PointCount;

        if (pointCount1 == 0 || pointCount2 == 0)
        {
            return points;
        }

        double stat1 = 0;
        for (int t1 = 0; t1 < pointCount1 - 1; t1++)
        {
            Point p11 = path1[t1], p12 = path1[t1 + 1];

            double stat2 = 0;
            for (int t2 = 0; t2 < pointCount2 - 1; t2++)
            {
                Point p21 = path2[t2], p22 = path2[t2 + 1];

                var point = SpatialAlgorithms.IntersectLine(p11, p12, p21, p22, true, tolerance: tolerance);
                if (point != null)
                {
                    points.Add(new PointM3(point, M, stat1 + p11.Distance2D(point), stat2 + p21.Distance2D(point)));
                }

                stat2 += p21.Distance2D(p22);
            }
            stat1 += p11.Distance2D(p12);
        }

        return points;
    }

    public static IEnumerable<T> RemoveIdenticNeighbours<T>(this IEnumerable<T> points, double tolerance = 1e-7) where T : Point
    {
        var pointList = points.ToList();

        for (int i = 0; i < pointList.Count - 1; i++)
        {
            T p1 = pointList[i], p2 = pointList[i + 1];
            if (p1.Distance2D(p2) < tolerance)
            {
                pointList.Remove(p2);
                i--; // continue with same point
            }
        }

        return pointList;
    }

    //public static void Snap2DToAndClean(this PointCollection pointCollection, IEnumerable<Point> snapPoints, double tolerance)
    //{
    //    var points = pointCollection.ToArray();
    //    for (int i = 0; i < points.Length; i++)
    //    {
    //        foreach (var snapPoint in snapPoints)
    //        {
    //            if (snapPoint.Distance2D(points[i]) < tolerance)
    //            {
    //                points[i].X = snapPoint.X;
    //                points[i].Y = snapPoint.Y;
    //            }
    //        }
    //    }

    //    pointCollection.Clear();
    //    pointCollection.AddPoints(points.RemoveIdenticNeighbours(tolerance));
    //}

    public static PointCollection Snap2DAndClean(this PointCollection pointCollection, IEnumerable<Path> paths, double tolerance)
    {
        var points = SpatialAlgorithms.ShapePoints(pointCollection, true);

        foreach (var point in points)
        {
            foreach (var path in paths)
            {
                var polyLine = new Polyline(path);
                double dist, stat;

                SpatialAlgorithms.Point2PolylineDistance(polyLine, point, out dist, out stat, tolerance);
                if (dist < tolerance)
                {
                    var polyLinePoint = SpatialAlgorithms.PolylinePoint(polyLine, stat);

                    if (polyLinePoint != null)
                    {
                        point.X = polyLinePoint.X;
                        point.Y = polyLinePoint.Y;

                        break;
                    }
                }
            }
        }

        return new PointCollection(points.RemoveIdenticNeighbours(tolerance));
    }

    public static Path Extend(this Path path, double dist, bool onStart = true, bool onEnd = true)
    {
        var extendedPath = new Path(SpatialAlgorithms.ShapePoints(path, true));

        int pointCount = extendedPath.PointCount;
        if (pointCount < 2)
        {
            return extendedPath;
        }

        if (onStart)
        {
            double dx = extendedPath[0].X - extendedPath[1].X,
                   dy = extendedPath[0].Y - extendedPath[1].Y;

            double len = Math.Sqrt(dx * dx + dy * dy);

            extendedPath[0].X = extendedPath[1].X + dx * (len + dist) / len;
            extendedPath[0].Y = extendedPath[1].Y + dy * (len + dist) / len;
        }
        if (onEnd)
        {
            double dx = extendedPath[pointCount - 1].X - extendedPath[pointCount - 2].X,
                   dy = extendedPath[pointCount - 1].Y - extendedPath[pointCount - 2].Y;

            double len = Math.Sqrt(dx * dx + dy * dy);

            extendedPath[pointCount - 1].X = extendedPath[pointCount - 2].X + dx * (len + dist) / len;
            extendedPath[pointCount - 1].Y = extendedPath[pointCount - 2].Y + dy * (len + dist) / len;
        }

        return extendedPath;
    }

    #region Points

    public static IEnumerable<Point> DifferenceSet(this IEnumerable<Point> set, IEnumerable<Point> subset, double tolerance)
    {
        List<Point> diffSet = new List<Point>();

        foreach (var subsetPoint in subset)
        {
            if (set.Where(p => p.Distance2D(subsetPoint) <= tolerance).Count() == 0)
            {
                diffSet.Add(subsetPoint);
            }
        }

        return diffSet;
    }

    public static void SnapTo(this IEnumerable<Point> points, IEnumerable<Point> snapPoints)
    {
        foreach (var point in points)
        {
            double snapDist = double.MaxValue;
            Point snapTo = null;

            foreach (var snapPoint in snapPoints)
            {
                var dist = point.Distance2D(snapPoint);
                if (dist < snapDist)
                {
                    snapDist = dist;
                    snapTo = snapPoint;
                }
            }

            if (snapTo != null)
            {
                point.X = snapTo.X;
                point.Y = snapTo.Y;
            }
        }
    }

    #endregion

    #region HelperClasses

    private class Edge
    {
        public int Index { get; set; }
        public int Node1 { get; set; }
        public int Node2 { get; set; }
    }

    private class EdgeIndices : List<int>
    {
        public EdgeIndices()
            : base()
        {

        }

        public EdgeIndices(IEnumerable<int> collection)
            : base(collection)
        {

        }

        public override bool Equals(object obj)
        {
            if (!(obj is EdgeIndices))
            {
                return false;
            }

            var indices = (EdgeIndices)obj;

            if (indices.Count != this.Count)
            {
                return false;
            }

            foreach (var index in indices)
            {
                if (!this.Contains(index))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion
}
