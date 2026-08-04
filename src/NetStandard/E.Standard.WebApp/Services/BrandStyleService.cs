using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.WebApp.Services;

/// <summary>
/// Renders CSS custom properties (":root" variables) for brand colors that customers
/// can override via environment variables. This is primarily meant for containerized
/// deployments (e.g. Kubernetes) where the shipped "site.overrides.css" file cannot be
/// modified by the customer, but environment variables can easily be injected via
/// ConfigMaps/Secrets.
/// </summary>
public class BrandStyleService
{
    // Fixed, well-known list of supported environment variables and their matching
    // CSS custom property. Extend this list when new brand variables are introduced.
    private static readonly IReadOnlyList<BrandStyleVariable> _variables = new[]
    {
        new BrandStyleVariable("CSS_WEBGIS_BRAND_PRIMARY", "--webgis-brand-primary"),
        new BrandStyleVariable("CSS_WEBGIS_BRAND_PRIMARY_LIGHT", "--webgis-brand-primary-light"),
        new BrandStyleVariable("CSS_WEBGIS_BRAND_PRIMARY_LIGHT_TEXT_COLOR", "--webgis-brand-primary-light-text-color"),
        new BrandStyleVariable("CSS_WEBGIS_BRAND_LOGO", "--webgis-brand-logo", BrandStyleVariableType.Url),
        new BrandStyleVariable("CSS_WEBGIS_UI_SURFACE_NAVBAR", "--webgis-ui-surface-navbar"),
        new BrandStyleVariable("CSS_WEBGIS_UI_TEXT_NAVBAR", "--webgis-ui-text-navbar"),
    };

    // Whitelist of allowed CSS color/value formats. This is a defense-in-depth measure:
    // the value is rendered unescaped (via @Html.Raw) inside an inline <style> block, so
    // a malformed or malicious environment variable value (e.g. containing "</style>")
    // must never be able to break out of the CSS context.
    private static readonly Regex _validCssColorValue = new(
        @"^(" +
        @"#[0-9a-fA-F]{3,8}" +                                  // hex, e.g. #a00, #aa0000, #aa0000ff
        @"|rgba?\([0-9.,%\s]+\)" +                              // rgb()/rgba()
        @"|hsla?\([0-9.,%\s]+\)" +                              // hsl()/hsla()
        @"|oklch\([0-9.%\s/]+\)" +                              // oklch()
        @"|color-mix\(\s*in\s+[a-zA-Z0-9]+[0-9.,%\s]*,\s*(var\(--[a-zA-Z0-9-]+\)|#[0-9a-fA-F]{3,8}|[a-zA-Z]+)\s+[0-9.]+%,\s*(var\(--[a-zA-Z0-9-]+\)|#[0-9a-fA-F]{3,8}|[a-zA-Z]+)\s*\)" + // color-mix()
        @"|var\(--[a-zA-Z0-9-]+\)" +                            // var(--custom-property)
        @"|[a-zA-Z]+" +                                         // named color, e.g. red
        @")$",
        RegexOptions.Compiled);

    // Whitelist of allowed CSS length/dimension values (e.g. for paddings, margins,
    // border-radius, ...). Same defense-in-depth rationale as _validCssColorValue.
    // Supports CSS shorthand notation with 1 to 4 space-separated length tokens,
    // e.g. "4px", "4px 10px" or "4px 10px 4px 10px".
    private const string CssLengthToken =
        @"calc\([0-9a-zA-Z%.,\s+\-*/()]+\)" +                   // calc(...)
        @"|var\(--[a-zA-Z0-9-]+\)" +                            // var(--custom-property)
        @"|-?[0-9]*\.?[0-9]+(px|rem|em|%|vh|vw|vmin|vmax|pt|ch|ex|cm|mm|in|pc)" + // numeric length with unit
        @"|0";                                                  // unitless zero

    private static readonly Regex _validCssLengthValue = new(
        @"^(" + CssLengthToken + @")" +                         // first token
        @"(\s+(" + CssLengthToken + @")){0,3}$",                // up to 3 additional tokens
        RegexOptions.Compiled);

    // Whitelist of allowed CSS url(...) values (e.g. for a brand logo). Same
    // defense-in-depth rationale as _validCssColorValue. Only http(s) and root-relative
    // paths are allowed as the URL scheme, to prevent injection of "javascript:" or
    // other dangerous schemes; the URL itself may optionally be wrapped in matching
    // single or double quotes.
    private static readonly Regex _validCssUrlValue = new(
        @"^url\(\s*['""]?(https?:)?/{1,2}[^\s'""()]+['""]?\s*\)$",
        RegexOptions.Compiled);

    // Environment variables don't change during the lifetime of the process, so the
    // rendered CSS is computed once (lazily) and cached in memory. A restart is
    // required to pick up changed environment variables.
    private readonly Lazy<string> _brandVariablesCss;

    public BrandStyleService()
    {
        _brandVariablesCss = new Lazy<string>(BuildBrandVariablesCss);
    }

    /// <summary>
    /// Returns the CSS custom properties (without the surrounding ":root { ... }") for
    /// all brand environment variables that are currently set, e.g.:
    /// <code>--webgis-brand-primary:#a00;--webgis-brand-primary-light:#faa;</code>
    /// Returns an empty string if none of the known environment variables are set.
    /// </summary>
    public string GetBrandVariablesFromEnvironment()
        => _brandVariablesCss.Value;

    private static string BuildBrandVariablesCss()
    {
        var sb = new StringBuilder();

        foreach (var variable in _variables)
        {
            var value = Environment.GetEnvironmentVariable(variable.EnvironmentVariableName);

            if (!string.IsNullOrWhiteSpace(value))
            {
                value = value.Trim();

                var isValid = variable.Type switch
                {
                    BrandStyleVariableType.Length => _validCssLengthValue.IsMatch(value),
                    BrandStyleVariableType.Url => _validCssUrlValue.IsMatch(value),
                    _ => _validCssColorValue.IsMatch(value),
                };

                if (isValid)
                {
                    sb.Append(variable.CssVariableName)
                      .Append(':')
                      .Append(value)
                      .Append(';');
                }
            }
        }

        return sb.ToString();
    }
}
