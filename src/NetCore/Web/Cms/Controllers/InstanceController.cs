using E.Standard.Cms.Configuration;
using E.Standard.Security.Cryptography.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cms.Controllers;

public class InstanceController : Controller
{
    private readonly CryptoServiceOptions _cryptoServiceOptions;

    public InstanceController(IOptionsMonitor<CryptoServiceOptions> cryptoServiceOptions)
    {
        _cryptoServiceOptions = cryptoServiceOptions.CurrentValue;
    }

    public IActionResult Info()
    {
        return Json(new
        {
            version = CmsGlobals.Version.ToString()
            //cc_hash = _cryptoServiceOptions.GenerateHashCode()
        });
    }
}
