using System;
using Common.Models.Files;
using FluentValidation;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.MusicModeration.Contracts;

public class UserSoundDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long GroupId { get; set; }
    public long? Size { get; set; }
    public int Duration { get; set; }
    public string CopyrightCheckResults { get; set; }
    public bool? ContainsCopyrightedContent { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int UsageCount { get; set; }
    public FileMetadata[] Files { get; set; }
}

public class UserSoundValidator : AbstractValidator<UserSoundDto>
{
    public UserSoundValidator(IAdvancedFileStorageService fileStorage)
    {
        RuleFor(e => e.Name).NotEmpty();
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.GroupId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Duration).GreaterThanOrEqualTo(0);

        this.AddFileMetadataValidation<UserSoundDto, UserSound>(fileStorage);
    }
}