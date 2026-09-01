using System;
using System.Linq;

using E.Standard.CMS.Core;
using E.Standard.WebGIS.Core.Extensions;

namespace E.Standard.WebMapping.GeoServices.SearchService.Extensions;

internal static class UserIdentificationExtensions
{
    extension(CmsDocument.UserIdentification ui)
    {
        public string UserRolesAsString(
            string separator = " ",
            string openingBrace = "",
            string closingBrace = "",
            bool withRoleNamespace = false)
        {
            if (ui?.Userroles?.Any() != true)
            {
                return String.Empty;
            }

            return String.Join(separator,
                   ui.Userroles
                       .Select(r =>
                           withRoleNamespace
                                ? r
                                : r.RemoveAuthPrefix()
                       )
                       .Select(r => $"{openingBrace}{r}{closingBrace}")
            );
        }
    }
}
