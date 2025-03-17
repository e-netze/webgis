using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class SpatialReference
{
    private string _proj4 = String.Empty, _epsg = String.Empty; // _wkt = String.Empty;
    private readonly bool _isProjective = true;
    private string _name = String.Empty;
    private readonly int _id = -1;
    private AxisDirection _axisX = AxisDirection.North, _axisY = AxisDirection.East;

    public const int ID_WGS84 = 4326;

    public SpatialReference(int id, string name, string proj4)
    {
        _id = id;
        _name = name;
        _proj4 = proj4;

        if (_proj4.Contains("+proj=longlat"))
        {
            _isProjective = false;
            //_axisX = AxisDirection.East;
            //_axisY = AxisDirection.North;
        }
        /*
        if (id == 32633)
        {
            _axisX = AxisDirection.North;
            _axisY = AxisDirection.East;
        }
         * */
    }

    //public SpatialReference(int id, string name, string proj4, bool isProjective)
    //    : this(id, name, proj4)
    //{
    //    _isProjective = isProjective;
    //}

    public int Id
    {
        get { return _id; }
    }
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }
    public string Proj4
    {
        get { return _proj4; }
        set { _proj4 = value; }
    }
    public string EPSG
    {
        get { return _epsg; }
        set { _epsg = value; }
    }
    //public string WKT
    //{
    //    get { return _wkt; }
    //    set { _wkt = value; }
    //}

    public bool IsProjective
    {
        get { return _isProjective; }
    }

    public AxisDirection AxisX
    {
        get { return _axisX; }
        set { _axisX = value; }
    }
    public AxisDirection AxisY
    {
        get { return _axisY; }
        set { _axisY = value; }
    }

    public SpatialReference Clone()
    {
        SpatialReference clone = new SpatialReference(_id, _name, _proj4);
        //clone.WKT = this.WKT;
        clone.EPSG = this.EPSG;
        clone._axisX = _axisX;
        clone._axisY = _axisY;

        return clone;
    }

    /*
    public static int TryToDetermineId(int wktId, string wkt)
    {
        if (wktId > 0)
            return wktId;

        if (!String.IsNullOrEmpty(wkt))
        {
            // DoTo:
            return 31256;
        }

        return 0;
    }
     * */

    public void ReplaceOrInsertProj4TransformationParameters(string[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return;
        }

        List<string> sRefParameters = new List<string>(this.Proj4.Split(' '));
        List<string> replaceParameters = new List<string>(parameters);
        StringBuilder sb = new StringBuilder();

        // ReplaceParameters
        foreach (string sRefParameter in sRefParameters)
        {
            if (sb.Length >= 0)
            {
                sb.Append(" ");
            }

            string sRefParameterName = sRefParameter.Split('=')[0].Trim();
            string replaceParemter = replaceParameters.Where(p => p.StartsWith(sRefParameterName + "=")).FirstOrDefault();
            if (!String.IsNullOrWhiteSpace(replaceParemter))
            {
                sb.Append(replaceParemter);
                replaceParameters.Remove(replaceParemter);
            }
            else
            {
                sb.Append(sRefParameter);
            }
        }

        // add Parameters
        foreach (string replaceParameter in replaceParameters)
        {
            if (String.IsNullOrWhiteSpace(replaceParameter))
            {
                continue;
            }

            if (sb.Length >= 0)
            {
                sb.Append(" ");
            }

            sb.Append(replaceParameter);
        }

        this.Proj4 = sb.ToString();
    }
}
