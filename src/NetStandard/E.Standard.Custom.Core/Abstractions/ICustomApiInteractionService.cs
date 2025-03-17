using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiInteractionService
{
    T ExecuteToolCommand<ToolType, T>(HttpContext context, string method, NameValueCollection parameters, string username);
}
