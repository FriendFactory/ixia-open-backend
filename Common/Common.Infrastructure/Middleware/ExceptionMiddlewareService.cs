using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Infrastructure.Middleware;

public class ExceptionMiddlewareService(ILogger<ExceptionMiddlewareService> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            logger.Log(ex.LogLevel, ex, "An exception thrown while processing the request");
            await HandleExceptionAsync(httpContext, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception thrown while processing the request");

            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Request.Headers["X-Request-Id"];

        var field = exception.GetType().GetProperties().FirstOrDefault(x => x.Name == "StatusCode");

        context.Response.StatusCode = field == null ? StatusCodes.Status500InternalServerError : (int) field.GetValue(exception);
        context.Response.ContentType = "application/json";

        var errorDetails = new ErrorDetailsViewModel
                           {
                               StatusCode = context.Response.StatusCode,
                               Message = exception.Message,
                               InnerException = exception.InnerException?.ToString(),
                               RequestId = requestId
                           };

        if (exception is AppErrorWithStatusCodeException ex)
        {
            context.Response.StatusCode = (int) ex.StatusCode;
            errorDetails = new ErrorDetailsViewModel
                           {
                               StatusCode = (int) ex.StatusCode,
                               Message = ex.Message,
                               ErrorCode = ex.ErrorCode,
                               InnerException = ex.InnerException?.ToString(),
                               RequestId = requestId
                           };
        }

        var errorDetailsJson = JsonConvert.SerializeObject(errorDetails);

        return context.Response.WriteAsync(errorDetailsJson);
    }
}

public class ErrorDetailsViewModel
{
    public string ErrorCode;
    public string InnerException;
    public string Message;
    public string RequestId;
    public int StatusCode;
}