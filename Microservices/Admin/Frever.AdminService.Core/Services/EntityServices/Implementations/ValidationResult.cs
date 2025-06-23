using System;
using System.Collections.Generic;
using System.Linq;

namespace Frever.AdminService.Core.Services.EntityServices;

public class ValidationResult
{
    public static readonly ValidationResult Valid = new();

    private readonly ISet<string> _validationErrors = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

    private ValidationResult(IEnumerable<string> errors = null)
    {
        if (errors != null && errors.Any())
            _validationErrors = new HashSet<string>(
                errors.Where(e => !string.IsNullOrWhiteSpace(e)),
                StringComparer.CurrentCultureIgnoreCase
            );
    }

    public IEnumerable<string> Errors => _validationErrors;

    public bool IsValid => _validationErrors.Count == 0;

    public ValidationResult WithErrors(params string[] errors)
    {
        return new ValidationResult(_validationErrors.Concat(errors ?? Enumerable.Empty<string>()));
    }

    public ValidationResult Compose(ValidationResult result)
    {
        return IsValid && result.IsValid ? Valid : new ValidationResult(Errors.Concat(result.Errors));
    }

    /// <summary>
    ///     If current result is valid performs next validation step via calling provided function.
    /// </summary>
    public ValidationResult Chain(Func<ValidationResult> nextValidation)
    {
        return IsValid ? nextValidation() : this;
    }

    public static ValidationResult Fail(params string[] errors)
    {
        return new ValidationResult(errors);
    }

    public static ValidationResult Fail(IEnumerable<string> errors)
    {
        return new ValidationResult(errors);
    }
}