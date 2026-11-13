using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

internal static class UrlStringExtensions
{
    extension(string requestUrl)
    {
        public string AddAgsToken(string token)
           => String.IsNullOrWhiteSpace(token)
                    ? requestUrl
                    : $"{requestUrl}{(requestUrl.Contains("?") ? "&" : "?")}token={token}";

        // Without an f parameter, newer AGS versions (12.1+) redirect unauthenticated requests
        // to an HTML login page instead of returning a JSON error (499).
        public string AddAgsFormatJsonIfMissing(string postBodyData = null)
        {
            int queryStart = requestUrl.IndexOf('?');

            if ((queryStart >= 0 && HasFormatParameter(requestUrl, queryStart + 1))
                || HasFormatParameter(postBodyData, 0))
            {
                return requestUrl;
            }

            return $"{requestUrl}{(queryStart >= 0 ? "&" : "?")}f=json";
        }
    }

    // allocation free: called for every AGS request
    private static bool HasFormatParameter(string parameters, int start)
    {
        if (parameters is null)
        {
            return false;
        }

        for (int i = start; i < parameters.Length - 1; i++)
        {
            if ((parameters[i] == 'f' || parameters[i] == 'F')
                && parameters[i + 1] == '='
                && (i == start || parameters[i - 1] == '&'))
            {
                return true;
            }
        }

        return false;
    }
}