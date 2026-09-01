namespace E.Standard.WebApp.Services;

/// <summary>
/// Describes the kind of value a <see cref="BrandStyleVariable"/> holds. Used by
/// <see cref="BrandStyleService"/> to select the appropriate validation logic
/// before rendering the value into CSS.
/// </summary>
enum BrandStyleVariableType
{
    /// <summary>
    /// A CSS color value (hex, rgb()/rgba(), hsl()/hsla(), oklch(), color-mix(), var() or a named color).
    /// </summary>
    Color,

    /// <summary>
    /// A CSS length/dimension value (e.g. padding, margin, border-radius), such as
    /// <c>4px</c>, <c>0.5rem</c> or <c>1em</c>.
    /// </summary>
    Length,

    /// <summary>
    /// A CSS <c>url(...)</c> reference (e.g. for a background image or a logo), such as
    /// <c>url(https://example.com/logo.png)</c> or <c>url(/content/logo.png)</c>.
    /// </summary>
    Url
}
