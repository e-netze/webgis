using E.Standard.WebGIS.Core;
using System;

namespace E.Standard.Custom.Core.Models;

public class CustomAuthenticationUser
{
    public CustomAuthenticationUser()
    {
        this.AppendRolesAndParameters = false;
        this.SetCookie = true;
    }

    public string UserId { get; set; }
    public string Username { get; set; }
    public string[] Roles { get; set; }
    public string[] RoleParameters { get; set; }

    public UserType UserType { get; set; }
    public DateTimeOffset? Expires { get; set; }

    public bool AppendRolesAndParameters { get; set; }
    public bool SetCookie { get; set; }

    public string CookieValue { get; set; }
}
