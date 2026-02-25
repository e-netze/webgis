using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Portal.Core.AppCode;

public class PortalUser
{
    public PortalUser(string username, string displayName = null)
        : this(username, null, null, displayName)
    {

    }
    public PortalUser(string username, string[] userRoles, string[] roleParameters, string displayName = null)
    {
        this.Username = username ?? String.Empty;
        this.UserRoles = userRoles ?? new string[0];
        this.RoleParameters = roleParameters ?? new string[0];
        this._displayName = displayName;
    }

    public string Username { get; private set; }
    public string[] UserRoles { get; private set; }
    public string[] RoleParameters { get; private set; }

    public void AddRoles(IEnumerable<string> roles)
    {
        if (roles != null && roles.Count() > 0)
        {
            List<string> currentRoles = new List<string>(this.UserRoles ?? new string[0]);
            currentRoles.AddRange(roles);
            this.UserRoles = currentRoles.ToArray();
        }
    }

    public void AddRoleParameters(IEnumerable<string> roleParameters)
    {
        if (roleParameters != null && roleParameters.Count() > 0)
        {
            List<string> currentRoleParameters = new List<string>(this.RoleParameters ?? new string[0]);
            currentRoleParameters.AddRange(roleParameters);
            this.RoleParameters = currentRoleParameters.ToArray();
        }
    }

    private string _displayName;
    public string DisplayName
    {
        get
        {
            if (String.IsNullOrWhiteSpace(_displayName))
            {
                return this.Username;
            }

            return _displayName;
        }
    }

    public string UsernameGroupsString()
    {
        if (String.IsNullOrWhiteSpace(Username))
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(this.Username);

        if (UserRoles != null && UserRoles.Length > 0)
        {
            for (int i = 0; i < UserRoles.Length; i++)
            {
                var role = UserRoles[i];
                if (String.IsNullOrWhiteSpace(role))
                {
                    continue;
                }

                if (role.Contains("|"))
                {
                    throw new Exception("Forbidden charater (|) in role: " + role);
                }

                sb.Append("|" + role);
            }
        }

        return sb.ToString();
    }

    public bool IsAnonymous => String.IsNullOrWhiteSpace(this.Username);

    #region Static Members

    public static PortalUser Anonymous => new PortalUser(String.Empty);

    #endregion
}
