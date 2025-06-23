using FluentValidation;

namespace Frever.Video.Core.Features.Comments;

public class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(n => n.Text).NotEmpty().MinimumLength(1);
    }
}