using System.Linq;
using FluentValidation;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient.Http;

public class BlokurStatusTestRequestValidator : AbstractValidator<BlokurStatusTestRequest>
{
    public BlokurStatusTestRequestValidator()
    {
        RuleFor(e => e.Recordings).Must(e => e.Length > 0).WithMessage("Recording must be not empty");
        RuleForEach(e => e.Recordings).SetValidator(new BlokurRecordingInputValidator());
    }
}

public class BlokurRecordingInputValidator : AbstractValidator<BlokurRecordingInput>
{
    public BlokurRecordingInputValidator()
    {
        RuleFor(e => e.AudioProviderRecordingId).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Title).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Artists)
           .Must(e => e.Length > 0)
           .WithMessage("Artist must be provided")
           .Must(e => e.All(a => !string.IsNullOrWhiteSpace(a)))
           .WithMessage("Artist must not be empty");

        RuleFor(e => e.Isrc).NotEmpty().MinimumLength(1);
    }
}