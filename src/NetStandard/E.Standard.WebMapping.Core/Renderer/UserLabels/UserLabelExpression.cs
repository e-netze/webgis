#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Renderer.UserLabels;

public enum UserLabelExpressionTokenType
{
    Text,
    Field,
    NewLine
}

public record UserLabelExpressionToken(UserLabelExpressionTokenType Type, string Value);

/// <summary>
/// Simple label expressions: literal text with field placeholders in square brackets and line breaks,
/// e.g. "Fläche: [FLAECHE] m²". Every [...] is a field placeholder.
/// Double quotes are removed from the text, because they can't be escaped in all service syntaxes.
/// </summary>
public static class UserLabelExpression
{
    private static readonly Regex FieldRegex = new Regex(@"\[([^\[\]\r\n]+)\]", RegexOptions.Compiled);

    public static IReadOnlyList<UserLabelExpressionToken> Parse(string? expression)
    {
        var tokens = new List<UserLabelExpressionToken>();

        if (String.IsNullOrEmpty(expression))
        {
            return tokens;
        }

        var lines = expression!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                tokens.Add(new UserLabelExpressionToken(UserLabelExpressionTokenType.NewLine, "\n"));
            }

            var line = lines[i];
            int pos = 0;

            foreach (Match match in FieldRegex.Matches(line))
            {
                AddText(tokens, line.Substring(pos, match.Index - pos));
                tokens.Add(new UserLabelExpressionToken(UserLabelExpressionTokenType.Field, match.Groups[1].Value.Trim()));
                pos = match.Index + match.Length;
            }

            AddText(tokens, line.Substring(pos));
        }

        return tokens;
    }

    public static IEnumerable<string> Fields(string? expression)
        => Parse(expression)
            .Where(t => t.Type == UserLabelExpressionTokenType.Field)
            .Select(t => t.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static int LineCount(string? expression)
        => String.IsNullOrEmpty(expression)
            ? 0
            : Parse(expression).Count(t => t.Type == UserLabelExpressionTokenType.NewLine) + 1;

    private static void AddText(List<UserLabelExpressionToken> tokens, string text)
    {
        text = text.Replace("\"", "");
        if (text.Length > 0)
        {
            tokens.Add(new UserLabelExpressionToken(UserLabelExpressionTokenType.Text, text));
        }
    }
}
