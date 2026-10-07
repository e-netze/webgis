using System;

using Cms.Controllers;

using E.Standard.Cms.Configuration.Services;
using E.Standard.CMS.Core.Extensions;
using E.Standard.Localization.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Cms.AppCode.Mvc;

/// <summary>
/// Rejects modifying CMS actions while a git merge is running in the user's working copy
/// (the conflicts have to be resolved or the merge aborted first).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CmsGitEditLockAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is not ApplicationSecurityController controller)
        {
            return;
        }

        var cmsId = context.ActionArguments.TryGetValue("id", out var id) && id is string idString && !String.IsNullOrEmpty(idString)
            ? idString
            : context.RouteData.Values["id"]?.ToString();

        if (String.IsNullOrEmpty(cmsId))
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var resolver = services.GetRequiredService<CmsManagerResolver>();

        if (resolver.IsMerging(cmsId, controller.GetCurrentUsername()))
        {
            var localizer = services.GetRequiredService<IStringLocalizerFactory>().CreateCmsLocalizer(typeof(GitController));

            context.Result = new JsonResult(new
            {
                success = false,
                error_key = "error-merge-in-progress",
                exception = localizer.Localize("error-merge-in-progress")
            });
        }
    }
}
