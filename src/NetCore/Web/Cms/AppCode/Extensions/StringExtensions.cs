using System;

namespace Cms.AppCode.Extensions;

static public class StringExtensions
{
    static public string CmsIdToElasticSerarchIndexName(this string id)
    {
        if (String.IsNullOrWhiteSpace(id))
        {
            return "webgis-cms";
        }

        return "webgis-cms-" + id.Trim();
    }
}
