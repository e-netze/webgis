#nullable enable

using System;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Api;

using Microsoft.Extensions.Localization;

namespace Api.Core.AppCode.Exceptions;

internal static class ApiToolEventArgumentsExtensions
{
    extension(ApiToolEventArguments e)
    {
        public ApiToolEventArguments EnsureAll(CmsDocument.UserIdentification? ui, IStringLocalizer stringLocalizer)
            => e.EnsureAllowAnonyousAccess(ui, stringLocalizer);

        public ApiToolEventArguments EnsureAllowAnonyousAccess(CmsDocument.UserIdentification? ui, IStringLocalizer stringLocalizer)
        {
            if (ui is not null && !ui.IsAnonymous)
            {
                return e;
            }

            if (e.GetConfigBool("allow-anoymous-access", true) == false)
            {
                throw new Exception(stringLocalizer.GetString("security.tool-anonyous-access-not-allowed"));
            }

            return e;
        }
    }
}
