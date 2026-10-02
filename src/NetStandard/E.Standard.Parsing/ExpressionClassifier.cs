namespace E.Standard.Parsing;

public enum ExpressionSyntax
{
    LegacyTemplate,
    StructuredExpression
}

public static class ExpressionClassifier
{
    public static ExpressionSyntax Classify(string? expression)
    {
        var source = expression?.Trim() ?? String.Empty;
        if (source.Length == 0)
        {
            return ExpressionSyntax.LegacyTemplate;
        }

        if (source[0] is '"' or '(' or '-' or '!'
            || Char.IsDigit(source[0])
            || StartsWithKeywordExpression(source, "true")
            || StartsWithKeywordExpression(source, "false")
            || StartsWithKeywordExpression(source, "null"))
        {
            return ExpressionSyntax.StructuredExpression;
        }

        var inString = false;
        var inField = false;
        var escaped = false;
        var startsWithField = source[0] == '[';
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (inField)
            {
                if (current == ']')
                {
                    inField = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == '[')
            {
                inField = true;
                continue;
            }

            if (startsWithField && "+-*/%<>=&|!".Contains(current))
            {
                return ExpressionSyntax.StructuredExpression;
            }
        }

        var identifierLength = 0;
        while (identifierLength < source.Length
               && (Char.IsLetterOrDigit(source[identifierLength]) || source[identifierLength] == '_'))
        {
            identifierLength++;
        }

        while (identifierLength < source.Length && Char.IsWhiteSpace(source[identifierLength]))
        {
            identifierLength++;
        }

        return identifierLength < source.Length && source[identifierLength] == '('
            ? ExpressionSyntax.StructuredExpression
            : ExpressionSyntax.LegacyTemplate;
    }

    private static bool StartsWithKeywordExpression(string source, string keyword)
    {
        if (!source.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var position = keyword.Length;
        while (position < source.Length && Char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        return position == source.Length
            || "+-*/%<>=&|!".Contains(source[position]);
    }
}
