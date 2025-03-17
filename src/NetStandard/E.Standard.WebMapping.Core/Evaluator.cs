using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.Data;

namespace E.Standard.WebMapping.Core;

public class Eval
{
    //private static NumberFormatInfo _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    // simulate basic evaluation of arithmetic expression

    private const string NaN = "NaN";

    public Eval()
    {

    }

    public static bool VerifyAllowed(string e)
    {
        string allowed = "0123456789+-*/()., ";
        for (int i = 0; i < e.Length; i++)
        {
            if (allowed.IndexOf("" + e[i]) == -1)
            {
                return false;
            }
        }
        return true;
    }

    public static string Evaluate(string e)
    {
        e = e.Replace("$pi()", Math.PI.ToString());

        if (e.Length == 0)
        {
            return "String length is zero";
        }
        if (!VerifyAllowed(e))
        {
            return "The string contains not allowed characters";
        }
        if (e[0] == '-')
        {
            e = "0" + e;
        }
        string res = "";
        try
        {
            res = Calculate(e).ToString();
        }
        catch
        {
            return "The call caused an exception";
        }
        return res;
    }

    public static double Calculate(string e)
    {
        e = e.Replace("$pi()", Math.PI.ToString());

        //e = e.Replace(".", ",");
        if (e.IndexOf("(") != -1)
        {
            int a = e.LastIndexOf("(");
            int b = e.IndexOf(")", a);
            double middle = Calculate(e.Substring(a + 1, b - a - 1));
            return Calculate(e.Substring(0, a) + middle.ToString() + e.Substring(b + 1));
        }
        double result = 0;
        string[] plus = e.Split('+');
        if (plus.Length > 1)
        {
            // there were some +
            result = Calculate(plus[0]);
            for (int i = 1; i < plus.Length; i++)
            {
                result += Calculate(plus[i]);
            }
            return result;

        }
        else
        {
            // no +
            string[] minus = plus[0].Split('-');
            if (minus.Length > 1)
            {
                // there were some -
                result = Calculate(minus[0]);
                for (int i = 1; i < minus.Length; i++)
                {
                    result -= Calculate(minus[i]);
                }
                return result;

            }
            else
            {
                // no -
                string[] mult = minus[0].Split('*');
                if (mult.Length > 1)
                {
                    // there were some *
                    result = Calculate(mult[0]);
                    for (int i = 1; i < mult.Length; i++)
                    {
                        result *= Calculate(mult[i]);
                    }
                    return result;

                }
                else
                {
                    // no *
                    string[] div = mult[0].Split('/');
                    if (div.Length > 1)
                    {
                        // there were some /
                        result = Calculate(div[0]);
                        for (int i = 1; i < div.Length; i++)
                        {
                            result /= Calculate(div[i]);
                        }
                        return result;

                    }
                    else
                    {
                        // no /
                        return e.ToPlatformDouble();
                    }
                }
            }
        }
    }

    public static string Evaluate2(string e)
    {
        try
        {
            e = e.Replace("$pi()", Math.PI.ToString());

            e = e.Replace(",", ".");
            DataTable table = new DataTable();
            object result = table.Compute(e, "");
            return Convert.ToDouble(result).ToString();

            //table.Columns.Add("expression", string.Empty.GetType(), e);
            //DataRow row = table.NewRow();
            //table.Rows.Add(row);

            //return double.Parse((string)row["expression"]).ToString();
        }
        catch // (Exception ex)
        {
            return NaN;
            //return ex.Message;
        }
    }

