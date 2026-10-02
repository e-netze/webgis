using System;
using System.Data;
using System.Linq;

using Proj4Net.Core;

namespace E.Standard.WebMapping.Core.Geometry;


/// <summary>
/// Performs transformation between two spatial referernces.
/// </summary>
public sealed class GeometricTransformer : IGeometricTransformer
{
    private CoordinateReferenceSystem _fromSrs = null, _toSrs = null;
    private bool _toProjective = true, _fromProjective = true;
    private int _fromSrsId, _toSrsId;
    private string _fromProj4 = String.Empty, _toProj4 = String.Empty;

    private const double RAD2DEG = (180.0 / Math.PI);
    //static private object lockThis = new object();

    private CoordinateReferenceSystemFactory _factory = new CoordinateReferenceSystemFactory();

    #region Members

    private void FromSpatialReference(string parameters, bool isGeographic)
    {
        _fromSrsId = 0;
        _fromSrs = _factory.CreateFromParameters("from", _fromProj4 = parameters);

        _fromProjective = (isGeographic == false);
    }

    public void FromSpatialReference(SpatialReference spatialReference)
    {
        if (spatialReference == null)
        {
            _fromSrs = null;
            _fromSrsId = 0;
            _fromProj4 = String.Empty;
            return;
        }

        FromSpatialReference(spatialReference.Proj4, !spatialReference.IsProjective);
        _fromSrsId = spatialReference.Id;
    }

    private void ToSpatialReference(string parameters, bool isGeographic)
    {
        _toSrsId = 0;
        _toSrs = _factory.CreateFromParameters("to", _toProj4 = parameters);

        _toProjective = (isGeographic == false);
    }

    public void ToSpatialReference(SpatialReference spatialReference)
    {
        if (spatialReference == null)
        {
            _toSrs = null;
            _toSrsId = 0;
            _toProj4 = String.Empty;
            return;
        }

        ToSpatialReference(spatialReference.Proj4, !spatialReference.IsProjective);
        _toSrsId = spatialReference.Id;
    }

    public void Transform2D(Shape shape)
        => Transform2D(shape, ShapeSrsProperties.SrsId);

    public void Transform2D(
        Shape shape,
        ShapeSrsProperties setSrsProperties)
    {
        if (shape == null || !CanTransform)
        {
            return;
        }

        if (shape is Envelope)
        {
            Point p1 = ((Envelope)shape).LowerLeft;
            Point p2 = ((Envelope)shape).UpperRight;
            Transform2D(p1);
            Transform2D(p2);
            ((Envelope)shape).LowerLeft = p1;
            ((Envelope)shape).UpperRight = p2;
        }
        else
        {
            //DateTime now = DateTime.Now;

            var shapePoints = SpatialAlgorithms.ShapePoints(shape, false);
            double[] x = shapePoints.Select(p => p.X).ToArray();
            double[] y = shapePoints.Select(p => p.Y).ToArray();
            Transform2D(x, y);
            for (var i = 0; i < x.Length; i++)
            {
                shapePoints[i].X = x[i];
                shapePoints[i].Y = y[i];
            }

            //foreach (Point point in SpatialAlgorithms.ShapePoints(shape, false))
            //{
            //    Transform2D(point);
            //}

            //double ms = (DateTime.Now - now).TotalMilliseconds;
        }
        SetSrsProperties(shape, setSrsProperties, _toSrsId, _toProj4);
    }
    public void Transform2D(Point point)
        => Transform2D(point, ShapeSrsProperties.SrsId);

    public void Transform2D(
        Point point,
        ShapeSrsProperties setSrsProperties)
    {
        if (point != null && CanTransform)
        {
            double[] x = new double[] { point.X };
            double[] y = new double[] { point.Y };
            Transform2D(x, y);
            point.X = x[0];
            point.Y = y[0];

            SetSrsProperties(point, setSrsProperties, _toSrsId, _toProj4);
        }
    }

    public void Transform2D(double[] x, double[] y)
    {
        Transform2D_(x, y, _fromSrs, _toSrs, _fromProjective, _toProjective);
    }

    public void InvTransform2D(Shape shape)
        => InvTransform2D(shape, ShapeSrsProperties.SrsId);

