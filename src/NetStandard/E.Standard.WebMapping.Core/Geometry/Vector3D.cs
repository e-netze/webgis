using System;

namespace E.Standard.WebMapping.Core.Geometry;


public class Vector3D
{
    double _x, _y, _z;

    public Vector3D(double x, double y, double z)
    {
        _x = x;
        _y = y;
        _z = z;
    }
    public Vector3D(Vector3D vec)
        : this(vec.X, vec.Y, vec.Z)
    { }
    public Vector3D(Point start, Point end)
        : this(end.X - start.X, end.Y - start.Y, end.Z - start.Z)
    { }

    public double X
    {
        get { return _x; }
        set { _x = value; }
    }
    public double Y
    {
        get { return _y; }
        set { _y = value; }
    }
    public double Z
    {
        get { return _z; }
        set { _z = value; }
    }

    public double Length
    {
        get
        {
            return Math.Sqrt((_x * _x + _y * _y + _z * _z));
        }
        set
        {
            Normalize();
            _x *= value;
            _y *= value;
            _z *= value;
        }
    }

    public double Angle
    {
        get
        {
            double angle = Math.Atan2(_y, _x);
            if (angle < 0)
            {
                angle += 2.0 * Math.PI;
            }

            return angle;
        }
    }

    public double Azimut
    {
        get
        {
            double azi = Math.PI / 2.0 - this.Angle;
            if (azi < 0)
            {
                azi += 2.0 * Math.PI;
            }

            return azi;
        }
    }

    public double Z_angle
    {
        get
        {
            double l2d = this.Length2D;
            return Math.Atan2(_z, l2d);
        }
    }

    public double Length2D
    {
        get
        {
            Vector2D v = new Vector2D(_x, _y);
            return v.Length;
        }
    }

    public void Normalize()
    {
        double len = Length;
        if (len == 0.0 || len == 1.0)
        {
            return;
        }

        _x /= len;
        _y /= len;
        _z /= len;
    }
}
