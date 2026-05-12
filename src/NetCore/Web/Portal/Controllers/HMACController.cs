using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebApp.Options;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Portal.Core.AppCode;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Reflection;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;

namespace E.Standard.WebGIS.Core.Reflection.Authentication;

[EnableCors("hmac")]
public class HMACController : PortalBaseController
{
    private readonly ILogger<HMACController> _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly HmacService _hmacService;
    private readonly IEnumerable<IPortalAuthenticationService> _authenticationServices;
    private readonly ConfigurationService _configService;
    private readonly ICryptoService _crypto;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly JwtAccessTokenService _tokenService;
    private readonly SecurityOptions _securityOptions;

    public HMACController(ILogger<HMACController> logger,
                          UrlHelperService urlHelper,
                          HmacService hmacService,
                          IEnumerable<IPortalAuthenticationService> authenticationServices,
                          ConfigurationService configService,
                          ICryptoService crypto,
                          IOptions<ApplicationSecurityConfig> appSecurityConfig,
                          JwtAccessTokenService tokenService,
                          IOptions<SecurityOptions> securityOptions,
                          IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _urlHelper = urlHelper;
        _hmacService = hmacService;
        _authenticationServices = authenticationServices;
        _configService = configService;
        _crypto = crypto;
        _appSecurityConfig = appSecurityConfig.Value;
        _tokenService = tokenService;
        _securityOptions = securityOptions.Value;
    }

    [AuthorizeEndpoint]
    async public Task<IActionResult> Index(string redirect = null, string token = null)
    {
        PortalUser portalUser = null;

        if (!this.User.StopAuthenticationPropagation())
        {
            if (User.Identity.IsAuthenticated && User.Identity is WindowsIdentity)
            {
                var windowsUser = await _authenticationServices.GetService(UserType.WindowsUser)
                                                               .TryAuthenticationServiceUser(this.HttpContext, this.User.Identity.Name, true);
                if (windowsUser != null)
                {
                    portalUser = new PortalUser(windowsUser.Username, windowsUser.UserRoles, null);
                }
            }
        }

        portalUser = portalUser ?? this.User.ToPortalUser(_appSecurityConfig);

        var hmacObject = await _hmacService.CreateHmacObjectAsync(portalUser);

        if (!string.IsNullOrEmpty(redirect)
            && (_appSecurityConfig.UseOpenIdConnect() || _appSecurityConfig.UseAzureAD()))
        {
            // eg. to authenticate datalinq pages
            // => datalinq.js will call this url, if no username is set
            hmacObject.authEndpoint = $"{_urlHelper.AppRootUrl(this.Request, this).RemoveEndingSlashes()}/auth/LoginOidc?webgis-redirect={_crypto.EncryptTextDefault(redirect, Security.Cryptography.CryptoResultStringType.Hex)}";
        }

        #region Add Roles when correct password is given

        if(!string.IsNullOrEmpty(token)
            && _securityOptions.EndpointAuthorizationBearerUsername.Equals(_tokenService.ValidateToken(token)?.Identity?.Name))
        {
            hmacObject.userroles = portalUser.UserRoles;
            hmacObject.userroleparameteres = portalUser.RoleParameters;
        }

        #endregion

        return JsonObject(hmacObject);
    }
}
