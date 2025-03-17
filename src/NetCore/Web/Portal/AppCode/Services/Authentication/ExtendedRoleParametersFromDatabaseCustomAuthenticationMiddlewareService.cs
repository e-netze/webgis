using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.DbConnector;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

public class ExtendedRoleParametersFromDatabaseCustomAuthenticationMiddlewareService : ICustomPortalAuthenticationMiddlewareService
{
    private readonly string _source;
    private readonly string _statement;

    public ExtendedRoleParametersFromDatabaseCustomAuthenticationMiddlewareService(ConfigurationService config)
    {
        _source = config.ExtendedRoleParametersSource();
        _statement = config.ExtendedRoleParametersStatement();
    }

    public bool ForceInvoke(HttpContext httpContext) => true;

    async public Task<CustomAuthenticationUser> InvokeFromMiddleware(HttpContext httpContext)
    {
        try
        {
            if (httpContext.User?.Identity != null &&
                httpContext.User.Identity.IsAuthenticated &&
                !String.IsNullOrEmpty(_source) &&
                !String.IsNullOrEmpty(_statement))
            {
                using (var dbFactory = new DBFactory(_source))
                using (var connection = dbFactory.GetConnection())
                {
                    var command = dbFactory.GetCommand(connection);
                    command.CommandText = _statement;

                    if (command.CommandText.Contains("@username"))
                    {
                        string username = httpContext.User.Identity.Name.RemoveAuthPrefix();
                        command.Parameters.Add(dbFactory.GetParameter("username", /*dbFactory.EncodeString(username)*/ username));  // Muss man noch encoden, wenn man das per Parameter übergibt?!

                    }

                    if (command.CommandText.Contains("@pvp_gvgid"))
                    {
                        string pvp_gvGid = httpContext.Request.Headers["X-AUTHENTICATE-gvGid"].ToString();
                        command.Parameters.Add(dbFactory.GetParameter("pvp_gvgid", /*dbFactory.EncodeString(pvp_gvGid)*/ pvp_gvGid));  // Muss man noch encoden, wenn man das per Parameter übergibt?!
                    }

                    List<string> extendedRoleParameters = new List<string>();
                    List<string> extendedRoles = new List<string>();

                    connection.Open();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        int rowCount = 0;
                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var val = reader.GetValue(i);
                                if (val == null || val == DBNull.Value)
                                {
                                    continue;
                                }

                                string fieldName = reader.GetName(i);
                                if (fieldName.ToLower() == "webgisaddroles")
                                {
                                    foreach (string addRole in reader.GetValue(i).ToString().Replace(";", ",").Split(','))
                                    {
                                        if (String.IsNullOrWhiteSpace(addRole))
                                        {
                                            continue;
                                        }

                                        extendedRoles.Add(addRole.Trim());
                                    }
                                }
                                else
                                {
                                    if (rowCount > 0)
                                    {
                                        fieldName += "[" + rowCount + "]";
                                    }

                                    string roleParameterVals = reader.GetValue(i).ToString();
                                    foreach (var roleParameterVal in roleParameterVals.Split('~'))
                                    {
                                        extendedRoleParameters.Add($"{fieldName}={roleParameterVal.Trim()}");
                                    }
                                }
                            }
                            rowCount++;
                        }
                    }

                    if (extendedRoleParameters.Count > 0 || extendedRoles.Count > 0)
                    {
                        return new CustomAuthenticationUser()
                        {
                            Roles = extendedRoles.ToArray(),
                            RoleParameters = extendedRoleParameters.ToArray(),
                            AppendRolesAndParameters = true
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }

        return null;
    }
}
