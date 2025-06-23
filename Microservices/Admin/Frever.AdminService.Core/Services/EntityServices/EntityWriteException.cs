using System;
using System.Collections.Generic;

namespace Frever.AdminService.Core.Services.EntityServices;

public class EntityWriteException : Exception
{
    public EntityWriteException(string message, WriteOperation operation, int statusCode = 400) : base(message)
    {
        Operation = operation;
        StatusCode = statusCode;
    }

    public EntityWriteException(string message, Exception innerException, WriteOperation operation, int statusCode = 400) : base(
        message,
        innerException
    )
    {
        Operation = operation;
        StatusCode = statusCode;
    }

    public WriteOperation Operation { get; }

    public int StatusCode { get; }
}

public class EntityValidationException : EntityWriteException
{
    public EntityValidationException(ValidationResult validationResult, WriteOperation operation) : base(
        string.Join(Environment.NewLine, validationResult),
        operation
    )
    {
        if (validationResult.IsValid)
            throw new ArgumentException("Cannot throw with successful validation result");
        ValidationErrors = validationResult.Errors;
    }

    public IEnumerable<string> ValidationErrors { get; }
}