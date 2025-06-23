using FluentValidation;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Sounds.UserSounds.Validators;

internal sealed class UserSoundValidator : AbstractValidator<UserSoundCreateModel>
{
    public UserSoundValidator(IAdvancedFileStorageService fileStorage)
    {
        RuleFor(e => e.Duration).GreaterThanOrEqualTo(0);

        this.AddFileMetadataValidation<UserSoundCreateModel, UserSound>(fileStorage);
    }
}