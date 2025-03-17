using E.Standard.Platform;
using System;

namespace E.Standard.WebMapping.GeoServices.Topo;

public class Vertex<T>
{
    T _x, _y;

    public Vertex(T x, T y, object m = null, bool isNode = true)
    {
        _x = x;
        _y = y;
        this.M = m;
        this.IsNode = isNode;
    }

    public T X { get { return _x; } }
    public T Y { get { return _y; } }

    public object M { get; set; }

    public bool IsNode { get; set; }

    public bool Equals(Vertex<T> obj, double espilon)
    {
        if (obj != null)
        {
            if (typeof(T) == typeof(System.Double))
            {
                return Math.Abs(Convert.ToDouble(obj._x) - Convert.ToDouble(_x)) <= espilon &&
                       Math.Abs(Convert.ToDouble(obj._y) - Convert.ToDouble(_y)) <= espilon;
            }

            return obj._x.Equals(_x) &&
                   obj._y.Equals(_y);
        }
        return false;
    }

    public override string ToString()
    {
        if (typeof(T) == typeof(double))
        {
            return (Convert.ToDouble(_x)).ToPlatformNumberString() + "," + (Convert.ToDouble(_y)).ToPlatformNumberString();
        }

        return _x.ToString() + "," + _y.ToString();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
