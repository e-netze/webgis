#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;

using Api.Core.AppCode.Extensions;

using E.Standard.Api.App;
using E.Standard.Api.App.Exceptions;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Json;
using E.Standard.WebGIS.Core.Models.Abstraction;
using E.Standard.WebMapping.Core.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Endpoints;

public class SecureEndpointHandlerService
{
    private ILogger<SecureEndpointHandlerService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEnumerable<ICustomApiService> _customServices;

    public SecureEndpointHandlerService(
            ILogger<SecureEndpointHandlerService> logger,
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<ICustomApiService> customServices
        )
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _customServices = customServices;
    }

    async public Task<IResult> HandlerAsync(
            Func<CmsDocument.UserIdentification, Task<IResult>> func,
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

    protected IResult HandleAuthenticationException()
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

    private IResult ThrowJsonException(Exception ex, int statusCode = 200, LogLevel logLevel = LogLevel.Error)
    {
        _logger.Log(logLevel, ex, "An json exception is thrown");

        string type = ex.GetType().ToString().ToLower();
        type = type.Substring(type.LastIndexOf(".") + 1);

        return JsonViewSuccess(false,
                               $"{ex.SecureMessage()}{(ex is NullReferenceException ? $" {ex.StackTrace}" : String.Empty)}",
                               type,
                               ex is ReportWarningException ? ((ReportWarningException)ex).RequestId : null);
    }

    private IResult JsonViewSuccess(bool success, string exceptionMessage = "", string exceptionType = "", string? requestId = null)
    {
        if (!success && !String.IsNullOrEmpty(exceptionMessage))
        {
            return Results.Json(new
            {
                success = success,
                exception = exceptionMessage,
                exception_type = exceptionType,
                requestid = requestId,
                //taskId = _httpContextAccessor.HttpContext!.Request.FormOrQuery("taskId"),
                //toolId = _httpContextAccessor.HttpContext!.Request.FormOrQuery("toolId")
            });
        }
        return Results.Json(new { success = success });
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
            (string?)_httpContextAccessor.HttpContext!.Request?.Headers["Origin"] != null
                ? (string)_httpContextAccessor.HttpContext!.Request.Headers["Origin"]!
                : "*"
             );
        _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Access-Control-Allow-Credentials", "true");
        // is this also required? Maybe after an OPTION request
        // response.Headers.TryAdd("Access-Control-Allow-Methods", "*");
    }

    #endregion

    #region Return Json

    async internal ValueTask<IResult> ApiJsonResult(object obj, bool pretty = false)
    {
        var httpContext = _httpContextAccessor.HttpContext!;

        if (_customServices.Any())
        {
            var json = JSerializer.Serialize(obj, pretty || ApiGlobals.IsDevelopmentEnvironment);

            await _customServices.HandleApiResultObject(obj as IWatchable, json, _httpContextAccessor.HttpContext?.User?.Identity?.Name);
        }

        httpContext.Response
            .AddNoCacheHeaders()
            .AddApiCorsHeaders(httpContext.Request);

        return Results.Json(obj);
    }

    internal IResult ApiRawResponse(byte[] responseBytes, string contentType, NameValueCollection headers)
    {
        var httpContext = _httpContextAccessor.HttpContext!;

        if (headers != null)
        {
            foreach (string header in headers)
            {
                httpContext.Response.Headers.Append(header, headers[header]);
            }
        }

        httpContext.Response.AddApiCorsHeaders(httpContext.Request);

        return ApiFileRespoinse(responseBytes, contentType);
    }

    internal IResult ApiRawResponse(byte[] responseBytes, string contentType, string filename)
    {
        var httpContext = _httpContextAccessor.HttpContext!;

        httpContext.Response.AddApiCorsHeaders(httpContext.Request);

        return ApiFileRespoinse(responseBytes, contentType, filename);
    }

    private IResult ApiFileRespoinse(byte[] data, string contentType, string fileName = "")
    {
        //if (!String.IsNullOrWhiteSpace(fileName))
        //{
        //    _httpContextAccessor.HttpContext!.Response.Headers.TryAdd("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        //}

        return Results.File(data, contentType, fileName);
    }

    #endregion
}
