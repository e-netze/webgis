using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.Core.Geometry;

public class Envelope : Shape
{
    private double _minx = 0.0, _miny = 0.0, _maxx = 0.0, _maxy = 0.0;
    //private double[]_rot=null;

    public Envelope()
    {
    }

    public Envelope(double minx, double miny, double maxx, double maxy)
    {
        Set(minx, miny, maxx, maxy);
    }

    public Envelope(Envelope env)
    {
        if (env != null)
        {
            _minx = env.MinX;
            _miny = env.MinY;
            _maxx = env.MaxX;
            _maxy = env.MaxY;

            _rotatedPolygon = env._rotatedPolygon;
            base.SrsId = env.SrsId;
            base.SrsP4Parameters = env.SrsP4Parameters;
        }
    }

    public Envelope(double[] bbox)
        : this(bbox[0], bbox[1], bbox[2], bbox[3])
    {

    }

    public Envelope(Point centerPoint, double width, double height)
        : this(centerPoint.X - width / 2, centerPoint.Y - height / 2, centerPoint.X + width / 2, centerPoint.Y + height / 2)
    {

    }

    public double MinX
    {
        get { return Math.Min(_minx, _maxx); }
        set { _minx = value; }
    }

    public double MinY
    {
        get { return Math.Min(_miny, _maxy); }
        set { _miny = value; }
    }

    public double MaxX
    {
        get { return Math.Max(_minx, _maxx); }
        set { _maxx = value; }
    }

    public double MaxY
    {
        get { return Math.Max(_miny, _maxy); }
        set { _maxy = value; }
    }

    public double Width
    {
        get { return Math.Abs(_maxx - _minx); }
    }

    public double Height
    {
        get { return Math.Abs(_maxy - _miny); }
    }

    public double SphericWidth(double R)
    {
        double lamAngle = Width * Math.PI / 180.0;
        double phi = (_miny + _maxy) / 2.0 * Math.PI / 180.0;

        return lamAngle * R * Math.Cos(phi);
    }

    public double SphericHeight(double R)
    {
        double phiAngle = Height * Math.PI / 180.0;

        return phiAngle * R;
    }

    public void Set(double minx, double miny, double maxx, double maxy)
    {
        _minx = Math.Min(minx, maxx);
        _miny = Math.Min(miny, maxy);
        _maxx = Math.Max(minx, maxx);
        _maxy = Math.Max(miny, maxy);
    }

    public Geometry.Point CenterPoint
    {
        get
        {
            return new Point((_maxx + _minx) * 0.5, (_maxy + _miny) * 0.5);
        }
        set
        {
            if (value == null)
            {
                return;
            }

            double W = this.Width;
            double H = this.Height;

            _minx = value.X - W / 2.0;
            _maxx = value.X + W / 2.0;
            _miny = value.Y - H / 2.0;
            _maxy = value.Y + H / 2.0;
        }
    }
    public Point LowerLeft
    {
        get
        {
            return new Point(_minx, _miny);
        }
        set
        {
            if (value != null)
            {
                _minx = value.X;
                _miny = value.Y;
            }
        }
    }
    public Point UpperRight
    {
        get
        {
            return new Point(_maxx, _maxy);
        }
        set
        {
            if (value != null)
            {
                _maxx = value.X;
                _maxy = value.Y;
            }
        }
    }

    public bool IsNull
    {
        get
        {
            return
                _minx == 0.0 &&
                _miny == 0.0 &&
                _maxx == 0.0 &&
                _maxy == 0.0;
        }
    }

    public bool HasValidExtent
    {
        get
        {
            return this.Width > 0D && this.Height > 0D;
        }
    }

    public void Union(Envelope envelope)
    {
        if (envelope == null ||
            envelope.IsNull)
        {
            return;
        }

        if (IsNull)
        {
            _minx = envelope.MinX;
            _miny = envelope.MinY;
            _maxx = envelope.MaxX;
            _maxy = envelope.MaxY;
        }
        else
        {
            _minx = Math.Min(_minx, envelope.MinX);
            _miny = Math.Min(_miny, envelope.MinY);
            _maxx = Math.Max(_maxx, envelope.MaxX);
            _maxy = Math.Max(_maxy, envelope.MaxY);
        }
    }

