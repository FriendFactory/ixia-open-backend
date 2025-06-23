using System;
using FluentValidation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core.Validators;

public class AiGeneratedContentInputValidator : AbstractValidator<AiGeneratedContentInput>
{
    public AiGeneratedContentInputValidator(
        IValidator<AiGeneratedImageInput> imageValidator,
        IValidator<AiGeneratedVideoInput> videoValidator
    )
    {
        ArgumentNullException.ThrowIfNull(imageValidator);
        ArgumentNullException.ThrowIfNull(videoValidator);

        RuleFor(m => m.Image).NotNull().SetValidator(v => v.Image != null ? imageValidator : null).When(m => m.Video == null);
        RuleFor(m => m.Video).NotNull().SetValidator(v => v.Video != null ? videoValidator : null).When(m => m.Image == null);
    }
}

public class AiGeneratedImageInputValidator : AbstractValidator<AiGeneratedImageInput>
{
    public AiGeneratedImageInputValidator(
        IValidator<AiGeneratedImagePersonInput> personValidator,
        IValidator<AiGeneratedImageSourceInput> sourceValidator,
        IAdvancedFileStorageService fileStorage
    )
    {
        ArgumentNullException.ThrowIfNull(personValidator);
        ArgumentNullException.ThrowIfNull(sourceValidator);
        ArgumentNullException.ThrowIfNull(fileStorage);

        RuleFor(m => m.Prompt).MaximumLength(4096).When(v => v != null);
        RuleFor(m => m.Workflow).MaximumLength(4096);
        RuleFor(m => m.Persons).ForEach(e => e.SetValidator(personValidator)).When(a => a.Persons != null);
        RuleFor(m => m.Sources).ForEach(e => e.SetValidator(sourceValidator)).When(a => a.Sources != null);

        this.AddFileMetadataValidation<AiGeneratedImageInput, AiGeneratedImage>(fileStorage);
    }
}

public class AiGeneratedVideoInputValidator : AbstractValidator<AiGeneratedVideoInput>
{
    public AiGeneratedVideoInputValidator(IValidator<AiGeneratedVideoClipInput> videoClipValidator, IAdvancedFileStorageService fileStorage)
    {
        ArgumentNullException.ThrowIfNull(videoClipValidator);
        ArgumentNullException.ThrowIfNull(fileStorage);

        RuleFor(m => m.Clips)
           .NotEmpty()
           .Must(v => v.Count >= 1 && v.Count <= 3)
           .WithMessage("Video must contains 1-3 video clips")
           .ForEach(e => e.SetValidator(videoClipValidator));

        RuleFor(m => m.Workflow).MaximumLength(4096);


        this.AddFileMetadataValidation<AiGeneratedVideoInput, AiGeneratedVideo>(fileStorage);
    }
}

public class AiGeneratedImagePersonInputValidator : AbstractValidator<AiGeneratedImagePersonInput>
{
    public AiGeneratedImagePersonInputValidator(IAdvancedFileStorageService fileStorage)
    {
        ArgumentNullException.ThrowIfNull(fileStorage);

        RuleFor(m => m.ParticipantAiCharacterSelfieId).NotEmpty();
        this.AddFileMetadataValidation<AiGeneratedImagePersonInput, AiGeneratedImagePerson>(fileStorage);
    }
}

public class AiGeneratedImageSourceInputValidator : AbstractValidator<AiGeneratedImageSourceInput>
{
    public AiGeneratedImageSourceInputValidator(IAdvancedFileStorageService fileStorage)
    {
        ArgumentNullException.ThrowIfNull(fileStorage);
        this.AddFileMetadataValidation<AiGeneratedImageSourceInput, AiGeneratedImageSource>(fileStorage);
    }
}

public class AiGeneratedVideoClipInputValidator : AbstractValidator<AiGeneratedVideoClipInput>
{
    public AiGeneratedVideoClipInputValidator(IValidator<AiGeneratedImageInput> imageValidator, IAdvancedFileStorageService fileStorage)
    {
        ArgumentNullException.ThrowIfNull(imageValidator);
        ArgumentNullException.ThrowIfNull(fileStorage);

        RuleFor(m => m.Image).SetValidator(imageValidator).When(v => v != null);
        RuleFor(m => m.Prompt).MaximumLength(4096).When(v => v != null);
        RuleFor(m => m.Workflow).MaximumLength(4096);
        RuleFor(m => m.LengthSec).Must(v => v is >= 1 and <= 15).WithMessage("Clip length must be beteween 1 and 15 seconds");

        this.AddFileMetadataValidation<AiGeneratedVideoClipInput, AiGeneratedVideoClip>(fileStorage);
    }
}