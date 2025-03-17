using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Authentication;
using E.DataLinq.Web.Reflection;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class HmacAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public HmacAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next; ;
    }

    async public Task Invoke(HttpContext httpContext,
                             RoutingEndPointReflectionService endpointReflection,
                             HmacAuthenticationService hmacAuth)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, ApiAuthenticationTypes.Hmac) ||
            httpContext.User.ApplyDataLinqHostAuthentication(endpointReflection, HostAuthenticationTypes.DataLinqEngine))
        {
            try
            {
                //var stopWath = new StopWatch("hmac_cost");

                var ui = hmacAuth.GetHmacUser(httpContext);

                //int ms = stopWath.Stop();
                //using (var writer = System.IO.File.AppendText(@"C:\temp\hmac_cost.txt"))
                //{
                //    writer.WriteLine($"Hmac_cost { ms }ms");
                //}

                if (ui != null)
                {
                    if (ui.IsAnonymous == false || ui.HasCmsRoles())  // Anonymous user kann in Cloud CMS Rollen haben => übernehmen
                    {
                        if (httpContext.User.ApplyDataLinqHostAuthentication(endpointReflection, HostAuthenticationTypes.DataLinqEngine))
                        {
                            var dataLinqCodeRoles = httpContext.User
                                .GetRoles()?
                                .ToArray()
                                .Where(r => r == "datalinq-code" || r.StartsWith("datalinq-code("))
                                .ToArray();
                            CmsDocument.UserIdentification.ResetUserroles(ui, ui.Userroles.ConcatRoles(dataLinqCodeRoles));
                        }

                        httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.Hmac);
                    }
                }

            }
            catch (NotAuthorizedException nae)
            {
                httpContext.User = CmsDocument.UserIdentification.Anonymous.ToClaimsPrincipal(ApiAuthenticationTypes.Hmac, exceptionMessage: nae.Message);
            }
        }

        await _next(httpContext);
    }
}
