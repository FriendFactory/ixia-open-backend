using FluentValidation;

namespace Frever.AdminService.Core.Services.AI;

public class AiLlmPrompt
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string Prompt { get; set; }
}

public class AiLlmPromptValidator : AbstractValidator<AiLlmPrompt>
{
    public AiLlmPromptValidator()
    {
        RuleFor(e => e.Key).NotEmpty();
        RuleFor(e => e.Prompt).NotEmpty();
    }
}