using Api.Core.AppCode.Services.Rest;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;

namespace Api.Core.AppCode.Services.Api;

public class ApiToolsService
{
    private readonly ApiConfigurationService _apiConfig;
    private readonly BridgeService _bridge;
    private readonly ICryptoService _crypto;

    public ApiToolsService(ApiConfigurationService config,
                           BridgeService bridge,
                           ICryptoService crypto)
    {
        _apiConfig = config;
        _bridge = bridge;
        _crypto = crypto;
    }

    public T ExecuteToolCommand<ToolType, T>(HttpContext context, string method, NameValueCollection parameters, string username)
    {
        //Console.WriteLine("CoolTool: " + typeof(ToolType));
        //Console.WriteLine("Method: " + method);

        var ui = new CmsDocument.UserIdentification(username, null, null, _apiConfig.InstanceRoles);
        var button = Activator.CreateInstance(typeof(ToolType)) as IApiButton; //new WebMapping.Tools.Api.Portal.Portal();

        //Console.WriteLine("IApiButton created: " + button != null);

        if (button == null)
        {
            return default(T);
        }

        var bridge = _bridge.CreateInstance(ui, button);

        ApiToolEventArguments e = new ApiToolEventArguments(bridge, parameters ?? new NameValueCollection());
        bridge.CurrentEventArguments = e;

        ApiEventResponse apiResponse = null;

        try
        {
            foreach (var methodInfo in button.GetType().GetMethods())
            {
                ServerToolCommandAttribute[] attributes = (ServerToolCommandAttribute[])methodInfo.GetCustomAttributes(typeof(ServerToolCommandAttribute), true);
                if (attributes == null || attributes.Length == 0)
                {
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    if (method == attribute.Method)
                    {
                        //Console.WriteLine("Invoke: " + methodInfo.Name);
                        apiResponse = (ApiEventResponse)methodInfo.Invoke(button, new object[] { bridge, e });
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
                throw ex.InnerException;
            }

            throw;
        }

        if (apiResponse is ApiRawJsonEventResponse)
        {
            var t = (T)((ApiRawJsonEventResponse)apiResponse).RawJsonObject;
            return t.DecryptSecureProperties<T>(_crypto);
        }

        if (apiResponse is ApiRawStringEventResponse)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(((ApiRawStringEventResponse)apiResponse).RawString, typeof(T));
            }

            return JSerializer.Deserialize<T>(((ApiRawStringEventResponse)apiResponse).RawString)
                                              .DecryptSecureProperties<T>(_crypto);
        }

        return default(T);
    }
}
