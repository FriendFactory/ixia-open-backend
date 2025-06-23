using System;
using Common.Models.Files;
using Frever.Client.Core.Utils.Models;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.Song.Models;

public class Song : HasReadiness, IPublication, IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public long[] Tags { get; set; }
    public long GenreId { get; set; }
    public long LabelId { get; set; }
    public int SortOrder { get; set; }
    public bool ParentalExplicit { get; set; }
    public long? MoodId { get; set; }
    public int UsageCount { get; set; }
    public string[] AvailableForCountries { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public ArtistInfo Artist { get; set; }
    public AlbumInfo Album { get; set; }
    public FileMetadata[] Files { get; set; }
}