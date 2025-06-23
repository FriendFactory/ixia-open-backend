using FluentValidation;

namespace Frever.Video.Core.Features.ReportInappropriate;

public sealed class ReportInappropriateVideoRequestValidator : AbstractValidator<ReportInappropriateVideoRequest>
{
    public ReportInappropriateVideoRequestValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty();
        RuleFor(x => x.ReasonId).NotEmpty();
        RuleFor(x => x.Message).Length(1, 1024);
    }
}