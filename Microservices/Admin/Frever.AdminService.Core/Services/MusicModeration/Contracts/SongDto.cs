using System;
using Common.Models.Files;
using FluentValidation;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.MusicModeration.Contracts;

public class SongDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long GenreId { get; set; }
    public long ArtistId { get; set; }
    public long LabelId { get; set; }
    public long? AlbumId { get; set; }
    public long? MoodId { get; set; }
    public long GroupId { get; set; }
    public long ReadinessId { get; set; }
    public long? Size { get; set; }
    public int Channels { get; set; }
    public int SamplingSize { get; set; }
    public int Duration { get; set; }
    public int SamplingFrequency { get; set; }
    public long UploaderUserId { get; set; }
    public long UpdatedByUserId { get; set; }
    public long[] Tags { get; set; }
    public long[] Emotions { get; set; }
    public int SortOrder { get; set; }
    public bool ParentalExplicit { get; set; }
    public string ExternalPartnerId { get; set; }
    public string[] AvailableForCountries { get; set; } = [];
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public int UsageCount { get; set; }
    public FileMetadata[] Files { get; set; }
}

public class SongValidator : AbstractValidator<SongDto>
{
    public SongValidator(IAdvancedFileStorageService fileStorage)
    {
        RuleFor(e => e.Name).NotEmpty();
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(e => e.GenreId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.ArtistId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.LabelId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.AlbumId).GreaterThanOrEqualTo(0).When(e => e.AlbumId.HasValue);
        RuleFor(e => e.MoodId).GreaterThanOrEqualTo(0).When(e => e.AlbumId.HasValue);
        RuleFor(e => e.GroupId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.ReadinessId).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Channels).GreaterThanOrEqualTo(0);
        RuleFor(e => e.SamplingSize).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Duration).GreaterThanOrEqualTo(0);
        RuleFor(e => e.SamplingFrequency).GreaterThanOrEqualTo(0);

        this.AddFileMetadataValidation<SongDto, Song>(fileStorage);
    }
}