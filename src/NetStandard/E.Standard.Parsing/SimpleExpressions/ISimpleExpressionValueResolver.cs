namespace E.Standard.Parsing.SimpleExpressions;

public interface ISimpleExpressionValueResolver
{
    string? GetValue(string fieldName);

    /// <summary>
    /// Resolves keys with a special meaning for the caller (e.g. spatial keys).
    /// Returns false if the key is not handled and should be resolved as a field key.
    /// A handled key with a null replacement leaves the placeholder unchanged.
    /// </summary>
    bool TryResolveSpecialKey(string key, out string? replacement);

    bool TryParseNumber(string? value, out double number);

    string UrlEncodeLatin1(string? value);
}
