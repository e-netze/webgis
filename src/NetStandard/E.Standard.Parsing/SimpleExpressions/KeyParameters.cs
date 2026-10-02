namespace E.Standard.Parsing.SimpleExpressions;

public static class KeyParameters
{
    /// <summary>
    /// Returns the [KEY] parameters of a simple expression. Keys containing ';' are split into
    /// multiple keys; leading empty keys are skipped. Returns null if no keys are found.
    /// </summary>
    public static string[]? Parse(string? expression)
    {
        var parameters = TemplateScanner
            .Scan(expression)
            .SelectMany(parameter => parameter.Key.Split(';'))
            .SkipWhile(String.IsNullOrEmpty)
            .ToArray();

        return parameters.Length == 0 ? null : parameters;
    }
}
