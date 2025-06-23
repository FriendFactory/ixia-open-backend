using System;
using FluentValidation;

namespace AuthServer.Contracts;

public class UpdateAccountRequest
{
    public DateTime BirthDate { get; set; }
    public string DefaultLanguage { get; set; }
    public string Country { get; set; }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(e => e.Country).NotEmpty().Length(2, 3);
        RuleFor(e => e.DefaultLanguage).NotEmpty().Length(2, 3);
        RuleFor(e => e.BirthDate).LessThanOrEqualTo(DateTime.UtcNow).GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-150));
    }
}