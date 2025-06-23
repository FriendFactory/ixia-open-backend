using FluentValidation;

namespace Frever.Client.Shared.AI.ComfyUi;

public class ComfyUiApiSettings
{
    public string QueueUrl { get; init; }

    public string ResponseQueueUrl { get; init; }

    public void Validate()
    {
        var validator = new InlineValidator<ComfyUiApiSettings>();
        validator.RuleFor(e => e.QueueUrl).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.ResponseQueueUrl).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}