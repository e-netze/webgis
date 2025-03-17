namespace E.Standard.WebMapping.Core.Geometry;

public class Azimut
{
    static public double ToAzimutDeg(double alpha)
    {
        double a = 90 - alpha;
        return PositiveAngleDeg(a);
    }

    static public double ToAzimutGon(double alpha)
    {
        double a = ToAzimutDeg(alpha);
        return a / 0.9;
    }

    static public double FromAzimutDeg(double a)
    {
        double aplha = 90.0 - a;
        return PositiveAngleDeg(aplha);
    }

    static public double FromAzimutGon(double a)
    {
        return FromAzimutDeg(a * 0.9);
    }

    static public double PositiveAngleDeg(double angle)
    {
        while (angle < 0)
        {
            angle += 360.0;
        }

        while (angle >= 360.0)
        {
            angle -= 360.0;
        }

        return angle;
    }

    static public double PositiveAngleGon(double angle)
    {
        while (angle < 0)
        {
            angle += 400.0;
        }

        while (angle >= 400.0)
        {
            angle -= 400.0;
        }

        return angle;
    }
}
