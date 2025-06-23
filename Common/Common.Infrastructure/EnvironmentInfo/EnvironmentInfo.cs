using System;
using FluentValidation;

namespace Common.Infrastructure.EnvironmentInfo;

public sealed class EnvironmentInfo
{
    public string Version { get; set; }

    public string Type { get; set; }

    public bool IsProduction => StringComparer.OrdinalIgnoreCase.Equals((Type ?? string.Empty).Trim(), KnownEnvironmentTypes.Production);

    public void Validate()
    {
        new EnvironmentInfoValidator().ValidateAndThrow(this);
    }

    public static class KnownEnvironmentTypes
    {
        public static readonly string Production = "production";
        public static readonly string Stage = "stage";
        public static readonly string Development = "dev";
        public static readonly string Test = "test";
    }

    private class EnvironmentInfoValidator : AbstractValidator<EnvironmentInfo>
    {
        public EnvironmentInfoValidator()
        {
            RuleFor(e => e.Version).NotEmpty().MinimumLength(1);
        }
    }
}