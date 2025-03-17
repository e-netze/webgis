using Cms.AppCode.Services;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Security.App;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cms.AppCode.Mvc;

public class ApplicationSecurityController : Controller
{
    private readonly CmsConfigurationService _cmsConfigurationService;
    private readonly ApplicationSecurityUserManager _applicationSecurityUserManager;
    private readonly ApplicationSecurity _applicationSecurity;
    private readonly IEnumerable<ICustomCmsPageSecurityService> _customSecurity;
    private readonly ICryptoService _crypto;
    private readonly UrlHelperService _urlHelperService;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ApplicationSecurityController(
        CmsConfigurationService cmsConfigurationService,
        UrlHelperService urlHelperService,
        ApplicationSecurityUserManager applicationSecurityUserManager,
        IEnumerable<ICustomCmsPageSecurityService> customSecurity,
        ICryptoService crypto,
        CmsItemInjectionPackService instanceService)
    {
        _cmsConfigurationService = cmsConfigurationService;
        _urlHelperService = urlHelperService;
        _applicationSecurityUserManager = applicationSecurityUserManager;
        _applicationSecurity = new ApplicationSecurity(
                                            _applicationSecurityUserManager,
                                            new DefaultApplicationSecurityProvider(_applicationSecurityUserManager, this));
        _customSecurity = customSecurity;
        _crypto = crypto;

        _servicePack = instanceService.ServicePack;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            var controllerActionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

            string cmsId =
                context.ActionArguments.ContainsKey("cmsId") ?
                        context.ActionArguments["cmsId"]?.ToString() :
                        String.Empty;

            if (String.IsNullOrEmpty(cmsId))
            {
                cmsId = context.ActionArguments.ContainsKey("id") ?
                        context.ActionArguments["id"]?.ToString() :
                        String.Empty;
            }

            if (_cmsConfigurationService.IsCustomCms(cmsId))
            {
                CheckCustomCmsSecurity(cmsId);
            }
            else
            {
                ViewData["Username"] = _applicationSecurity.CheckSecurity(this, controllerActionDescriptor?.MethodInfo, this.User);
            }

            ViewData["CanLogout"] = _applicationSecurity.CanLogout;

            base.OnActionExecuting(context);
            ViewData["AppRootUrl"] = _urlHelperService.AppRootUrl(this, _cmsConfigurationService.Instance != null ? _cmsConfigurationService.Instance.ForceHttps : false);
        }
        catch (E.Standard.Security.Cryptography.Exceptions.CryptographyException)
        {
            // logout... Cookie my be invalid

            context.Result = RedirectToAction("Logout", "Login");
        }
        catch (Exception ex)
        {
            context.Result = ExceptionResult(ex);
        }
    }

    public IActionResult OpenConsole(BackgroundProcess backgroundProcess, string title, string cmsId)
    {
        return Json(backgroundProcess.ProcDefinition(title));
    }

    public IActionResult ExceptionResult(Exception ex)
    {
        if (ex is NotAuthorizedException)
        {
            switch (_applicationSecurityUserManager?.ApplicationSecurity?.IdentityType)
            {
                case ApplicationSecurityIdentityTypes.OpenIdConnection:
                case ApplicationSecurityIdentityTypes.AzureAD:
                    return RedirectToAction("Index", "Authenticate");
                case ApplicationSecurityIdentityTypes.Windows:
                    return RedirectToAction("Forbidden", "Authenticate");
                default:
                    return RedirectToAction("Index", "Login");
            }
        }

        return Json(new
        {
            success = false,
            exception = ex.FullMessage(),
            stacktrace = /*ex is NullReferenceException ||*/ true ? ex.StackTrace : null
        });
    }

    #region AuthCookies

    public void SetAuthCookie(string userName, bool persistentCookie)
    {
        Response.Cookies.Append(AuthCookieName, _crypto.EncryptCookieValue(userName), new Microsoft.AspNetCore.Http.CookieOptions()
        {
            //Expires=DateTimeOffset.
        });
        Response.Headers.Append("P3P", "CP='IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT'");
    }

    new public void SignOut()
    {
        Response.Cookies.Append(AuthCookieName, String.Empty);
    }

    public string GetCookieUsername()
    {
        return _crypto.DecryptCookieValue(Request.Cookies[AuthCookieName]) ?? String.Empty;
    }

    public string GetCurrentUsername()
    {
        try
        {
            var userName = this.GetCookieUsername()?.Split(',')[0] ?? String.Empty;

            if (String.IsNullOrEmpty(userName))
            {
                userName = _applicationSecurity.CurrentLoginUser(this.User);
            }

            return userName;
        }
        catch { return String.Empty; }
    }

    private const string AuthCookieName = "e-cms-auth";

    private class CookieData
    {
        public CookieData()
        {
            this.Created = DateTime.UtcNow.Ticks;
            this.Expires = DateTime.UtcNow.AddDays(1).Ticks;
        }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "authtype")]
        [System.Text.Json.Serialization.JsonPropertyName("authtype")]
        public int AuthType { get; set; }

        [JsonProperty(PropertyName = "created")]
        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonProperty(PropertyName = "expires")]
        [System.Text.Json.Serialization.JsonPropertyName("expires")]
        public long Expires { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpired
        {
            get { return DateTime.UtcNow > new DateTime(Expires, DateTimeKind.Utc); }
        }
    }

    #endregion

    #region Custom Security

    private void CheckCustomCmsSecurity(string cmsId)
    {
        #region Custom CMS

        string username = GetCookieUsername();
        if (username.Split(',').Contains(cmsId))
        {
            username = username.Split(',')[0];
        }
        else
        {
            var user = _customSecurity.CheckSecurity(this.HttpContext, cmsId);

            if (user == null)
            {
                throw new NotAuthorizedException();
            }

            string cookieString = user.Username + "," + user.UserId + "," + String.Join(',', user.Roles);
            SetAuthCookie(cookieString, false);
        }

        _cmsConfigurationService.InitCustomCms(_servicePack, cmsId);

        ViewData["Username"] = username;

        #endregion
    }

    public string GetCloudUserIdFromCookie(string cmsId)
    {
        if (_cmsConfigurationService.IsCustomCms(cmsId))
        {
            string[] username = GetCookieUsername().Split(',');
            if (username.Length >= 2 && username.Contains(cmsId))
            {
                return username[1];
            }
        }

        return String.Empty;
    }

    #endregion
}