    public void InvTransform2D(
        Shape shape,
        ShapeSrsProperties setSrsProperties)
    {
        if (shape == null || !CanTransform)
        {
            return;
        }

        if (shape is Envelope)
        {
            Point p1 = ((Envelope)shape).LowerLeft;
            Point p2 = ((Envelope)shape).UpperRight;
            InvTransform2D(p1);
            InvTransform2D(p2);
            ((Envelope)shape).LowerLeft = p1;
            ((Envelope)shape).UpperRight = p2;
        }
        else
        {
            foreach (Point point in SpatialAlgorithms.ShapePoints(shape, false))
            {
                InvTransform2D(point);
            }
        }

        SetSrsProperties(shape, setSrsProperties, _fromSrsId, _fromProj4);
    }
    public void InvTransform2D(Point point)
        => InvTransform2D(point, ShapeSrsProperties.SrsId);

    public void InvTransform2D(
        Point point,
        ShapeSrsProperties setSrsProperties)
    {
        if (point != null && CanTransform)
        {
            double[] x = new double[] { point.X };
            double[] y = new double[] { point.Y };
            Transform2D_(x, y, _toSrs, _fromSrs, _toProjective, _fromProjective);
            point.X = x[0];
            point.Y = y[0];

            SetSrsProperties(point, setSrsProperties, _fromSrsId, _fromProj4);
        }
    }

