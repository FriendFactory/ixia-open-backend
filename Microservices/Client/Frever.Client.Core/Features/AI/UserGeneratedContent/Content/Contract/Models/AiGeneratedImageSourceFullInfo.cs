using Common.Models.Files;
using Frever.Client.Shared.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedImageSourceFullInfo : IFileMetadataOwner
{
    public AiGeneratedImageSourceType Type { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}

public enum AiGeneratedImageSourceType
{
    Origin,
    MakeUp,
    Outfit,
    Background,
    Style,
    Camera,
    Pose,
    Lighting
}

public class AiGeneratedImageSourceFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedImageSource>
{
    public AiGeneratedImageSourceFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
    }
}