using System;

using E.Standard.CMS.Core;
using E.Standard.Api.App.Services;
using E.Standard.Platform;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using Microsoft.AspNetCore.Http;

namespace Api.Core.AppCode.Extensions;

static public class DynamicContentRequestExtensions
{
    static Envelope DynamicContentBbox(this HttpRequest request)
    {
        if (!String.IsNullOrEmpty(request.FormOrQuery("_bbox")))
        {
            var envelope = new Envelope();
            envelope.FromBBox(request.FormOrQuery("_bbox"));

            return envelope;
        }

        return null;
    }

    static public string DynamicContentUrl(this HttpRequest request, CmsDocument.UserIdentification ui, ICryptoService crypto)
    {
        string type = request.DynamicContentType();
        if (type == DynamicContentSignature.TocApiQueryType)
        {
            return request.Form["url"].ToString();
        }

        string url = DynamicContentSignature.Unwrap(crypto, type, request.Form["url"].ToString(), out bool signed);

        if (DynamicContentSignature.RequiresSignature(type) && !signed && !DynamicContentSettings.AllowUnsigned)
        {
            throw new UnauthorizedAccessException("Dynamic content url is not signed. Reload the map.");
        }

        url = url.ReplacePlaceholders(ui);

        var bbox = request.DynamicContentBbox();
        if (bbox != null)
        {
            url = url
                    .Replace("{lng_min}", bbox.MinX.ToPlatformNumberString())
                    .Replace("{lat_min}", bbox.MinY.ToPlatformNumberString())
                    .Replace("{lng_max}", bbox.MaxX.ToPlatformNumberString())
                    .Replace("{lat_max}", bbox.MaxY.ToPlatformNumberString());
        }

        return url;
    }

    static public string DynamicContentType(this HttpRequest request)
    {
        return request.Form["type"];
    }
}
