namespace E.Standard.WebApp.Services;

/// <summary>
/// Describes a single brand CSS custom property that can be overridden via an
/// environment variable (e.g. for Kubernetes deployments where the CSS files
/// cannot be edited by the customer).
/// </summary>
sealed class BrandStyleVariable
{
    public BrandStyleVariable(string environmentVariableName, string cssVariableName, BrandStyleVariableType type = BrandStyleVariableType.Color)
    {
        EnvironmentVariableName = environmentVariableName;
        CssVariableName = cssVariableName;
        Type = type;
    }

    /// <summary>
    /// Name of the environment variable, e.g. <c>CSS_WEBGIS_BRAND_PRIMARY</c>.
    /// </summary>
    public string EnvironmentVariableName { get; }

    /// <summary>
    /// Name of the resulting CSS custom property, e.g. <c>--webgis-brand-primary</c>.
    /// </summary>
    public string CssVariableName { get; }

    /// <summary>
    /// The kind of value this variable holds, used to select the appropriate
    /// validation logic when rendering the value into CSS.
    /// </summary>
    public BrandStyleVariableType Type { get; }
}