    public static string EvalFunctions(string e)
    {
        e = e.Replace("$pi()", Math.PI.ToString());

        List<string> functions = new List<string>(new string[] { "$eval", "$cos", "$sin", "$tan", "$acos", "$asin", "$atan" });

        foreach (string func in functions)
        {
            while (e.IndexOf(func + "(") != -1)
            {
                int pos = e.LastIndexOf(func + "("), pos2 = -1, level = 0;
                for (int i = pos + func.Length + 1; i < e.Length; i++)
                {
                    if (e[i] == '(')
                    {
                        level++;
                    }

                    if (e[i] == ')')
                    {
                        if (level == 0)
                        {
                            pos2 = i;
                            break;
                        }
                        else
                        {
                            level--;
                        }
                    }
                }
                if (pos2 == -1)
                {
                    return "Syntax error: " + e;
                }

                string eval = e.Substring(pos + func.Length + 1, pos2 - pos - (func.Length + 1));

                // Recursiv
                foreach (string f in functions)
                {
                    if (eval.Contains(f))
                    {
                        eval = EvalFunctions(eval);
                    }
                }

                eval = Evaluate2(eval);

                try
                {
                    switch (func)
                    {
                        case "$sin":
                            eval = Math.Sin(eval.ToPlatformDouble()).ToString();
                            break;
                        case "$cos":
                            eval = Math.Cos(eval.ToPlatformDouble()).ToString();
                            break;
                        case "$tan":
                            eval = Math.Tan(eval.ToPlatformDouble()).ToString();
                            break;
                        case "$asin":
                            eval = Math.Acos(eval.ToPlatformDouble()).ToString();
                            break;
                        case "$acos":
                            eval = Math.Asin(eval.ToPlatformDouble()).ToString();
                            break;
                        case "$atan":
                            eval = Math.Atan(eval.ToPlatformDouble()).ToString();
                            break;
                    }

                    e = e.Substring(0, pos) + eval + e.Substring(pos2 + 1, e.Length - pos2 - 1);
                }
                catch
                {
                    e = NaN;
                }
            }
        }

        return e;
    }

    public static string ParseEvalExpression(string e)
    {
        e = e.Replace("$pi()", Math.PI.ToString());

        //while (e.IndexOf("$eval(") != -1)
        //{
        //    int pos = e.LastIndexOf("$eval("), pos2 = -1, level = 0;
        //    for (int i = pos + 6; i < e.Length; i++)
        //    {
        //        if (e[i] == '(')
        //            level++;
        //        if (e[i] == ')')
        //        {
        //            if (level == 0)
        //            {
        //                pos2 = i;
        //                break;
        //            }
        //            else
        //            {
        //                level--;
        //            }
        //        }
        //    }
        //    if (pos2 == -1)
        //        return "Syntax error: " + e;

        //    string eval = e.Substring(pos + 6, pos2 - pos - 6);
        //    eval = Evaluate2(eval);

        //    e = e.Substring(0, pos) + eval + e.Substring(pos2 + 1, e.Length - pos2 - 1);
        //}

        e = EvalFunctions(e);

        // round
        for (int i = 0; i < 6; i++)
        {
            string func = $"$round{i}(";

            while (e.IndexOf(func) != -1)
            {
                int pos = e.LastIndexOf(func), pos2 = e.IndexOf(")", pos);
                if (pos2 == -1)
                {
                    return $"Syntax error: {e}";
                }

                try
                {
                    double round = e.Substring(pos + 8, pos2 - pos - 8).ToPlatformDouble();

                    round = Math.Round(round, i);
                    e = e.Substring(0, pos) + String.Format(RoundFormat(i), round) + e.Substring(pos2 + 1, e.Length - pos2 - 1);
                }
                catch
                {
                    e = NaN;
                    break;
                }
            }
        }

        // Standard Numeric Format 1000.123 => n0, n1, n2 ... => 1.000, 1.000,1, 1.000.12 ...
        for (int i = 0; i < 6; i++)
        {
            string func = $"$n{i}(";

            while (e.IndexOf(func) != -1)
            {
                int pos = e.LastIndexOf(func), pos2 = e.IndexOf(")", pos);
                if (pos2 == -1)
                {
                    return $"Syntax error: {e}";
                }

                try
                {
                    double number = e.Substring(pos + 4, pos2 - pos - 4).ToPlatformDouble();

                    e = e.Substring(0, pos) + number.ToString($"N{i}") + e.Substring(pos2 + 1, e.Length - pos2 - 1);
                }
                catch
                {
                    e = NaN;
                    break;
                }
            }
        }

        // Standard Numeric Format 1000.123 (German Culture) => n0, n1, n2 ... => 1.000, 1.000,1, 1.000.12 ...
        for (int i = 0; i < 6; i++)
        {
            string func = $"$n{i}_de(";

            while (e.IndexOf(func) != -1)
            {
                int pos = e.LastIndexOf(func), pos2 = e.IndexOf(")", pos);
                if (pos2 == -1)
                {
                    return $"Syntax error: {e}";
                }

                try
                {
                    double number = e.Substring(pos + 7, pos2 - pos - 7).ToPlatformDouble();

                    e = e.Substring(0, pos) + number.ToString($"N{i}", NumberConverter.GermanCultureInfo) + e.Substring(pos2 + 1, e.Length - pos2 - 1);
                }
                catch
                {
                    e = NaN;
                    break;
                }
            }
        }

        return e;
    }

