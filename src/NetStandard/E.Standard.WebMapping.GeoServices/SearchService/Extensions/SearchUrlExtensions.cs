using E.Standard.CMS.Core;

namespace E.Standard.WebMapping.GeoServices.SearchService.Extensions;

internal static class SearchUrlExtensions
{
    extension(string url)
    {
        public string ReplaceSolrTermPlaceholder(string term)
        {
            if (url.Contains("{0}"))
            {
                url = url.Replace("{0}", term);  // do not use String.Format... it changes {{roles}} to {roles} ? wtf!!!
            }

            if (url.Contains("{term}"))
            {
                url = url.Replace("{term}", term);
            }

            if (url.Contains("[term]"))  // Lagacy
            {
                url = url.Replace("[term]", term);
            }

            return url;
        }

        public string ReplaceRolesPlaceholder(CmsDocument.UserIdentification ui)
        {
            if (url.Contains("{{roles}}"))
            {
                url = url.Replace("{{roles}}", ui.UserRolesAsString(
                    separator: " ",
                    withRoleNamespace: false));
            }
            if (url.Contains("{{namespace-roles}}"))
            {
                url = url.Replace("{{namespace-roles}}", ui.UserRolesAsString(
                    separator: " ",
                    openingBrace: "\"",
                    closingBrace: "\"",
                    withRoleNamespace: true));
            }

            return url;
        }
    }
}
