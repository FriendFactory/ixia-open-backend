using Common.Models.Files;
using FluentValidation;
using Frever.Client.Shared.Files;

namespace Frever.AdminService.Core.Services.AI;

public class AiArtStyle : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public long GenderId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public FileMetadata[] Files { get; set; }
}

public class ArtStyleFileConfig : DefaultFileMetadataConfiguration<Shared.MainDb.Entities.AiArtStyle>
{
    public ArtStyleFileConfig()
    {
        AddThumbnail(512, "jpg");
    }
}

public class AiArtStyleValidator : AbstractValidator<AiArtStyle>
{
    public AiArtStyleValidator(IAdvancedFileStorageService fileStorage)
    {
        RuleFor(x => x.Files).NotEmpty();

        this.AddFileMetadataValidation<AiArtStyle, Shared.MainDb.Entities.AiArtStyle>(fileStorage);
    }
}