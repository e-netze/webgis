using E.Standard.WebGIS.Api.Abstractions;

namespace Api.Core.AppCode.Extensions;

static internal class UrlHelperExtensions
{
    extension(IUrlHelperService urlHelper)
    {
        public string DataLinqReportUrl => $"{urlHelper.AppRootUrl()}/datalinq/report/";
        public bool IsLocalDataLinqReportUrl(string url)
            => url.StartsWith(urlHelper.DataLinqReportUrl, System.StringComparison.OrdinalIgnoreCase);

        public (string dataLinqUrl, string dataLinqRoute, string payload) ToDataLinqUrlParts(string url)
        {
            var urlHasParameters = url.IndexOf("?") > 0;

            string datalinqUrl = urlHasParameters
                    ? url.Substring(0, url.IndexOf("?"))
                    : url,
                route = datalinqUrl.Substring(datalinqUrl.LastIndexOf("/") + 1),
                payloay = urlHasParameters
                    ? url.Substring(url.IndexOf("?") + 1)
                    : "";

            return (datalinqUrl,  route, payloay);
        }
    }
}
