using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;

namespace Frever.Client.Core.Utils.HttpResults;

public class ValidationErrorResult
{
    public string ErrorType = "ValidationError";

    public ValidationErrorResult(ValidationException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        Message = ex.Message;
        Errors = ex.Errors.ToArray();
    }

    public bool Ok => false;

    public string Message { get; }

    public ValidationFailure[] Errors { get; }
}