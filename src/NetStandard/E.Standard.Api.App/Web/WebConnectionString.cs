using System;

namespace E.Standard.Api.App.Web;

public class WebConnectionString
{
    public WebConnectionString(string connectionString)
    {
        if (connectionString.ToLower().StartsWith("http://") || connectionString.ToLower().StartsWith("https://"))
        {
            this.Service = connectionString;
        }
        else
        {
            this.Service = ExtractParameter("service", connectionString);
            this.User = ExtractParameter("user", connectionString);
            this.Password = ExtractParameter("pwd", connectionString);

            this.UseProxyServer = ExtractParameter("use_proxy", connectionString)?.ToLower() == "true";

            if (!String.IsNullOrEmpty(ExtractParameter("cache_expires", connectionString)))
            {
                if (int.TryParse(ExtractParameter("cache_expires", connectionString), out int cache_expires))
                {
                    this.CacheExpires = cache_expires;
                }
            }
        }
    }

    public string Service { get; set; }
    public string User { get; set; }
    public string Password { get; set; }

    public bool UseProxyServer { get; set; }

    public int CacheExpires { get; set; }

    #region Helper

    private string ExtractParameter(string parameter, string connectionString)
    {
        foreach (var p in connectionString.Split(';'))
        {
            if (p.ToLower().StartsWith(parameter.ToLower() + "="))
            {
                return p.Substring(parameter.Length + 1).Trim();
            }
        }

        return String.Empty;
    }

    #endregion
}
