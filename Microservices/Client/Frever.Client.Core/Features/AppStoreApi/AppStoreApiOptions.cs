using FluentValidation;

namespace Frever.Client.Core.Features.AppStoreApi;

public class AppStoreApiOptions
{
    public string KeyId { get; set; }
    public string IssuerId { get; set; }
    public string KeyDataBase64 { get; set; }
    public string SharedSecret { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<AppStoreApiOptions>();
        validator.RuleFor(s => s.KeyId).NotNull().MinimumLength(1);
        validator.RuleFor(s => s.IssuerId).NotNull().MinimumLength(1);
        validator.RuleFor(s => s.KeyDataBase64).NotNull().MinimumLength(1);
        validator.RuleFor(s => s.SharedSecret).NotNull().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}