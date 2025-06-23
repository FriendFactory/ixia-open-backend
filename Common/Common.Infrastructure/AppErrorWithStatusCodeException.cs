using System;
using System.Net;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure;

/// <summary>
///     Custom exception which provides custom Http status code.
/// </summary>
public class AppErrorWithStatusCodeException : Exception
{
    public AppErrorWithStatusCodeException(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    public AppErrorWithStatusCodeException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public AppErrorWithStatusCodeException(string message, Exception innerException, HttpStatusCode statusCode) : base(
        message,
        innerException
    )
    {
        StatusCode = statusCode;
    }

    public AppErrorWithStatusCodeException(string message, Exception innerException) : base(message, innerException) { }

    public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.InternalServerError;

    public string ErrorCode { get; private set; }

    public LogLevel LogLevel { get; private set; } = LogLevel.Error;

    public static AppErrorWithStatusCodeException AccountBlocked(LogLevel logLevel, Exception innerException = null)
    {
        return new AppErrorWithStatusCodeException("User account has been blocked", innerException)
               {
                   ErrorCode = ErrorCodes.Auth.AccountBlocked, StatusCode = HttpStatusCode.Forbidden, LogLevel = logLevel
               };
    }

    public static AppErrorWithStatusCodeException NotEmployee(Exception innerException = null)
    {
        return new AppErrorWithStatusCodeException("You need employee permissions to perform this operation", innerException)
               {
                   ErrorCode = ErrorCodes.Auth.AccountInsufficientPermissions, StatusCode = HttpStatusCode.Forbidden
               };
    }

    public static AppErrorWithStatusCodeException NotArtist(Exception innerException = null)
    {
        return new AppErrorWithStatusCodeException("You need artist rights to perform this operation", innerException)
               {
                   ErrorCode = ErrorCodes.Auth.AccountInsufficientPermissions, StatusCode = HttpStatusCode.Forbidden
               };
    }

    public static AppErrorWithStatusCodeException BadRequest(string errorDescription, string errorCode, Exception innerException = null)
    {
        if (string.IsNullOrWhiteSpace(errorDescription))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorDescription));
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorCode));

        return new AppErrorWithStatusCodeException(errorDescription, innerException)
               {
                   ErrorCode = errorCode, StatusCode = HttpStatusCode.BadRequest
               };
    }

    public static AppErrorWithStatusCodeException NotFound(string errorDescription, string errorCode, Exception innerException = null)
    {
        if (string.IsNullOrWhiteSpace(errorDescription))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorDescription));
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorCode));

        return new AppErrorWithStatusCodeException(errorDescription, innerException)
               {
                   ErrorCode = errorCode, StatusCode = HttpStatusCode.NotFound
               };
    }

    public static AppErrorWithStatusCodeException NotAuthorized(string errorDescription, string errorCode, Exception innerException = null)
    {
        if (string.IsNullOrWhiteSpace(errorDescription))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorDescription));
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorCode));

        return new AppErrorWithStatusCodeException(errorDescription, innerException)
               {
                   ErrorCode = errorCode, StatusCode = HttpStatusCode.Unauthorized
               };
    }

    public static AppErrorWithStatusCodeException Forbidden(string errorDescription, string errorCode, Exception innerException = null)
    {
        if (string.IsNullOrWhiteSpace(errorDescription))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorDescription));
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(errorCode));

        return new AppErrorWithStatusCodeException(errorDescription, innerException)
               {
                   ErrorCode = errorCode, StatusCode = HttpStatusCode.Forbidden
               };
    }
}