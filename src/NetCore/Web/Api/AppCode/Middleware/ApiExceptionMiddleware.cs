using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Extensions.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }

        catch (Exception ex)
        {
            //context.Response.StatusCode = StatusCodes.Status400BadRequest;
           
            context.Response
                .AddNoCacheHeaders()
                .AddApiCorsHeaders(context.Request);

            var error = new ApiBaseController.JsonException()
            {
                success = false,
                exception = ex is IGenericExceptionMessage gex
                    ? gex.GenericMessage
                    : ex.Message,
                exception_type = ex.GetType().Name,
                requestid = context.Request.QueryOrForm("requestid", "")
            };

            if (ex is NullReferenceException || ApiGlobals.IsDevelopmentEnvironment)
            {
                error.exception += Environment.NewLine + ex.StackTrace;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(error);
        }
    }
}