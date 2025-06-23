using FluentValidation;

namespace Frever.AdminService.Core.Services.AI;

public class AiWorkflowMetadata
{
    public long Id { get; set; }
    public string AiWorkflow { get; set; }
    public string Key { get; set; }
    public string Description { get; set; }
    public bool RequireBillingUnits { get; set; }
    public bool IsActive { get; set; }
    public int HardCurrencyPrice { get; set; }
    public int? EstimatedLoadingTimeSec { get; set; }
}

public class AiWorkflowMetadataValidator : AbstractValidator<AiWorkflowMetadata>
{
    public AiWorkflowMetadataValidator()
    {
        RuleFor(m => m.AiWorkflow).NotEmpty().MinimumLength(1).MaximumLength(1024);
        RuleFor(m => m.Key).NotEmpty().MinimumLength(1).MaximumLength(1024);
        RuleFor(m => m.AiWorkflow).MaximumLength(1024);
        RuleFor(m => m.HardCurrencyPrice).GreaterThanOrEqualTo(0);
        RuleFor(m => m.EstimatedLoadingTimeSec).GreaterThanOrEqualTo(0).When(v => v != null);
    }
}