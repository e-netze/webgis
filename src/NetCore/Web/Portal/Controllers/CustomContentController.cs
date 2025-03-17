using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class CustomContentController : PortalBaseController
{
    private readonly ILogger<CustomContentController> _logger;
    private readonly CustomContentService _customContent;
    private readonly WebgisApiService _api;

    public CustomContentController(ILogger<CustomContentController> logger,
                                   CustomContentService customContent,
                                   UrlHelperService urlHelper,
                                   ICryptoService crypto,
                                   WebgisApiService api,
                                   IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                                   IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _customContent = customContent;
        _api = api;
    }

    async public Task<IActionResult> Load(string t, string c, string f, string id)
    {
        try
        {
            // ToDo:
            // Etag

            var knownContent = new string[] { "custom.js", "portal.css", "default.css" };
            if (!knownContent.Contains(c))
            {
                throw new Exception("Not allowed");
            }

            string path = String.Empty, content = String.Empty;

            if (!String.IsNullOrWhiteSpace(t))
            {
                var identity = _customContent.FromTempPortalToken(t);
                if (identity.pageId != id) // Is token valid for this portal?
                {
                    throw new NotAuthorizedException();
                }

                path = $"{_customContent.PortalCustomContentRootPath}/{identity.pageId}/{c}";
            }
            else if (!String.IsNullOrWhiteSpace(id) && f == "json")
            {
                var portalUser = CurrentPortalUser();
                if (portalUser == null)
                {
                    throw new NotAuthorizedException();
                }

                var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);

                if (!IsPortalOwner(portal, portalUser))
                {
                    throw new NotAuthorizedException();
                }

                path = $"{_customContent.PortalCustomContentRootPath}/{id}/{c}";
            }
            else
            {
                throw new Exception("Invalid request");
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                content = System.IO.File.ReadAllText(fileInfo.FullName);
            }

            if (f == "json")
            {
                return Json(new
                {
                    success = true,
                    content = content
                });
            }

            var contentType = c.EndsWith(".js") ? "application/javascript" : "text/css";
            return PlainView(content, contentType);
        }
        catch (NotAuthorizedException)
        {
            return StatusCode(401);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    async public Task<IActionResult> Save(string id, string c, string content)
    {
        try
        {
            var portalUser = CurrentPortalUser();
            if (portalUser == null)
            {
                throw new NotAuthorizedException();
            }

            var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);

            if (!IsPortalOwner(portal, portalUser))
            {
                throw new NotAuthorizedException();
            }

            var knownContent = new string[] { "custom.js", "portal.css", "default.css" };
            if (!knownContent.Contains(c))
            {
                throw new Exception("Not allowed");
            }

            string path = $"{_customContent.PortalCustomContentRootPath}/{id}/{c}";

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            System.IO.File.WriteAllText(path, content);

            return JsonViewSuccess(true);
        }
        catch (NotAuthorizedException)
        {
            return StatusCode(401);
        }
        catch (Exception ex)
        {
            return JsonViewSuccess(false, ex.Message);
        }
    }
}