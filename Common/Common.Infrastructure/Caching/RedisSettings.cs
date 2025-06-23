using FluentValidation;

namespace Common.Infrastructure.Caching;

public sealed class RedisSettings
{
    public string ConnectionString { get; set; }

    public bool EnableCaching { get; set; }

    public string ClientIdentifier { get; set; }

    public void Validate()
    {
        new RedisSettingsValidator().ValidateAndThrow(this);
    }

    private class RedisSettingsValidator : AbstractValidator<RedisSettings>
    {
        public RedisSettingsValidator()
        {
            RuleFor(e => e.ConnectionString).NotEmpty().MinimumLength(1);
            RuleFor(e => e.ClientIdentifier).NotEmpty().MinimumLength(1);
        }
    }
}