using System;

namespace Frever.Shared.MainDb.Entities;

public class FavoriteSound
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? UserSoundId { get; set; }
    public DateTime Time { get; set; }
}

public class FavoriteSoundInternal
{
    public long Id { get; set; }
    public int Type { get; set; }
    public string SongName { get; set; }
    public string ArtistName { get; set; }
    public long? OwnerGroupId { get; set; }
    public int Duration { get; set; }
    public int UsageCount { get; set; }
    public string Files { get; set; }
    public DateTime Time { get; set; }
}