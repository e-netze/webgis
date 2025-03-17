namespace E.Standard.Custom.Core.Models;

public class PortalAuthenticationServiceUser
{
    public string Username { get; set; }
    public string[] UserRoles { get; set; }

    public string[] RoleParameters { get; set; }

    public string DisplayName { get; set; }
}
