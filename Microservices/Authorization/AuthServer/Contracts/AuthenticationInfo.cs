using FluentValidation;

namespace AuthServer.Contracts;

public class AuthenticationInfo
{
    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string UserName { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(PhoneNumber);
    }
}

public class AuthenticationInfoValidator : AbstractValidator<AuthenticationInfo>
{
    public AuthenticationInfoValidator()
    {
        RuleFor(e => e.Email)
           .Cascade(CascadeMode.Stop)
           .EmailAddress()
           .WithMessage("Invalid email format")
           .When(e => !string.IsNullOrWhiteSpace(e.Email));
    }
}