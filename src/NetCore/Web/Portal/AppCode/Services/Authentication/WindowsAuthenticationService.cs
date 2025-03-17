using E.Standard.ActiveDirectory;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Portal.Core.AppCode.Services.Authentication;

public class WindowsAuthenticationService : IPortalAuthenticationService
{
    private readonly WebgisApiService _api;
    private readonly ConfigurationService _config;
    private readonly InMemoryPortalAppCache _cache;
    private readonly IWebHostEnvironment _environment;

    public WindowsAuthenticationService(
        WebgisApiService api,
        ConfigurationService config,
        InMemoryPortalAppCache cache,
        IWebHostEnvironment environment)
    {
        _api = api;
        _config = config;
        _cache = cache;
        _environment = environment;
    }

    public UserType UserType => UserType.WindowsUser;

    async public Task<PortalAuthenticationServiceUser> TryAuthenticationServiceUser(HttpContext context, string user, bool cache = false)
    {
        if (String.IsNullOrEmpty(user))
        {
            return new PortalAuthenticationServiceUser();
        }

        user = user.StartsWith("nt-user::") ? user.Substring(9) : user;

        PortalAuthenticationServiceUser windowsUser = new PortalAuthenticationServiceUser()
        {
            Username = UserManagement.AppendUserPrefix(user, UserType.WindowsUser)
        };

        if (cache == true)
        {
            var cachedRoles = _cache.GetUserRoles("windows-auth:" + user);
            if (cachedRoles != null)
            {
                windowsUser.UserRoles = cachedRoles;
                return windowsUser;
            }
        }

        if (!String.IsNullOrEmpty(_config[PortalConfigKeys.SecurityWindowsGetGroupDirectoryEntry]))
        {
            IImpersonateUser impersonateUser = null;
            try
            {
                string[] impuser = _config[PortalConfigKeys.ImpersonateUser].Split('|');
                if (impuser != null && impuser.Length == 3)
                {
                    impersonateUser = impersonateUser = ActiveDirectoryFactory.InterfaceImplementation<IImpersonateUser>();
                    impersonateUser.Impersonate(impuser[0], impuser[1], impuser[2]);
                }

                string u = user, domain = String.Empty;
                StringBuilder groups = new StringBuilder();
                if (u.Contains(@"\"))
                {
                    domain = user.Substring(0, user.IndexOf(@"\"));
                    u = u.Substring(domain.Length + 1, u.Length - domain.Length - 1);

                    string domainSubstitute = _config[PortalConfigKeys.SecurityWindowsDomainSubstitute];
                    if (!String.IsNullOrEmpty(domainSubstitute))
                    {
                        int pos = domainSubstitute.IndexOf("=");
                        if (pos != -1)
                        {
                            string d1 = domainSubstitute.Substring(0, pos);
                            string d2 = domainSubstitute.Substring(pos + 1, domainSubstitute.Length - pos - 1);

                            if (d1.ToLower() == domain.ToLower())
                            {
                                domain = d2;
                            }
                        }
                    }
                }

                string dea = _config[PortalConfigKeys.SecurityWindowsGetGroupDirectoryEntry];
                dea = dea.Replace("[DOMAIN]", domain);
                var adQuery = ActiveDirectoryFactory.InterfaceImplementation<IAdQuery>(dea);
                bool recursiveSearch = _config.Get<bool>(PortalConfigKeys.SecurityWindowsGetGroupRecursiv);
                windowsUser.UserRoles =
                    (adQuery.UserRoles(u, recursiveSearch) ?? new string[0])
                    .OrderBy(r => r)
                    .ToArray();

                bool succeeded = windowsUser.UserRoles != null && windowsUser.UserRoles.Length > 0;
                var thinedUserRoles = ThinUserRolesFromAllCMS(windowsUser.UserRoles, await _api.ApiCmsUserRoles(context.Request), true);

                if (thinedUserRoles != null)
                {
                    windowsUser.UserRoles = thinedUserRoles.OrderBy(r => r).ToArray();
                }

                windowsUser.UserRoles = UserManagement.AppendUserPrefix(windowsUser.UserRoles, UserType.WindowsGroup);

                if (cache == true && _cache.AllCmsRoles != null)
                {
                    _cache.SetUserRoles("windows-auth:" + user, windowsUser.UserRoles);
                }
            }
            finally
            {
                if (impersonateUser != null)
                {
                    impersonateUser.Undo();
                }
            }
        }

        return windowsUser;
    }

    #region Helper

    public string[] ThinUserRolesFromAllCMS(string[] userroles, string[] cmsUserRoles, bool includeCompatibility = false)
    {
        Dictionary<string, CmsDocument> allcms = new Dictionary<string, CmsDocument>();
        UniqueList uList = new UniqueList();
        UniqueList cmsList = new UniqueList();

        if (cmsUserRoles != null)
        {
            foreach (var cmsUserRole in cmsUserRoles)
            {
                cmsList.Add(cmsUserRole.ToLower());
            }
        }

        if (includeCompatibility)
        {
            try
            {
                XmlDocument config = new XmlDocument();
                config.Load($"{_environment.ContentRootPath}/_config/compatibility.config");

                foreach (XmlNode cmsDocNode in config.SelectNodes("//cms-documents/cms-document[@path and @name]"))
                {
                    string cmsName = cmsDocNode.Attributes["name"].Value + Guid.NewGuid().ToString("N");
                    CmsDocument cmsDocument = new CmsDocument(cmsName, _environment.ContentRootPath, $"{_environment.ContentRootPath}/../etc/",
                                                              Array.Empty<ICustomCmsDocumentAclProviderService>());
                    cmsDocument.ReadXml(cmsDocNode.Attributes["path"].Value);

                    foreach (var cmsRole in cmsDocument.AllRoles)
                    {
                        cmsList.Add(cmsRole.ToLower());
                    }
                }
            }
            catch { }
        }

        foreach (string role in userroles)
        {
            if (cmsList.Contains(role.ToLower()) ||
                cmsList.Contains("nt-group::" + role.ToLower()))
            {
                uList.Add(role);
            }
        }

        return uList.ToArray();
    }

    #endregion
}
