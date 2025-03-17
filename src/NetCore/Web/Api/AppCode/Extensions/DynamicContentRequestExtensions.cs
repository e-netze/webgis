using E.Standard.CMS.Core;
using E.Standard.Platform;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Http;
using System;

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

    static public string DynamicContentUrl(this HttpRequest request, CmsDocument.UserIdentification ui)
    {
        string url = request.Form["url"].ToString().ReplacePlaceholders(ui);

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