    #region Parse Functions

    private static string[] _functions = new string[] { "$GNR", "$GNRKEY16", "$URL" };

    public static string ParseFunctionExpression(string e)
    {
        if (!ContainsFunctions(e))
        {
            return e;
        }

        foreach (string function in _functions)
        {
            while (e.IndexOf(function + "(") != -1)
            {
                int pos = e.LastIndexOf(function + "("), pos2 = -1, level = 0;
                for (int i = pos + function.Length + 1; i < e.Length; i++)
                {
                    if (e[i] == '(')
                    {
                        level++;
                    }

                    if (e[i] == ')')
                    {
                        if (level == 0)
                        {
                            pos2 = i;
                            break;
                        }
                        else
                        {
                            level--;
                        }
                    }
                }
                if (pos2 == -1)
                {
                    return "Syntax error: " + e;
                }

                string eval = ParseFunctionExpression(e.Substring(pos + function.Length + 1, pos2 - pos - function.Length - 1));
                switch (function)
                {
                    case "$GNR":
                        eval = FuncGNR(eval);
                        break;
                    case "$GNRKEY16":
                        eval = FuncGNRKEY16(eval);
                        break;
                    case "$URL":
                        eval = FuncURL(eval);
                        break;
                }
                e = e.Substring(0, pos) + eval + e.Substring(pos2 + 1, e.Length - pos2 - 1);
            }
        }
        return e;
    }

    private static bool ContainsFunctions(string e)
    {
        foreach (string function in _functions)
        {
            if (e.Contains($"{function}("))
            {
                return true;
            }
        }
        return false;
    }

    private static string RoundFormat(int digits)
    {
        if (digits <= 0)
        {
            return "{0}";
        }

        return $"{{0:0.{"0".PadLeft(digits, '0')}}}";
    }

    #region Functions

    private static string FuncGNR(string e)
    {
        string[] parts = e.Split(',');
        if (parts.Length == 3)
        {
            string bfl = parts[0].Trim();
            string snr = parts[1].Trim();
            string unr = parts[2].Trim();

            e = bfl + snr + ((String.IsNullOrEmpty(unr) || unr == "0") ? String.Empty : "/" + unr);
        }
        return e;
    }

    private static string FuncGNRKEY16(string e)
    {
        string[] parts = e.Split(',');
        if (parts.Length == 4)
        {
            string kg = parts[0].Trim();
            string bfl = parts[1].Trim();
            string snr = parts[2].Trim();
            string unr = parts[3].Trim();

            e = kg.PadLeft(5, ' ') + bfl.PadLeft(1, ' ') + snr.PadLeft(5, ' ') + unr.PadLeft(5, ' ');
        }
        return e;
    }

    private static string FuncURL(string e)
    {
        return e.Replace(" ", "%20").Replace("#", "%23").Replace("+", "%2b").Replace("&", "%26");
    }

    #endregion

    #endregion
}
