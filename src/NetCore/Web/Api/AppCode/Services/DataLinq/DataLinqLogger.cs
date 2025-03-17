using E.DataLinq.Web.Services.Abstraction;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqLogger : IDataLinqLogger
{
    private readonly IDatalinqPerformanceLogger _logger;

    public DataLinqLogger(IDatalinqPerformanceLogger logger)
    {
        _logger = logger;
    }

    public IDisposable CreatePerformanceLogger(HttpContext httpContext,
                                               string method,
                                               string dataLinqRoute,
                                               string dataLinqUsername)
    {
        return _logger.Start(CmsDocument.UserIdentification.Create(dataLinqUsername),
                               String.Empty,
                               "api5_datalinq",
                               method,
                               $"{dataLinqRoute}|{LogQueryString(httpContext)}");
    }

    #region Helper

    private string LogQueryString(HttpContext httpContext)
    {
        StringBuilder sb = new StringBuilder();

        try
        {
            foreach (string key in httpContext.Request.Query.Keys)
            {
                if (key == null || key == "_f" || key == "_id" || key == "hmac" || key.StartsWith("hmac_") || String.IsNullOrEmpty(httpContext.Request.Query[key]))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                sb.Append(key + "=" + httpContext.Request.Query[key]);
            }
        }
        catch { }

        return sb.ToString();
    }

    #endregion

    #region Classes

    #endregion
}
