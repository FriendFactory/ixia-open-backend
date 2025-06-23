using FluentValidation;

namespace Common.Infrastructure.BasePath;

public class FreverBasePathOptions
{
    public string BasePath { get; set; }

    public bool IsApplicable => !string.IsNullOrWhiteSpace(BasePath);

    public void Validate()
    {
        var validator = new InlineValidator<FreverBasePathOptions>();
        validator.RuleFor(v => v.BasePath).Must(v => v.StartsWith("/")).When(v => !string.IsNullOrWhiteSpace(v.BasePath));

        validator.ValidateAndThrow(this);
    }
}