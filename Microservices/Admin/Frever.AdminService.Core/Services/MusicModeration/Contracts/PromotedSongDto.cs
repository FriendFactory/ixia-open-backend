using Common.Models.Files;
using FluentValidation;

namespace Frever.AdminService.Core.Services.MusicModeration.Contracts;

public class PromotedSongDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string[] AvailableForCountries { get; set; } = [];
    public FileMetadata[] Files { get; set; }
}

public class PromotedSongValidator : AbstractValidator<PromotedSongDto>
{
    public PromotedSongValidator()
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(e => e.SongId).GreaterThanOrEqualTo(0);
        RuleFor(e => new {e.SongId, e.ExternalSongId})
           .Must(e => e.SongId.HasValue || e.ExternalSongId.HasValue)
           .WithMessage("SongId or ExternalSongId must be provided");
    }
}