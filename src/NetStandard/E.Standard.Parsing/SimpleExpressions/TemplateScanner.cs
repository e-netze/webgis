namespace E.Standard.Parsing.SimpleExpressions;

public sealed record TemplateParameter(string Key, int Start, int Length);

public static class TemplateScanner
{
    public static IReadOnlyList<TemplateParameter> Scan(string? template)
    {
        if (String.IsNullOrEmpty(template))
        {
            return [];
        }

        var result = new List<TemplateParameter>();
        var searchFrom = 0;

        while (searchFrom < template.Length)
        {
            var start = template.IndexOf('[', searchFrom);
            if (start < 0)
            {
                break;
            }

            var end = template.IndexOf(']', start);
            if (end < 0)
            {
                break;
            }

            result.Add(new TemplateParameter(
                template.Substring(start + 1, end - start - 1),
                start,
                end - start + 1));
            searchFrom = end + 1;
        }

        return result;
    }

    public static string Replace(
        string template,
        Func<TemplateParameter, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(resolver);

        var parameters = Scan(template);
        if (parameters.Count == 0)
        {
            return template;
        }

        var result = new System.Text.StringBuilder(template.Length);
        var sourcePosition = 0;
        foreach (var parameter in parameters)
        {
            result.Append(template, sourcePosition, parameter.Start - sourcePosition);
            result.Append(resolver(parameter) ?? template.Substring(parameter.Start, parameter.Length));
            sourcePosition = parameter.Start + parameter.Length;
        }

        result.Append(template, sourcePosition, template.Length - sourcePosition);
        return result.ToString();
    }
}
