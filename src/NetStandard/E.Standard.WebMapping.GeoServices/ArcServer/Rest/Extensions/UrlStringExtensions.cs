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
    }
}