    public void Raise(double percent)
    {
        percent /= 100.0;
        double w = Math.Abs(_maxx - _minx);
        double h = Math.Abs(_maxy - _miny);

        w = (w * percent - w) / 2;
        h = (h * percent - h) / 2;

        _minx -= w;
        _miny -= h;
        _maxx += w;
        _maxy += h;
    }

    public void Raise(double cX, double cY, double percent)
    {
        percent /= 100;
        double w1 = cX - _minx, w2 = _maxx - cX;
        double h1 = cY - _miny, h2 = _maxy - cY;

        w1 = w1 * percent; w2 = w2 * percent;
        h1 = h1 * percent; h2 = h2 * percent;

        _minx = cX - w1;
        _miny = cY - h1;
        _maxx = cX + w2;
        _maxy = cY + h2;
    }

    public void Resize(double x)
    {
        Resize(x, x);
    }
    public void Resize(double x, double y)
    {
        _minx -= x;
        _maxx += x;
        _miny -= y;
        _maxy += y;
    }

    public void Translate(double mx, double my)
    {
        _minx += mx;
        _miny += my;
        _maxx += mx;
        _maxy += my;
    }

    public void TranslateTo(double mx, double my)
    {
        double cx = _minx * 0.5 + _maxx * 0.5;
        double cy = _miny * 0.5 + _maxy * 0.5;

        Translate(mx - cx, my - cy);
    }

    public void Rotate(double a)
    {
        a *= Math.PI / 180D;
        double cosa = Math.Cos(a), sina = Math.Sin(a);

        double cx = this.CenterPoint.X, cy = this.CenterPoint.Y;

        double r1x = _minx - cx, r1y = _miny - cy;
        double r2x = _maxx - cx, r2y = _maxy - cy;

        double r1x_ = r1x * cosa + r1y * sina;
        double r1y_ = -r1x * sina + r1y * cosa;

        double r2x_ = r2x * cosa + r2y * sina;
        double r2y_ = -r2x * sina + r2y * cosa;

        _minx = cx + r1x_;
        _miny = cy + r1y_;
        _maxx = cx + r2x_;
        _maxy = cy + r2y_;
    }

    public PointCollection ToPointCollection(int accuracy)
    {

        if (accuracy < 0)
        {
            accuracy = 0;
        }

        double stepx = this.Width / (accuracy + 1);
        double stepy = this.Height / (accuracy + 1);

        PointCollection pColl = new PointCollection();
        for (int y = 0; y <= accuracy + 1; y++)
        {
            for (int x = 0; x <= accuracy + 1; x++)
            {
                pColl.AddPoint(new Point(this.MinX + stepx * x, this.MinY + stepy * y));
            }

        }

        return pColl;
    }

    public bool Intersects(Envelope envelope)
    {
        if (envelope == null)
        {
            return false;
        }

        if (envelope.MaxX >= _minx &&
            envelope.MinX <= _maxx &&
            envelope.MaxY >= _miny &&
            envelope.MinY <= _maxy)
        {
            return true;
        }

        return false;
    }

    public bool Contains(Envelope envelope)
    {
        if (envelope == null)
        {
            return false;
        }

        if (envelope.MinX < _minx
            || envelope.MaxX > _maxx)
        {
            return false;
        }

        if (envelope.MinY < _miny
            || envelope.MaxY > _maxy)
        {
            return false;
        }

        return true;
    }

    public bool Contains(Point point)
    {
        if (point.X < _minx
            || point.X > _maxx)
        {
            return false;
        }

        if (point.Y < _miny
            || point.Y > _maxy)
        {
            return false;
        }

        return true;
    }

    public bool Contains(double pointX, double pointY)
    {
        if (pointX < _minx
            || pointX > _maxx)
        {
            return false;
        }

        if (pointY < _miny
            || pointY > _maxy)
        {
            return false;
        }

        return true;
    }

