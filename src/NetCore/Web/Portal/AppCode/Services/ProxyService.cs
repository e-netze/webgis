using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Custom.Core.Models;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Core;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services;

public class ProxyService
{
    private readonly ConfigurationService _config;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly UrlHelperService _urlHelper;
    private readonly WebgisApiService _api;
    private readonly IEnumerable<ICustomPortalSecurityService> _customSecurity;

    public ProxyService(ConfigurationService config,
                        SubscriberDatabaseService subscriberDb,
                        UrlHelperService urlHelper,
                        WebgisApiService api,
                        IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                        IEnumerable<ICustomPortalSecurityService> customSecurity = null)
    {
        _config = config;
        _subscriberDb = subscriberDb;
        _urlHelper = urlHelper;
        _api = api;
        _appSecurityConfig = appSecurityConfig.CurrentValue;
        _customSecurity = customSecurity;
    }

    public CustomSecurityPrefix[] SecurityPrefixes(string id)
    {
        var allowedMethods = _config.AllowedSecurityMethods();

        List<CustomSecurityPrefix> prefixes = new List<CustomSecurityPrefix>();

        #region Add Subscriber

        if (!_customSecurity.DisallowSubscriberUser(allowedMethods))
        {
            prefixes.Add(new CustomSecurityPrefix("subscriber::", SecurityTypes.User));
        }

        #endregion

        #region Add OpenIdConnect/AzureAD

        if (_appSecurityConfig.UseAnyOidcMethod())
        {
            prefixes.Add(new CustomSecurityPrefix($"{_appSecurityConfig.OidcAuthenticationUserPrefix()}::", SecurityTypes.User));
            prefixes.Add(new CustomSecurityPrefix($"{_appSecurityConfig.OidcAuthenticationRolePrefix()}::", SecurityTypes.Group));
        }

        #endregion

        #region Add Header based 

        if (_config.UseHeaderAuthentication())
        {
            if (!String.IsNullOrEmpty(_config.HeaderAuthenticationUserPrefix()))
            {
                prefixes.Add(new CustomSecurityPrefix($"{_config.HeaderAuthenticationUserPrefix()}::", SecurityTypes.User));
            }
            if (!String.IsNullOrEmpty(_config.HeaderAuthenticationRolePrefix()))
            {
                prefixes.Add(new CustomSecurityPrefix($"{_config.HeaderAuthenticationRolePrefix()}::", SecurityTypes.Group));
            }
        }

        #endregion

        #region Add Custom (Windows...)

        _customSecurity.AddCustomSecurityPrefixes(prefixes, allowedMethods);

        #endregion

        #region Add Instance

        if (!_customSecurity.DisallowInstanceGroup(allowedMethods))
        {
            prefixes.Add(new CustomSecurityPrefix("instance::", SecurityTypes.Group));
        }

        #endregion

        return prefixes.ToArray();
    }

    async public Task<string[]> SecurityAutocomplete(HttpRequest httpRequest, string term, string prefix, string cmsId = "", string subscriberId = "")
    {
        List<string> ret = new List<string>();

        var secInfo = await _api.SecurityInfo(httpRequest);

        if (String.IsNullOrEmpty(prefix) || prefix == "subscriber::")
        {
            try
            {
                var db = _subscriberDb.CreateInstance();
                if (db != null)
                {
                    foreach (var subscriberName in db.SubscriberNames(term + "%"))
                    {
                        ret.Add(!String.IsNullOrEmpty(prefix) ?
                            subscriberName :
                            UserManagement.AppendUserPrefix(subscriberName, UserType.ApiSubscriber));
                    }
                }
            }
            catch (Exception ex)
            {
                ret.Add("subscriber-exception::" + ex.Message);
            }
        }

        if (String.IsNullOrEmpty(prefix) || prefix == "instance::")
        {
            if (secInfo?.InstanceRoles != null)
            {
                ret.AddRange(secInfo.InstanceRoles.Where(r => r.StartsWith(term, StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        ret.AddRange(await _customSecurity.AutoCompleteValues(term, prefix, cmsId, subscriberId));

        return ret.ToArray();
    }
}
