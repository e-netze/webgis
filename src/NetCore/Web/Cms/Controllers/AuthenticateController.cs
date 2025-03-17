using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cms.Controllers;


public class AuthenticateController : Controller
{
    private readonly ApplicationSecurityConfig _applicationSecurity;
    public AuthenticateController(IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity)
    {
        _applicationSecurity = applicationSecurity.CurrentValue;
    }

    [Authorize]
    public IActionResult Index()
    {
        switch (_applicationSecurity?.IdentityType)
        {
            case ApplicationSecurityIdentityTypes.OpenIdConnection:
            case ApplicationSecurityIdentityTypes.AzureAD:
                if (_applicationSecurity.ConfirmSecurity(
                        this.User.GetUsername(),
                        this.User.GetRoles(_applicationSecurity)) == false)
                {
                    return RedirectToAction("Forbidden");
                }

                break;
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Forbidden()
    {
        ViewData["Username"] = this.User?.Identity.Name ?? "Unknown";

        return View();
    }
}