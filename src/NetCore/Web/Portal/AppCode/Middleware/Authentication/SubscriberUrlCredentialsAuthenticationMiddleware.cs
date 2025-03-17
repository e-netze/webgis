// DEPRECATED
// **********

//using E.Standard.Security.App.Exceptions;
//using E.Standard.Security.Cryptography.Abstractions;
//using E.Standard.WebGIS.Core;
//using E.Standard.WebGIS.Core.Services;
//using Microsoft.AspNetCore.Http;
//using Portal.Core.AppCode.Extensions;
//using Portal.Core.AppCode.Services.Authentication;
//using System;
//using System.Threading.Tasks;

//namespace Portal.Core.AppCode.Middleware.Authentication
//{
//    public class SubscriberUrlCredentialsAuthenticationMiddleware
//    {
//        private readonly RequestDelegate _next;


//        public SubscriberUrlCredentialsAuthenticationMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task Invoke(HttpContext context,
//                                 WebgisCookieService webgisCookieService,
//                                 ITracerService tracer,
//                                 ICryptoService crypto)
//        {
//            if (context.User.ApplyAuthenticationMiddleware())
//            {
//                if (!String.IsNullOrWhiteSpace(context.Request.Query["credentials"]))
//                {
//                    string[] credentials = crypto.DecryptTextDefault(context.Request.Query["credentials"]).Split('|');
//                    if (credentials.Length != 3 ||
//                        !HasPortalInPath(context.Request.Path, credentials[0]) ||
//                        (new DateTime(DateTime.UtcNow.Ticks) - new DateTime(long.Parse(credentials[2]))).TotalSeconds > 3D)
//                    {
//                        throw new NotAuthorizedException();
//                    }

//                    string username = credentials[1];

//                    if (webgisCookieService.SetAuthCookieFor(context))
//                    {
//                        webgisCookieService.SetAuthCookie(context, false, username, UserType.ApiSubscriber);
//                    }

//                    context.User = new PortalUser(username, null).ToClaimsPricipal();

//                    tracer.TracePortalUser(this, context);
//                }
//            }

//            await _next.Invoke(context);
//        }

//        private bool HasPortalInPath(string path, string portalId)
//        {
//            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(portalId))
//            {
//                return false;
//            }

//            return
//                (path.Contains($"/{portalId}/", StringComparison.CurrentCultureIgnoreCase)) ||
//                (path.EndsWith($"/{portalId}", StringComparison.CurrentCultureIgnoreCase));
//        }
//    }
//}
