using System;
using System.Collections.Generic;

namespace E.Standard.Api.App.Configuration;

/// <summary>
/// Options for services that users add themselves by url in the viewer ("add custom service",
/// <c>/rest/customserviceinfo</c> and the <c>custom.{id}.connection</c> parameter of map requests).
/// </summary>
/// <remarks>
/// Configurable via <c>api.config</c>
/// (bound by <c>ConfigurationServiceExtensions.BindCustomServicesOptions</c>):
/// <list type="bullet">
/// <item><c>security:allow-custom-services</c> → <see cref="Allow"/></item>
/// <item><c>security:custom-services-allow-hosts</c> → <see cref="AllowHosts"/></item>
/// <item><c>security-add-custom-services-host-blacklist</c> (legacy key) → <see cref="HostBlacklist"/></item>
/// </list>
/// </remarks>
public class CustomServicesOptions
{
    /// <summary>
    /// Custom services are only accepted by the server if true. The client option
    /// <c>webgis.usability.allowAddCustomServices</c> only shows the UI. Default: false.
    /// </summary>
    public bool Allow { get; set; } = false;

    /// <summary>
    /// Host names (or <c>*.domain</c> patterns, <c>*</c> = all hosts) that may resolve to loopback,
    /// private or link-local addresses. All other hosts must resolve to public addresses only.
    /// </summary>
    public IReadOnlyCollection<string> AllowHosts { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Regular expressions for forbidden hosts. The entry <c>forbidden</c> disables custom services completely.
    /// </summary>
    public IReadOnlyCollection<string> HostBlacklist { get; set; } = Array.Empty<string>();
}
