using System;
using Common.Models.Database.Interfaces;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class Song : IFileMetadataConfigRoot, IAdminAsset, IGroupAccessible, IUploadedByUser, IUpdatedByUser, IStageable, ITaggable
{
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
    public long GenreId { get; set; }
    public long ArtistId { get; set; }
    public long LabelId { get; set; }
    public long GroupId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long ReadinessId { get; set; }
    public long? Size { get; set; }
    public int Channels { get; set; }
    public int SamplingSize { get; set; }
    public int Duration { get; set; }
    public int SamplingFrequency { get; set; }
    public string Name { get; set; }
    public long? AlbumId { get; set; }
    public long UploaderUserId { get; set; }
    public long UpdatedByUserId { get; set; }
    public long[] Tags { get; set; }
    public long[] Emotions { get; set; }
    public int SortOrder { get; set; }
    public bool ParentalExplicit { get; set; }
    public long? MoodId { get; set; }
    public string ExternalPartnerId { get; set; }
    public string[] AvailableForCountries { get; set; } = [];

    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public int UsageCount { get; set; }

    public virtual Album Album { get; set; }
    public virtual Artist Artist { get; set; }
    public virtual Genre Genre { get; set; }
    public virtual Group Group { get; set; }
    public virtual Label Label { get; set; }
    public virtual Mood Mood { get; set; }
    public virtual Readiness Readiness { get; set; }
    public virtual User UploaderUser { get; set; }
}