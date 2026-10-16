namespace E.Standard.WebMapping.Core.Renderer.UserLabels;

/// <summary>
/// Global settings for user-defined labeling ("Beschriftung" in the viewer TOC).
/// </summary>
/// <remarks>
/// Set via <c>api.config</c>, section <c>toc-contextmenu</c>: <c>allow-toc-userlabeling</c> (<see cref="Allowed"/>)
/// </remarks>
public static class UserLabelingSettings
{
    /// <summary>
    /// Global switch (default: false). If false, user-defined labeling is disabled for all services
    /// (regardless of the CMS property allow_user_labeling)
    /// </summary>
    public static bool Allowed { get; set; } = false;
}
