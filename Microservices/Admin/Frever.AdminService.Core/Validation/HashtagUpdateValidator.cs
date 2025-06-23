using FluentValidation;
using Frever.AdminService.Core.Services.HashtagModeration;

namespace Frever.AdminService.Core.Validation;

public class HashtagUpdateValidator : AbstractValidator<HashtagUpdate>
{
    public HashtagUpdateValidator()
    {
        RuleFor(e => e.Name).MaximumLength(25).Matches(@"^\w{1,25}$");
        RuleFor(e => e.ChallengeSortOrder).Must(e => e >= 0);
    }
}