namespace E.Standard.Parsing.SimpleExpressions;

/// <summary>
/// Solves simple [KEY] template expressions. Keys are replaced sequentially over the whole
/// expression (string.Replace), so values containing [KEY] placeholders may be replaced again.
/// </summary>
/// <remarks>
/// Supported key forms (examples):
/// <list type="bullet">
/// <item><c>Parcel [KG]-[GNR]</c> – plain field values.</item>
/// <item><c>[KG;GNR]</c> – multiple keys in one bracket (split at ';', each replaced on its own).</item>
/// <item><c>https://host/info?id=[!ID]</c> – required value: if ID is empty, the whole result is an empty string.</item>
/// <item><c>[AREA:0.00] m²</c> – <see cref="string.Format(string, object)"/> format; numeric values are parsed first.</item>
/// <item><c>[DATE:yyyy-MM-dd]</c> – format applied to the raw value if it is not numeric.</item>
/// <item><c>?q=[url-encode:NAME]</c> – value is URL encoded (UTF-8).</item>
/// <item><c>?q=[url-encode-latin1:NAME]</c> – value is URL encoded using Windows-1252.</item>
/// <item><c>[~NAME]</c> – legacy prefix (formerly used for 1:n hotlinks); now the same as <c>[NAME]</c>.</item>
/// <item><c>[BBOX]</c>, <c>[spatial::point::4326]</c>, <c>[spatial::latlng]</c> – special keys resolved by
/// <see cref="ISimpleExpressionValueResolver.TryResolveSpecialKey"/>; unresolved values keep the placeholder.</item>
/// </list>
/// </remarks>
public static class SimpleExpression
{
    private const string UrlEncodePrefix = "url-encode:";
    private const string UrlEncodeLatin1Prefix = "url-encode-latin1:";

    public static string Solve<TResolver>(
        string expression,
        IReadOnlyList<string>? keys,
        TResolver? resolver)
        where TResolver : ISimpleExpressionValueResolver
    {
        if (keys == null || resolver is null)
        {
            return expression;
        }

        foreach (string key in keys)
        {
            if (resolver.TryResolveSpecialKey(key, out string? specialValue))
            {
                if (specialValue != null)
                {
                    expression = Replace(expression, key, specialValue);
                }
            }
            else if (key.StartsWith(UrlEncodePrefix))
            {
                expression = Replace(expression, key,
                    Uri.EscapeDataString(resolver.GetValue(key.Substring(UrlEncodePrefix.Length))!));
            }
            else if (key.StartsWith(UrlEncodeLatin1Prefix))
            {
                expression = Replace(expression, key,
                    resolver.UrlEncodeLatin1(resolver.GetValue(key.Substring(UrlEncodeLatin1Prefix.Length))));
            }
            else if (key.Contains(':'))
            {
                expression = Replace(expression, key, FormatValue(key, resolver));
            }
            else if (key.StartsWith('~'))
            {
                expression = Replace(expression, key, resolver.GetValue(key.Substring(1)));
            }
            else if (key.StartsWith('!'))  // required parameter
            {
                string? value = resolver.GetValue(key.Substring(1));
                if (String.IsNullOrEmpty(value))
                {
                    return String.Empty;
                }

                expression = Replace(expression, key, value);
            }
            else
            {
                expression = Replace(expression, key, resolver.GetValue(key));
            }
        }

        return expression;
    }

    private static string FormatValue<TResolver>(string key, TResolver resolver)
        where TResolver : ISimpleExpressionValueResolver
    {
        int pos = key.IndexOf(':');
        string format = "{0:" + key.Substring(pos + 1) + "}";
        string? value = resolver.GetValue(key.Substring(0, pos));

        return resolver.TryParseNumber(value, out double number)
            ? String.Format(format, number)
            : String.Format(format, value);
    }

    private static string Replace(string expression, string key, string? value)
        => expression.Replace($"[{key}]", value);
}
