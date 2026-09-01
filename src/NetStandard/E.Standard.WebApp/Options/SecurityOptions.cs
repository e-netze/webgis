using Microsoft.Extensions.Primitives;

namespace E.Standard.WebApp.Options;

public class SecurityOptions
{
    public bool DisableAntiforgery { get; set; } = false;

    public string EndpointAuthorizationUrlPassword { get; set; } = "";
    public string EndpointAuthorizationBasicUsername { get; set; } = "";
    public string EndpointAuthorizationBasicPassword { get; set; } = "";
    public string EndpointAuthorizationBearerUsername { get; set; } = "";
}
