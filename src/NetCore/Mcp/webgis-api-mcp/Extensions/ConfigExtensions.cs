namespace WebGIS.API.MCP.Extensions;

internal static class ConfigExtensions
{
    static public bool UseAuthentication(this IConfiguration config)
    {
        return config.GetValue<bool>("Authentication:Use", false) && !String.IsNullOrEmpty(config.GetAuthenticationAuthority());
    }

    static public string GetAuthenticationAuthority(this IConfiguration config)
    {
        return config.GetValue<string>("Authentication:Authority", String.Empty) ?? String.Empty;
    }

    static public string[] GetAuthenticationScopes(this IConfiguration config)
    {
        var scopes = config.GetValue<string>("Authentication:Scopes", String.Empty) ?? String.Empty;
        return scopes.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).ToArray();
    }

    static public string GetAuthenticationAudience(this IConfiguration config)
    {
        return config.GetValue<string>("Authentication:Audience", String.Empty) ?? String.Empty;
    }

    static public string GetMcpServerUrl(this IConfiguration config)
    {
        return config.GetValue<string>("McpServer:Url", "http://localhost:5000") ?? "http://localhost:5000";
    }
}
