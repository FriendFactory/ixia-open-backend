using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Frever.Video.Core.Features.Uploading.Models;

namespace Frever.Video.Core.Features.Uploading.Validators;

public class VideoUploadingRequestBaseValidator<T> : AbstractValidator<T>
    where T : VideoUploadingRequestBase
{
    protected VideoUploadingRequestBaseValidator()
    {
        RuleFor(e => e.Description).MaximumLength(1000);
        RuleFor(e => e.Mentions)
           .Must(mentions => mentions.Count <= 10)
           .Custom(
                (list, context) =>
                {
                    ValidateOnDuplication();
                    ValidateOnLength();
                    return;

                    void ValidateOnLength()
                    {
                        if (list.Count > 10)
                        {
                            var validationFailure = new ValidationFailure(null, "Mention users reach limit") {ErrorCode = "MentionLimit"};

                            context.AddFailure(validationFailure);
                        }
                    }

                    void ValidateOnDuplication()
                    {
                        var mentions = new HashSet<string>();
                        var duplicated = new HashSet<string>();

                        foreach (var mention in list)
                            if (!mentions.Add(mention))
                                duplicated.Add(mention);

                        if (duplicated.Count != 0)
                        {
                            var validationFailure = new ValidationFailure(null, "You’ve already mentioned this user")
                                                    {
                                                        ErrorCode = "DuplicatedMention", AttemptedValue = duplicated
                                                    };
                            context.AddFailure(validationFailure);
                        }
                    }
                }
            );
        RuleFor(e => e.Hashtags).ForEach(h => h.MaximumLength(25));

        RuleFor(e => e.Links)
           .Must(l => l == null || (l.Count < 20 && l.All(k => !string.IsNullOrWhiteSpace(k.Key) && !string.IsNullOrWhiteSpace(k.Value))));
    }
}