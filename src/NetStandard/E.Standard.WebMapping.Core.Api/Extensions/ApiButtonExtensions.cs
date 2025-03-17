using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Reflection;

namespace E.Standard.WebMapping.Core.Api.Extensions;
static public class ApiButtonExtensions
{
    public static T CheckToolPolicy<T>(this T button, CmsDocument.UserIdentification ui)
        where T : IApiButton
    {
        var toolPolicy = button?.GetType().GetCustomAttribute<ToolPolicyAttribute>();

        if (toolPolicy is null)
        {
            return button;  // no policy
        }

        if (toolPolicy.RequireAuthentication && ui.IsAnonymous)
        {
            throw new Exception("Sorry, tool is not allowed for unauthenticated users (anonymous users)");
        }

        return button;
    }
}
