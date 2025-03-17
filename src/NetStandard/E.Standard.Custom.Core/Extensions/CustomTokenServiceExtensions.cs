using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Custom.Core.Extensions;

static public class CustomTokenServiceExtensions
{
    static public CustomToken GetCustomToken(this IEnumerable<ICustomTokenService> tokenServices, HttpRequest request)
    {
        return tokenServices?.Select(t => t.GetCustomToken(request))
                             .Where(t => t != null)
                             .FirstOrDefault();
    }
}
