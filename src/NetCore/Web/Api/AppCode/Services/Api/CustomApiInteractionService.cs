using E.Standard.Custom.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace Api.Core.AppCode.Services.Api;

public class CustomApiInteractionService : ICustomApiInteractionService
{
    private readonly ApiToolsService _apiTools;

    public CustomApiInteractionService(ApiToolsService apiTools)
    {
        _apiTools = apiTools;
    }

    public T ExecuteToolCommand<ToolType, T>(HttpContext context, string method, NameValueCollection parameters, string username)
    {
        return _apiTools.ExecuteToolCommand<ToolType, T>(context, method, parameters, username);
    }
}
