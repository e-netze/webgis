using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using Api.Core.Models.Home;
using E.Standard.Api.App.Reflection;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Web.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers;

[ApiAuthentication(ApiAuthenticationTypes.Cookie)]
public class HomeController : ApiBaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly ViewDataHelperService _viewDataHelper;
    private readonly CacheService _cache;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;

    public HomeController(ILogger<HomeController> logger,
                          ViewDataHelperService viewDataHelper,
                          CacheService cache,
                          UrlHelperService urlHelper,
                          IHttpService http,
                          IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                          IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _viewDataHelper = viewDataHelper;
        _cache = cache;
        _expectableUserRolesNamesProviders = expectableUserRolesNamesProviders;
    }

    public IActionResult Index()
    {
        _cache.Init(_expectableUserRolesNamesProviders);

        //
        // Tesen, ob ein Anwender angemedet ist
        // Damit in der UI dann Abmelden/Anmelden Buttons richtig angeigt werden...
        //
        //SubscribersControllerImplementation<ResultType>.DetermineCurrentAuthSubscriber(Base, Request, false, "");

        _viewDataHelper.AddUsernameViewData(this);

        return ViewResult();
    }

    public IActionResult Error()
    {
        string message = "Unknown error", stackTrace = null;

        var exceptionHandler = this.HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandler?.Error != null)
        {
            message = exceptionHandler.Error.Message;

            if (exceptionHandler.Error is NullReferenceException)
            {
                stackTrace = exceptionHandler.Error.StackTrace;
            }
        }

        if (new string[] { "json", "pjson" }.Contains(Request.FormOrQuery("f")))
        {
            return Json(new { success = false, error = message, stacktrace = stackTrace });
        }

        return View(new ErrorModel()
        {
            Message = message,
            StackTrace = stackTrace
        });
    }
}
