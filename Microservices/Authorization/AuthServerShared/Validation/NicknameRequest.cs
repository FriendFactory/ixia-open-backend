using System.Linq;
using FluentValidation;

namespace AuthServerShared.Validation;

public class NicknameRequest
{
    public string Nickname { get; set; }
}

public class NicknameRequestValidator : AbstractValidator<NicknameRequest>
{
    public NicknameRequestValidator()
    {
        RuleFor(e => e.Nickname)
           .Cascade(CascadeMode.Stop)
           .NotEmpty()
           .WithMessage("User name cannot be null")
           .Must(n => !n.Any(char.IsWhiteSpace))
           .WithMessage("User name can not contain spaces")
           .Length(2, 24)
           .WithMessage("User name should be between 2 and 24 chars");
    }
}