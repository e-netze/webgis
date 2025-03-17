using System;

namespace E.Standard.GeoRSS20;

public class RssPoint : RssGeometry
{
    private double _long, _lat;

    public RssPoint()
    {
        _long = _lat = 0.0;
    }

    public RssPoint(double lat, double lon)
    {
        _long = lon;
        _lat = lat;
    }

    public RssPoint(RssPoint point)
    {
        if (point != null)
        {
            _long = point._long;
            _lat = point._lat;
        }
        else
        {
            _lat = _long = 0.0;
        }
    }

    public RssPoint(string str)
    {
        string[] coords = str.Trim().Split(' ');
        if (coords.Length != 2)
        {
            throw new ArgumentException($"Invalid Point String: {str}");
        }

        _lat = double.Parse(coords[0], Formatter.nhi);
        _long = double.Parse(coords[1], Formatter.nhi);
    }

    public double Long
    {
        get { return _long; }
        set { _long = value; }
    }
    public double Lat
    {
        get { return _lat; }
        set { _lat = value; }
    }

    public override string ToString()
    {
        return $"{_lat.ToString(Formatter.nhi)} {_long.ToString(Formatter.nhi)}";
    }
}
