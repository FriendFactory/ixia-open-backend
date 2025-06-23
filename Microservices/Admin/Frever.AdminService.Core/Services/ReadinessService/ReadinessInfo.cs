using FluentValidation;

namespace Frever.AdminService.Core.Services.ReadinessService;

public class ReadinessInfo
{
    public long Id { get; set; }

    public string Name { get; set; }
}

public class ReadinessInfoValidator : AbstractValidator<ReadinessInfo>
{
    public ReadinessInfoValidator()
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0).OverridePropertyName("id");
        RuleFor(e => e.Name).NotEmpty().OverridePropertyName("name");
    }
}