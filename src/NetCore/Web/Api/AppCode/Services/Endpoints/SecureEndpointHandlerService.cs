#nullable enable

using E.Standard.Api.App.Exceptions;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.WebMapping.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Endpoints;

public class SecureEndpointHandlerService
{
    private ILogger<SecureEndpointHandlerService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecureEndpointHandlerService(
            ILogger<SecureEndpointHandlerService> logger,
            IHttpContextAccessor httpContextAccessor
        )
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    async public Task<object> HandlerAsync(
            Func<CmsDocument.UserIdentification, Task<object>> func,
            ApiAuthenticationTypes authTypes = ApiAuthenticationTypes.Hmac
        )
    {
        CmsDocument.UserIdentification? ui = null;

        try
        {
            ui = _httpContextAccessor.HttpContext!.User.ToUserIdentification(authTypes, throwExceptions: true);

            AddNoCacheHeaders();
            AddApiCorsHeaders();

            return await func(ui);
        }
        catch (AuthenticationException)
        {
            return HandleAuthenticationException();
        }
        catch (ReportExceptionException ree)
        {
            _logger.LogError($"{ree.Message} User: {ui?.Username} ({String.Join(", ", ui?.Userroles ?? [])})");

            //_apiLogging.LogReportException(ree, ui);
            //_mapServiceInitializer.LogException(_requestContext, ree, $"{CurrentControllerName}.{CurrentActionName}");

            return ThrowJsonException(ree);
            throw;  // TODO
        }
        catch (ReportWarningException rwe)
        {
            _logger.LogWarning($"{rwe.Message} User: {ui?.Username} ({String.Join(", ", ui?.Userroles ?? [])})");

            //_apiLogging.LogReportException(rwe, ui);
            return ThrowJsonException(rwe, logLevel: LogLevel.Warning);
            throw;  // TODO

        }
        catch (InfoException iex)
        {
            return ThrowJsonException(iex, logLevel: LogLevel.Information);
        }
        catch (Exception ex)
        {
            if (ex is TargetInvocationException tie)
            {
                ex = tie.InnerException ?? tie;
            }
            _logger.LogError($"{ex.Message} User: {ui?.Username} ({String.Join(", ", ui?.Userroles ?? [])})");

            //_mapServiceInitializer.LogException(_requestContext, ex, $"{CurrentControllerName}.{CurrentActionName}",
            //    service: Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(this.Request));

            return ThrowJsonException(ex);
        }
    }

    #region Helper

    protected object HandleAuthenticationException()
    {
        // TODO
        //if (Request.Method.ToString() == "POST")
        {
            return ThrowJsonException(new Exception("Not authenticated"), 200);
        }

        //var securityConfig = new ApplicationSecurityConfig().LoadFromJsonFile();

        //if (securityConfig?.IdentityType == "oidc")
        //{
        //    return RedirectToAction("Forbidden", "Authenticate");
        //}

        //return RedirectToAction("Login");
    }

    private object ThrowJsonException(Exception ex, int statusCode = 200, LogLevel logLevel = LogLevel.Error)
    {
        _logger.Log(logLevel, ex, "An json exception is thrown");

        string type = ex.GetType().ToString().ToLower();
        type = type.Substring(type.LastIndexOf(".") + 1);

        return JsonViewSuccess(false,
                               $"{ex.SecureMessage()}{(ex is NullReferenceException ? $" {ex.StackTrace}" : String.Empty)}",
                               type,
                               ex is ReportWarningException ? ((ReportWarningException)ex).RequestId : null);
    }

    private object JsonViewSuccess(bool success, string exceptionMessage = "", string exceptionType = "", string? requestId = null)
    {
        if (!success && !String.IsNullOrEmpty(exceptionMessage))
        {
            return new
            {
                success = success,
                exception = exceptionMessage,
                exception_type = exceptionType,
                requestid = requestId,
                //taskId = _httpContextAccessor.HttpContext!.Request.FormOrQuery("taskId"),
                //toolId = _httpContextAccessor.HttpContext!.Request.FormOrQuery("toolId")
            };
        }
        return new { success = success };
    }

    private void AddNoCacheHeaders()
    {
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Pragma", "no-cache");
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");
    }

    private void AddApiCorsHeaders()
    {
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Access-Control-Allow-Headers", "*");
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Access-Control-Allow-Origin",
            (string)_httpContextAccessor.HttpContext!.Request?.Headers["Origin"] != null
                ? (string)_httpContextAccessor.HttpContext!.Request.Headers["Origin"]
                : "*"
             );
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Access-Control-Allow-Credentials", "true");
        // is this also required? Maybe after an OPTION request
        // response.Headers.TryAdd("Access-Control-Allow-Methods", "*");
    }

    #endregion
}