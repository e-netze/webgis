using System.Collections.Specialized;

using Microsoft.AspNetCore.Http;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiInteractionService
{
    T ExecuteToolCommand<ToolType, T>(HttpContext context, string method, NameValueCollection parameters, string username);
}
