using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using Microsoft.AspNetCore.Mvc;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services;
using System.IO;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class CompanyController : Controller
{
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;

    public CompanyController(ConfigurationService config,
                             UrlHelperService urlHelper)
    {
        _config = config;
        _urlHelper = urlHelper;
    }

    async public Task<IActionResult> Image(string id, string company = null)
    {
        string fileName = $"{id}.png";
        company = company.OrTake(_config.Company());

        var companiesFolder = new DirectoryInfo(Path.Combine(_urlHelper.AppRootPath(), "wwwroot", "content", "companies"));
        var file = new FileInfo(Path.Combine(companiesFolder.FullName, company, fileName));
        if (!file.Exists)
        {
            file = new FileInfo(Path.Combine(companiesFolder.FullName, "__default", fileName));
        }

        if (!file.Exists)
        {
            return File(new byte[0], "image/png");
        }

        return File(await System.IO.File.ReadAllBytesAsync(file.FullName), "image/png");
    }
}
