namespace E.Standard.WebMapping.Core.Renderer.UserRenderers;

/// <summary>
/// Global settings for user-defined renderers ("Darstellung" in the viewer TOC).
/// </summary>
/// <remarks>
/// Set via <c>api.config</c>, section <c>toc-contextmenu</c>:
/// <c>allow-toc-userrenderer</c> (<see cref="Allowed"/>),
/// <c>userrenderer-max-unique-value-classes</c> (<see cref="MaxUniqueValueClasses"/>).
/// </remarks>
public static class UserRendererSettings
{
    /// <summary>
    /// Global switch (default: false). If false, user-defined renderers are disabled for all services
    /// (regardless of the CMS property allow_user_renderers)
    /// </summary>
    public static bool Allowed { get; set; } = false;

    /// <summary>
    /// Max. number of classes for a unique value renderer
    /// </summary>
    public static int MaxUniqueValueClasses { get; set; } = UserRendererValidator.DefaultMaxUniqueValueClasses;
}
