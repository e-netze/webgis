using System;

namespace E.Standard.GeoRSS20;

public class RssBox : RssGeometry
{
    private RssPoint _p1, _p2;

    public RssBox()
    {
        _p1 = new RssPoint();
        _p2 = new RssPoint();
    }

    public RssBox(RssPoint LowerLeft, RssPoint UpperRight)
    {
        _p1 = LowerLeft;
        _p2 = UpperRight;
    }

    public RssBox(RssBox box)
    {
        if (box != null)
        {
            _p1 = box._p1;
            _p2 = box._p2;
        }
        else
        {
            _p1 = new RssPoint();
            _p2 = new RssPoint();
        }
    }

    public RssBox(string str)
    {
        string[] coords = str.Split(' ');
        if (coords.Length != 4)
        {
            throw new ArgumentException("RssBox: Invalid Point String: " + str);
        }

        _p1 = new RssPoint(double.Parse(coords[0], Formatter.nhi), double.Parse(coords[1], Formatter.nhi));
        _p2 = new RssPoint(double.Parse(coords[2], Formatter.nhi), double.Parse(coords[3], Formatter.nhi));
    }

    public RssPoint LowerLeft
    {
        get { return _p1; }
        set { _p1 = value; }
    }
    public RssPoint UpperRight
    {
        get { return _p2; }
        set { _p2 = value; }
    }

    public override string ToString()
    {
        if (_p1 == null || _p2 == null)
        {
            return String.Empty;
        }

        return _p1.ToString() + " " + _p2.ToString();
    }

    public static RssBox FromBBOX(string bbox)
    {
        if (String.IsNullOrEmpty(bbox))
        {
            return null;
        }

        string[] coords = bbox.Split(',');
        if (coords.Length != 4)
        {
            throw new ArgumentException("RssBox: Invalid Point String: " + bbox);
        }

        RssPoint ll = new RssPoint(double.Parse(coords[1], Formatter.nhi), double.Parse(coords[0], Formatter.nhi));
        RssPoint ur = new RssPoint(double.Parse(coords[3], Formatter.nhi), double.Parse(coords[2], Formatter.nhi));

        return new RssBox(ll, ur);
    }
}
