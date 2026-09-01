namespace E.Standard.WebApp.Reflection;

[Flags]
public enum EndpointAuthorizationType
{
    UrlPassword = 1,
    Basic = 2,
    BearerToken = 4,
    Localhost = 8
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class EndpointAuthorizationAttribute : Attribute
{
    public EndpointAuthorizationType AuthorizationType { get; set; }
    public bool AllowIfNotConfigured { get; set; } = false;
}