    public override Envelope ShapeEnvelope
    {
        get
        {
            return this;
        }
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<ENVELOPE ");
        sb.Append(base.AXLSrsAttribute());
        sb.Append("minx=\"" + (nfi != null ? MinX.ToString(nfi) : MinX.ToString()) + "\" ");
        sb.Append("miny=\"" + (nfi != null ? MinY.ToString(nfi) : MinY.ToString()) + "\" ");
        sb.Append("maxx=\"" + (nfi != null ? MaxX.ToString(nfi) : MaxX.ToString()) + "\" ");
        sb.Append("maxy=\"" + (nfi != null ? MaxY.ToString(nfi) : MaxY.ToString()) + "\" />");

        return sb.ToString();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Envelope))
        {
            return false;
        }

        Envelope env2 = (Envelope)obj;

        return
            Math.Abs(MinX - env2.MinX) < Shape.Epsilon &&
            Math.Abs(MinY - env2.MinY) < Shape.Epsilon &&
            Math.Abs(MaxX - env2.MaxX) < Shape.Epsilon &&
            Math.Abs(MaxY - env2.MaxY) < Shape.Epsilon;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return "(" + MinX.ToString() + " " + MinY.ToString() + "," + MaxX.ToString() + " " + MaxY.ToString() + ")";
    }

    public string ToBBox(SpatialReferenceCollection srefCollection = null, int targetSrsId = 0)
    {
        #region Project

        if (srefCollection != null && targetSrsId > 0)
        {
            int srsId = Math.Max(0, this.SrsId);

            if (srsId == 0 && this.HasWgs84Bounds)
            {
                srsId = 4326;
            }

            if (targetSrsId != srsId)
            {
                var sRef = srefCollection.ById(srsId);
                var targetSRef = srefCollection.ById(targetSrsId);

                if (sRef != null && targetSRef != null)
                {
                    var polygon = this.ToPolygon();
                    using (var transformer = new GeometricTransformerPro(sRef, targetSRef))
                    {
                        transformer.Transform(polygon);
                        return polygon.ShapeEnvelope.ToBBox();
                    }
                }
            }
        }

        #endregion

        return MinX.ToString().Replace(",", ".") + "," +
               MinY.ToString().Replace(",", ".") + "," +
               MaxX.ToString().Replace(",", ".") + "," +
               MaxY.ToString().Replace(",", ".");
    }
    public void FromBBox(string bbox)
    {
        string[] coords = bbox.Split(',');
        if (coords.Length != 4)
        {
            throw new ArgumentException("Ungültige BBOX: " + bbox);
        }

        NumberFormatInfo nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        _minx = Shape.Convert(coords[0], nhi);
        _miny = Shape.Convert(coords[1], nhi);
        _maxx = Shape.Convert(coords[2], nhi);
        _maxy = Shape.Convert(coords[3], nhi);
    }
    new static public Envelope FromArcXML(XmlNode node, NumberFormatInfo nfi)
    {
        if (node == null ||
            node.Name != "ENVELOPE")
        {
            throw new ArgumentException();
        }

        if (nfi == null)
        {
            return new Envelope(
                Shape.Convert(node.Attributes["minx"].Value, null),
                Shape.Convert(node.Attributes["miny"].Value, null),
                Shape.Convert(node.Attributes["maxx"].Value, null),
                Shape.Convert(node.Attributes["maxy"].Value, null));
        }
        else
        {
            return new Envelope(
                Shape.Convert(node.Attributes["minx"].Value, nfi),
                Shape.Convert(node.Attributes["miny"].Value, nfi),
                Shape.Convert(node.Attributes["maxx"].Value, nfi),
                Shape.Convert(node.Attributes["maxy"].Value, nfi));
        }
    }

    public override void Serialize(BinaryWriter w)
    {
        w.Write(_minx);
        w.Write(_miny);
        w.Write(_maxx);
        w.Write(_maxy);
    }

    public override void Deserialize(BinaryReader w)
    {
        _minx = w.ReadDouble();
        _miny = w.ReadDouble();
        _maxx = w.ReadDouble();
        _maxy = w.ReadDouble();
    }

    public Polygon ToPolygon()
    {
        Polygon poly = new Polygon();
        Ring ring = new Ring();
        ring.AddPoint(new Point(_minx, _miny));
        ring.AddPoint(new Point(_minx, _maxy));
        ring.AddPoint(new Point(_maxx, _maxy));
        ring.AddPoint(new Point(_maxx, _miny));
        poly.AddRing(ring);

        return poly;
    }

    public double[] ToArray()
    {
        return new double[]
                {
                    this.MinX,
                    this.MinY,
                    this.MaxX,
                    this.MaxY
                };
    }

    public double Area
    {
        get
        {
            return this.Width * this.Height;
        }
    }

    public bool HasWgs84Bounds =>
            this.MinX >= -180.0 && this.MinX <= 180.0 &&
            this.MaxX >= -180.0 && this.MaxX <= 180.0 &&
            this.MinY >= -90.0 && this.MinY <= 90.0 &&
            this.MaxY >= -90.0 && this.MaxY <= 90.0;

    #region Rotated Envelope

    private Polygon _rotatedPolygon = null;

    public double RotatedWidth
    {
        get
        {
            if (_rotatedPolygon == null)
            {
                return this.Width;
            }

            return _rotatedPolygon[0][0].Distance(_rotatedPolygon[0][1]);
        }
    }

    public double RotatedHeight
    {
        get
        {
            if (_rotatedPolygon == null)
            {
                return this.Height;
            }

            return _rotatedPolygon[0][1].Distance(_rotatedPolygon[0][2]);
        }
    }

    public Polygon GetRotatedBBoxPolygon()
    {
        if (_rotatedPolygon == null)
        {
            Polygon poly = new Polygon(new Ring());
            poly[0].AddPoint(new Point(_minx, _miny));
            poly[0].AddPoint(new Point(_maxx, _miny));
            poly[0].AddPoint(new Point(_maxx, _maxy));
            poly[0].AddPoint(new Point(_minx, _maxy));
            return poly;
        }
        return _rotatedPolygon;
    }

    /*
    public Polygon ToRotatedBBoxPolygon(double a)
    {
        a *= Math.PI / 180D;
        double cosa = Math.Cos(a), sina = Math.Sin(a);

        double dx = Math.Abs(_maxx - _minx), dy = Math.Abs(_maxy - _miny);
        int direction = dx > dy ? -1 : 1;

        double[] rx = new double[] { cosa, -sina * direction }; // R*(1 0)
        double[] ry = new double[] { sina * direction, cosa }; // R*(0 1)

        double[] l = new double[] { _maxx - _minx, _maxy - _miny };

        LinearEquation22 equation = new LinearEquation22(
            l[0], l[1], rx[0], ry[0],
                          rx[1], ry[1]);

        if (!equation.Solve())
            return null;

        double t1 = equation.Var1;
        double t2 = equation.Var2;

        Polygon polygon = new Polygon(new Ring());
        polygon[0].AddPoint(new Point(_minx, _miny));
        polygon[0].AddPoint(new Point(_minx + rx[0] * t1, _miny + rx[1] * t1));
        polygon[0].AddPoint(new Point(_maxx, _maxy));
        polygon[0].AddPoint(new Point(_maxx - rx[0] * t1, _maxy - rx[1] * t1));

        return polygon;
    }
     * */

    #endregion

    public static Envelope CreateRotatedEnvelope(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
    {
        Envelope ret = new Envelope(Math.Min(x1, x3),
                                    Math.Min(y1, y3),
                                    Math.Max(x2, x4),
                                    Math.Max(y2, y4));

        Polygon rotated = new Polygon(new Ring());
        rotated[0].AddPoint(new Point(x1, y1));
        rotated[0].AddPoint(new Point(x3, y3));
        rotated[0].AddPoint(new Point(x2, y2));
        rotated[0].AddPoint(new Point(x4, y4));

        ret._rotatedPolygon = rotated;

        return ret;
    }
}
