using FluentValidation;
using Frever.Video.Core.Features.Uploading.Models;

namespace Frever.Video.Core.Features.Uploading.Validators;

public class CompleteNonLevelVideoUploadingRequestValidator : VideoUploadingRequestBaseValidator<CompleteNonLevelVideoUploadingRequest>
{
    public CompleteNonLevelVideoUploadingRequestValidator()
    {
        RuleFor(e => e.DurationSec).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Size).GreaterThanOrEqualTo(0);
    }
}