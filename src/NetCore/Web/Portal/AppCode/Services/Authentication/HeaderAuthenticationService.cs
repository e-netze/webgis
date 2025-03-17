using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Models;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Portal.Core.AppCode.Services.Authentication;

public class HeaderAuthenticationService
{
    private readonly ConfigurationService _config;

    public HeaderAuthenticationService(ConfigurationService config)
    {
        _config = config;
    }

    public CustomAuthenticationUser GetUser(HttpContext httpContext)
    {
        NameValueCollection headers = httpContext.Request.HeadersCollection();

        string username =
            headers.GetValues(_config.HeaderAuthenticationUsernameVariable())?.FirstOrDefault();

        string[] roles =
            headers.GetValues(_config.HeaderAuthenticationRolesVariable()) ?? new string[0];

        if (String.IsNullOrEmpty(username))
        {
            return null;
        }

        List<string> allroles = new List<string>();
        List<string> allrolesParams = new List<string>();

        if ("InsideBrackets".Equals(_config.HeaderAuthenticationExtractRoleParameters(), StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (string role in roles)
            {
                foreach (string r in role.Split(_config.HeaderAuthenticationRoleSeparator()))
                {
                    string r2 = r.Trim(); string rParams = String.Empty;

                    if (r2.Contains("(") && r2.EndsWith(")"))
                    {
                        rParams = r2.Substring(r2.IndexOf("(") + 1, r2.Length - r2.IndexOf("(") - 2);

                        foreach (string rParam in rParams.Split(_config.HeaderAuthenticationRoleParametersSeparator()))
                        {
                            if (String.IsNullOrEmpty(rParam.Trim()))
                            {
                                continue;
                            }

                            if (!allrolesParams.Contains(rParam.Trim()))
                            {
                                allrolesParams.Add(rParam.Trim());
                            }
                        }

                        r2 = r2.Substring(0, r2.IndexOf("("));
                        r2 = r2.Trim();
                    }
                    allroles.Add(r2);
                }
            }
        }
        else
        {
            foreach (string role in roles)
            {
                allroles.AddRange(
                    role
                        .Split(_config.HeaderAuthenticationRoleSeparator())
                        .Select(r => r.Trim())
                    );
            }
        }

        var userRoles = allroles.ToArray();
        var userRoleParameters = (allrolesParams.Count > 0 ? allrolesParams.ToArray() : null);

        return new CustomAuthenticationUser()
        {
            Username = UserManagement.AppendUserPrefix(username, _config.HeaderAuthenticationUserPrefix()),
            Roles = UserManagement.AppendUserPrefix(userRoles, _config.HeaderAuthenticationRolePrefix()),
            RoleParameters = userRoleParameters,
            SetCookie = false
        };
    }
}
