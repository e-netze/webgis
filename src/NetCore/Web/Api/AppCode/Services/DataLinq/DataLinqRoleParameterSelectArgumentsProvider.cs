using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqRoleParameterSelectArgumentsProvider : IDataLinqCustomSelectArgumentsProvider
{
    private const string Prefix = "role-parameter:";

    private readonly CmsDocument.UserIdentification _ui;

    public DataLinqRoleParameterSelectArgumentsProvider(IHttpContextAccessor httpContext)
    {
        _ui = httpContext.HttpContext?.User.ToUserIdentification(ApiAuthenticationTypes.Any, throwExceptions: false);
    }

    #region IDataLinqCustomSelectArgumentsProvider

    public bool OverrideExisting => true;

    public string ExclusivePrefix => Prefix;

    public NameValueCollection CustomArguments()
    {
        var result = new NameValueCollection();

        if (_ui?.UserrolesParameters != null)
        {
            foreach (var roleParameter in _ui.UserrolesParameters)
            {
                try
                {
                    int pos = roleParameter.IndexOf("=");
                    if (pos <= 0)
                    {
                        continue;
                    }

                    string key = roleParameter.Substring(0, pos).Trim();
                    string val = roleParameter.Substring(pos + 1).Trim();

                    result[$"{Prefix}{key}"] = val;
                }
                catch { }
            }
        }

        return result;
    }

    #endregion
}
