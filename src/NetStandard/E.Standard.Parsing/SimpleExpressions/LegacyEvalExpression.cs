using System.Data;
using System.Globalization;

namespace E.Standard.Parsing.SimpleExpressions;

public static class LegacyEvalExpression
{
    private const string NaN = "NaN";

    public static string Parse(string expression)
    {
        expression = expression.Replace("$pi()", Math.PI.ToString());
        expression = EvaluateFunctions(expression);

        for (var digits = 0; digits < 6; digits++)
        {
            var function = $"$round{digits}(";
            while (expression.IndexOf(function) != -1)
            {
                var start = expression.LastIndexOf(function);
                var end = expression.IndexOf(")", start);
                if (end == -1)
                {
                    return $"Syntax error: {expression}";
                }

                try
                {
                    var number = ParsePlatformDouble(
                        expression.Substring(start + 8, end - start - 8));
                    number = Math.Round(number, digits);
                    expression = expression.Substring(0, start)
                        + String.Format(RoundFormat(digits), number)
                        + expression.Substring(end + 1, expression.Length - end - 1);
                }
                catch
                {
                    expression = NaN;
                    break;
                }
            }
        }

        for (var digits = 0; digits < 6; digits++)
        {
            var function = $"$n{digits}(";
            while (expression.IndexOf(function) != -1)
            {
                var start = expression.LastIndexOf(function);
                var end = expression.IndexOf(")", start);
                if (end == -1)
                {
                    return $"Syntax error: {expression}";
                }

                try
                {
                    var number = ParsePlatformDouble(
                        expression.Substring(start + 4, end - start - 4));
                    expression = expression.Substring(0, start)
                        + number.ToString($"N{digits}")
                        + expression.Substring(end + 1, expression.Length - end - 1);
                }
                catch
                {
                    expression = NaN;
                    break;
                }
            }
        }

        for (var digits = 0; digits < 6; digits++)
        {
            var function = $"$n{digits}_de(";
            while (expression.IndexOf(function) != -1)
            {
                var start = expression.LastIndexOf(function);
                var end = expression.IndexOf(")", start);
                if (end == -1)
                {
                    return $"Syntax error: {expression}";
                }

                try
                {
                    var number = ParsePlatformDouble(
                        expression.Substring(start + 7, end - start - 7));
                    expression = expression.Substring(0, start)
                        + number.ToString($"N{digits}", CultureInfo.GetCultureInfo("de-DE"))
                        + expression.Substring(end + 1, expression.Length - end - 1);
                }
                catch
                {
                    expression = NaN;
                    break;
                }
            }
        }

        return expression;
    }

    private static string EvaluateFunctions(string expression)
    {
        expression = expression.Replace("$pi()", Math.PI.ToString());
        string[] functions = ["$eval", "$cos", "$sin", "$tan", "$acos", "$asin", "$atan"];

        foreach (var function in functions)
        {
            while (expression.IndexOf(function + "(") != -1)
            {
                var start = expression.LastIndexOf(function + "(");
                var end = -1;
                var level = 0;
                for (var i = start + function.Length + 1; i < expression.Length; i++)
                {
                    if (expression[i] == '(')
                    {
                        level++;
                    }

                    if (expression[i] == ')')
                    {
                        if (level == 0)
                        {
                            end = i;
                            break;
                        }

                        level--;
                    }
                }

                if (end == -1)
                {
                    return "Syntax error: " + expression;
                }

                var evaluated = expression.Substring(
                    start + function.Length + 1,
                    end - start - function.Length - 1);

                foreach (var nestedFunction in functions)
                {
                    if (evaluated.Contains(nestedFunction))
                    {
                        evaluated = EvaluateFunctions(evaluated);
                    }
                }

                evaluated = EvaluateArithmetic(evaluated);
                try
                {
                    evaluated = function switch
                    {
                        "$sin" => Math.Sin(ParsePlatformDouble(evaluated)).ToString(),
                        "$cos" => Math.Cos(ParsePlatformDouble(evaluated)).ToString(),
                        "$tan" => Math.Tan(ParsePlatformDouble(evaluated)).ToString(),
                        "$asin" => Math.Acos(ParsePlatformDouble(evaluated)).ToString(),
                        "$acos" => Math.Asin(ParsePlatformDouble(evaluated)).ToString(),
                        "$atan" => Math.Atan(ParsePlatformDouble(evaluated)).ToString(),
                        _ => evaluated
                    };

                    expression = expression.Substring(0, start)
                        + evaluated
                        + expression.Substring(end + 1, expression.Length - end - 1);
                }
                catch
                {
                    expression = NaN;
                }
            }
        }

        return expression;
    }

    private static string EvaluateArithmetic(string expression)
    {
        try
        {
            expression = expression.Replace("$pi()", Math.PI.ToString());
            expression = expression.Replace(",", ".");
            var table = new DataTable();
            var result = table.Compute(expression, "");
            return Convert.ToDouble(result).ToString();
        }
        catch
        {
            return NaN;
        }
    }

    private static double ParsePlatformDouble(string value)
        => Double.Parse(
            value.Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture.NumberFormat);

    private static string RoundFormat(int digits)
        => digits <= 0
            ? "{0}"
            : $"{{0:0.{"0".PadLeft(digits, '0')}}}";
}
