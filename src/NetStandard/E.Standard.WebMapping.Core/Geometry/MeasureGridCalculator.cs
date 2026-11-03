using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace E.Standard.WebMapping.Core.Geometry;

// MapMarkup measure tool 'measure_grid' (Messgitter): same geometry as L.MeasureGrid.calc (webgis-graphics.measure_grid.js)
//   align false => vertex 0,1 => base edge (origin = vertex 0, direction of the columns), vertex 2 => depth (rows)
//   align true  => vertex 0,1 => opposite corners, axis-parallel, lines on multiples of the cell width/height (calc crs)
// input coordinates are WGS84 [lng, lat], the grid is calculated in the calc crs (meters)
// without a (known) calc crs a local equirectangular approximation is used (like L.ViewAngle.toCalc)
static public class MeasureGridCalculator
{
    // more cells => only the frame (print), no cells (export), same limit as L.MeasureGrid.maxCells
    public const int MaxCells = 2500;

    static public int RequiredVertexCount(bool align) => align ? 2 : 3;

    public class Grid : IDisposable
    {
        private readonly Func<double, double, Point> _toLngLat;
        private readonly IDisposable _transformer;

        internal Grid(Func<double, double, Point> toLngLat, IDisposable transformer)
        {
            _toLngLat = toLngLat;
            _transformer = transformer;
        }

        public void Dispose() => _transformer?.Dispose();

        public int CrsId { get; init; }
        public bool Aligned { get; init; }
        public Point Origin { get; init; }
        public double Ux { get; init; }
        public double Uy { get; init; }
        public double Vx { get; init; }
        public double Vy { get; init; }
        public double CellWidth { get; init; }
        public double CellHeight { get; init; }
        public int Cols { get; init; }
        public int Rows { get; init; }

        public int Cells => Cols * Rows;
        public bool TooMany => Cells > MaxCells;

        // c, r: cell units (may be fractional)
        public Point Corner(double c, double r)
            => new Point(Origin.X + Ux * c * CellWidth + Vx * r * CellHeight,
                         Origin.Y + Uy * c * CellWidth + Vy * r * CellHeight);

        public Point LngLat(double c, double r)
        {
            var corner = Corner(c, r);
            return _toLngLat(corner.X, corner.Y);
        }

        // aligned: coordinate of the grid line c (X) resp. r (Y)
        public double LineX(int c) => Origin.X + Ux * c * CellWidth;
        public double LineY(int r) => Origin.Y + Vy * r * CellHeight;
    }

    static public Grid Calc(IList<Point> lngLats, int calcCrsId, double cellWidth, double cellHeight, bool align)
    {
        int required = RequiredVertexCount(align);
        if (lngLats is null || lngLats.Count < required || !(cellWidth > 0) || !(cellHeight > 0))
        {
            return null;
        }

        var input = lngLats.Take(required).Select(p => new Point(p.X, p.Y)).ToList();
        var sRef = calcCrsId > 0 ? CoreApiGlobals.SRefStore?.SpatialReferences?.ById(calcCrsId) : null;

        List<Point> points;
        Func<double, double, Point> toLngLat;
        GeometricTransformerPro transformer = null;

        if (sRef != null)
        {
            transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, calcCrsId);
            points = input.Select(p =>
            {
                var point = new Point(p.X, p.Y);
                transformer.Transform(point);
                return point;
            }).ToList();

            toLngLat = (x, y) =>
            {
                var point = new Point(x, y);
                transformer.InvTransform(point);
                return point;
            };
        }
        else
        {
            calcCrsId = 0;
            double kx = 111320.0 * Math.Cos(input[0].Y * Math.PI / 180.0), ky = 110540.0;
            points = input.Select(p => new Point(p.X * kx, p.Y * ky)).ToList();
            toLngLat = (x, y) => new Point(x / kx, y / ky);
        }

        Point a = points[0], b = points[1];
        const double eps = 1e-6;
        double cw = cellWidth, ch = cellHeight;
        Point origin;
        double ux, uy, vx, vy;
        int cols, rows;

        if (align)
        {
            double minX = Math.Floor(Math.Min(a.X, b.X) / cw + eps) * cw, maxX = Math.Ceiling(Math.Max(a.X, b.X) / cw - eps) * cw,
                   minY = Math.Floor(Math.Min(a.Y, b.Y) / ch + eps) * ch, maxY = Math.Ceiling(Math.Max(a.Y, b.Y) / ch - eps) * ch;
            cols = Math.Max(1, (int)Math.Round((maxX - minX) / cw));
            rows = Math.Max(1, (int)Math.Round((maxY - minY) / ch));
            maxX = minX + cols * cw;
            maxY = minY + rows * ch;
            origin = new Point(a.X <= b.X ? minX : maxX, a.Y <= b.Y ? minY : maxY);
            ux = a.X <= b.X ? 1 : -1; uy = 0;
            vx = 0; vy = a.Y <= b.Y ? 1 : -1;
        }
        else
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len == 0)
            {
                transformer?.Dispose();
                return null;
            }

            var p = points[2];
            ux = dx / len; uy = dy / len;
            vx = -uy; vy = ux;
            double depth = (p.X - a.X) * vx + (p.Y - a.Y) * vy;
            if (depth < 0)
            {
                vx = -vx; vy = -vy;
                depth = -depth;
            }
            cols = Math.Max(1, (int)Math.Ceiling(len / cw - eps));
            rows = Math.Max(1, (int)Math.Ceiling(depth / ch - eps));
            origin = new Point(a.X, a.Y);
        }

        return new Grid(toLngLat, transformer)
        {
            CrsId = calcCrsId,
            Aligned = align,
            Origin = origin,
            Ux = ux, Uy = uy, Vx = vx, Vy = vy,
            CellWidth = cw, CellHeight = ch,
            Cols = cols, Rows = rows
        };
    }

    // 0 => A, 25 => Z, 26 => AA, ...
    static public string ColumnName(int col)
    {
        string name = String.Empty;
        int n = col + 1;
        while (n > 0)
        {
            int m = (n - 1) % 26;
            name = (char)('A' + m) + name;
            n = (n - 1) / 26;
        }
        return name;
    }

    // 0-based column/row => A1, B1, ...
    static public string CellName(int col, int row) => $"{ColumnName(col)}{row + 1}";

    static public string FormatCoordinate(double value)
        => Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
}