    private static void SetSrsProperties(
        Shape shape,
        ShapeSrsProperties setSrsProperties,
        int srsId,
        string proj4)
    {
        if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsId) && srsId > 0)
        {
            shape.SrsId = srsId;
        }

        if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsProj4Parameters))
        {
            shape.SrsP4Parameters = proj4;
        }
    }

    private void Transform2D_(double[] x, double[] y, CoordinateReferenceSystem from, CoordinateReferenceSystem to, bool fromProjective, bool toProjektive)
    {
        if (x == null || y == null || x.Length != y.Length || from == null || to == null)
        {
            return;
        }

        if (!fromProjective)
        {
            ToRad(x, y);
        }

        var projectionPipeline = ProjectionPipeline(from, to);

        for (int p = 0, p_to = projectionPipeline.Length; p < p_to - 1; p++)
        {
            BasicCoordinateTransform t = new BasicCoordinateTransform(projectionPipeline[p], projectionPipeline[p + 1]);

            ProjCoordinate cFrom = new ProjCoordinate(), cTo = new ProjCoordinate();
            for (int i = 0, i_to = x.Length; i < i_to; i++)
            {
                cFrom.X = x[i];
                cFrom.Y = y[i];
                t.Transform(cFrom, cTo);
                x[i] = cTo.X;
                y[i] = cTo.Y;
            }
        }

        if (!toProjektive)
        {
            ToDeg(x, y);
        }
    }

    private CoordinateReferenceSystem[] ProjectionPipeline(CoordinateReferenceSystem from, CoordinateReferenceSystem to)
    {
        //
        //  Proj4net berücksichtigt nadgrids=@null nicht, wodurch bei Koordinatensystem mit diesem Parametern ein Fehler im Hochwert entsteht!
        //  Workaround: Zuerst nach WGS84 projezieren und dann weiter...
        //
        //  Kontrolle:
        //  31255 (M31):    -27239.046 335772.696625   muss
        //  3857 (WebM):  1443413.9514 6133464.20918   ergeben!
        //                                             und umgekehrt!
        //
        //  Weiter Umrechnungen kann man auf https://mygeodata.cloud/cs2cs/ testen
        //

        // Das Problem sollte mit Proj4Net.Core behoben sein.
        // Der Zwischenschritt ist jetzt nicht mehr notwendig

        if (from.Parameters.Contains("+nadgrids=@null") || to.Parameters.Contains("+nadgrids=@null"))
        {
            var wgs84 = _factory.CreateFromParameters("epsg:4326", "+proj=longlat +ellps=WGS84 +datum=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs");

            if (!IsEqual(wgs84, from) && !IsEqual(wgs84, to))
            {
                return new CoordinateReferenceSystem[]
                {
                    from,
                    //wgs84,
                    to
                };
            }
        }

        return new CoordinateReferenceSystem[] { from, to };
    }

    private bool IsEqual(CoordinateReferenceSystem c1, CoordinateReferenceSystem c2)
    {
        if (c1.Parameters.Length != c2.Parameters.Length)
        {
            return false;
        }

        foreach (var p in c1.Parameters)
        {
            if (!c2.Parameters.Contains(p))
            {
                return false;
            }
        }

        return true;
    }

    private void ToDeg(double[] x, double[] y)
    {
        // Obsolete: Proj4.Net rechnet in Deg
        //if (x.Length != y.Length) return;

        //for (int i = 0; i < x.Length; i++)
        //{
        //    x[i] *= RAD2DEG;
        //    y[i] *= RAD2DEG;
        //}
    }

    private void ToRad(double[] x, double[] y)
    {
        // Obsolete: Proj4.Net rechnet in Deg
        //if (x.Length != y.Length) return;

        //for (int i = 0; i < x.Length; i++)
        //{
        //    x[i] /= RAD2DEG;
        //    y[i] /= RAD2DEG;
        //}
    }
    public void Release()
    {

    }

    public bool CanTransform
    {
        get
        {
            return _fromSrs != null && _toSrs != null;
        }
    }

    public bool ToIsProjective => _toProjective;
    public bool FromProjective => _fromProjective;

    #endregion

    static public void Transform2D(Shape shape, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        if (shape == null)
        {
            return;
        }

        if (from == null || to == null || from.Equals(to))
        {
            return;
        }

        using (GeometricTransformer transformer = new GeometricTransformer())
        {
            transformer.FromSpatialReference(from, isFromGeographic);
            transformer.ToSpatialReference(to, isToGeographic);
            transformer.Transform2D(shape);
            transformer.Release();
        }
    }

    static public Shape Transform2D(
        Shape shape,
        SpatialReference from,
        SpatialReference to)
    {
        if (shape == null || from == null || to == null)
        {
            return shape;
        }

        using (var transformer = new GeometricTransformer())
        {
            transformer.FromSpatialReference(from);
            transformer.ToSpatialReference(to);
            transformer.Transform2D(shape);
        }

        return shape;
    }

    static public void Transform2D(ref double x, ref double y, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        double[] X = new double[] { x };
        double[] Y = new double[] { y };

        Transform2D(X, Y, from, isFromGeographic, to, isToGeographic);
        x = X[0];
        y = Y[0];
    }

    static public void Transform2D(double[] x, double[] y, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        if (from == null || to == null || from.Equals(to))
        {
            return;
        }

        using (GeometricTransformer transformer = new GeometricTransformer())
        {
            transformer.FromSpatialReference(from, isFromGeographic);
            transformer.ToSpatialReference(to, isToGeographic);
            transformer.Transform2D(x, y);
            transformer.Release();
        }
    }

    static public void InvTransform2D(double[] x, double[] y, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        Transform2D(x, y, to, isToGeographic, from, isFromGeographic);
    }

    static public void Transform2D(DataTable tab, string xField, string yField, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        if (from == null || to == null || from.Equals(to) ||
            tab == null || tab.Rows.Count == 0 || tab.Columns[xField] == null || tab.Columns[xField] == null)
        {
            return;
        }

        double[] x = new double[tab.Rows.Count];
        double[] y = new double[tab.Rows.Count];

        for (int i = 0; i < x.Length; i++)
        {
            x[i] = Convert.ToDouble(tab.Rows[i][xField]);
            y[i] = Convert.ToDouble(tab.Rows[i][yField]);
        }

        Transform2D(x, y, from, isFromGeographic, to, isToGeographic);

        for (int i = 0; i < x.Length; i++)
        {
            tab.Rows[i][xField] = x[i];
            tab.Rows[i][yField] = y[i];
        }
    }

    static public void InvTransform2D(DataTable tab, string xField, string yField, string from, bool isFromGeographic, string to, bool isToGeographic)
    {
        Transform2D(tab, xField, yField, to, isToGeographic, from, isFromGeographic);
    }

    #region IDisposable Member

    public void Dispose()
    {
        this.Release();
    }

    #endregion

    #region IGeometricTransformer Member

    public void Transform(double[] x, double[] y)
    {
        Transform2D(x, y);
    }

    public void Transform(Shape shape)
    {
        Transform2D(shape);
    }

    public void InvTransform(Shape shape)
    {
        InvTransform2D(shape);
    }

    #endregion
}
