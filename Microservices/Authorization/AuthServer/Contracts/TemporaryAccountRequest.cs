using FluentValidation;

namespace AuthServer.Contracts;

public class TemporaryAccountRequest
{
    public string DefaultLanguage { get; set; }
    public string Country { get; set; }
}

public class TemporaryAccountRequestValidator : AbstractValidator<TemporaryAccountRequest>
{
    public TemporaryAccountRequestValidator()
    {
        RuleFor(e => e.Country).NotEmpty().Length(2, 3).When(e => string.IsNullOrWhiteSpace(e.Country));
        RuleFor(e => e.DefaultLanguage).NotEmpty().Length(2, 3).When(e => string.IsNullOrWhiteSpace(e.DefaultLanguage));
    }
}