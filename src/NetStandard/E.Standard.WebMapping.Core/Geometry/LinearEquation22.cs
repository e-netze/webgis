namespace E.Standard.WebMapping.Core.Geometry;

public class LinearEquation22
{
    private double _l0, _l1, _a00, _a01, _a10, _a11;
    private double t1 = double.NaN, t2 = double.NaN;

    public LinearEquation22(double l0, double l1, double a00, double a01, double a10, double a11)
    {
        _l0 = l0;
        _l1 = l1;
        _a00 = a00;
        _a01 = a01;
        _a10 = a10;
        _a11 = a11;
    }

    public bool Solve()
    {
        double detA = Det.Calc22(_a00, _a01, _a10, _a11);
        if (detA == 0.0)
        {
            return false;
        }

        t1 = Det.Calc22(_l0, _a01, _l1, _a11) / detA;
        t2 = Det.Calc22(_a00, _l0, _a10, _l1) / detA;
        return true;
    }

    public double Var1
    {
        get { return t1; }
    }
    public double Var2
    {
        get { return t2; }
    }

    #region Classes

    private class Det
    {
        static public double Calc22(double a00, double a01, double a10, double a11)
        {
            return a00 * a11 - a01 * a10;
        }
    }

    #endregion
}
