using FluentValidation;

namespace Frever.AdminService.Core.Services.AccountModeration;

public class AccountModerationServiceOptions
{
    public string Bucket { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<AccountModerationServiceOptions>();
        validator.RuleFor(e => e.Bucket).NotEmpty().MinimumLength(1);

        validator.Validate(this);
    }
}