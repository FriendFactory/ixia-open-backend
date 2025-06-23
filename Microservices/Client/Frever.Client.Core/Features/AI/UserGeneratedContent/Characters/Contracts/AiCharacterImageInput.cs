using Common.Models.Files;
using FluentValidation;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;

public class AiCharacterImageInput : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Type { get; set; } = "ai-face";
    public long? DetectedGenderId { get; set; }
    public int? DetectedAge { get; set; }
    public string AiModelRequest { get; set; }
    public string AiModelResponse { get; set; }
    public FileMetadata[] Files { get; set; }
}

public class AiCharacterImageFileConfig : DefaultFileMetadataConfiguration<AiCharacterImage>
{
    public AiCharacterImageFileConfig()
    {
        AddMainFile("jpeg", false);
        AddThumbnail(128, "jpeg");
        AddFile("cover", "jpeg", false);
    }
}

public class AiCharacterImageValidator : AbstractValidator<AiCharacterImageInput>
{
    public AiCharacterImageValidator(IAdvancedFileStorageService fileStorage)
    {
        RuleFor(x => x.Files).NotEmpty();

        this.AddFileMetadataValidation<AiCharacterImageInput, AiCharacterImage>(fileStorage);
    }
}