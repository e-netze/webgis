using System;

namespace E.Standard.WebMapping.Core;

public enum MapUnits
{
    meters = 0,
    feet = 1,
    decimal_degrees = 2
}

public class UnitConverter
{
    private static double _R = 6378137.0;
    private const double RAD2DEG = (180.0 / Math.PI);

    #region ToMeters
    public static double ToMeters(double val, MapUnits unit)
    {
        return ToMeters(val, unit, 1, 0.0);
    }
    public static double ToMeters(double val, MapUnits unit, int dim)
    {
        return ToMeters(val, unit, dim, 0.0);
    }
    public static double ToMeters(double val, MapUnits unit, int dim, double phi)
    {
        if (dim <= 0)
        {
            dim = 1;
        }

        switch (unit)
        {
            case MapUnits.meters:
                if (dim == 1)
                {
                    return val;
                }

                return Math.Pow(val, dim);
            case MapUnits.feet:
                return val * Math.Pow(0.3048, dim);
            case MapUnits.decimal_degrees:
                if (dim > 1)
                {
                    return 0.0;
                }

                return val / RAD2DEG * _R * Math.Cos(phi / RAD2DEG);
        }
        return val;
    }
    #endregion

    #region FromMeters
    public static double FromMeters(double val, MapUnits unit)
    {
        return FromMeters(val, unit, 1, 0.0);
    }
    public static double FromMeters(double val, MapUnits unit, int dim)
    {
        return FromMeters(val, unit, dim, 0.0);
    }
    public static double FromMeters(double val, MapUnits unit, int dim, double phi)
    {
        if (dim <= 0)
        {
            dim = 1;
        }

        switch (unit)
        {
            case MapUnits.meters:
                if (dim == 1)
                {
                    return val;
                }

                return Math.Pow(val, dim);
            case MapUnits.feet:
                return val * Math.Pow(3.28084, dim);
            case MapUnits.decimal_degrees:
                if (dim > 1 || Math.Cos(phi / RAD2DEG) == 0.0)
                {
                    return 0.0;
                }

                return val / (_R * Math.Cos(phi / RAD2DEG)) * RAD2DEG;
        }
        return val;
    }
    #endregion
}
