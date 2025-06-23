using System;
using System.Collections.Generic;
using AssetStoragePathProviding;
using NetTopologySuite.Geometries;

namespace Frever.Shared.MainDb.Entities;

public class Video : IVideoNameSource
{
    public Video()
    {
        Reposts = new HashSet<Reposts>();
        VideoAndHashtag = new HashSet<VideoAndHashtag>();
        VideoMentions = new HashSet<VideoMention>();
        VideoGroupTags = new HashSet<VideoGroupTag>();
    }

    public long Id { get; set; }
    public long? LevelId { get; set; }
    public long VerticalCategoryId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long Size { get; set; }
    public int ResolutionHeight { get; set; }
    public int ResolutionWidth { get; set; }
    public int Duration { get; set; }
    public int FrameRate { get; set; }
    public int PlatformId { get; set; }
    public bool Watermark { get; set; }
    public int CharactersCount { get; set; }
    public long? RemixedFromVideoId { get; set; }
    public long? ToplistPosition { get; set; }
    public long GroupId { get; set; }
    public bool IsDeleted { get; set; }
    public long? DeletedByGroupId { get; set; }
    public string Version { get; set; }
    public bool IsRemixable { get; set; }
    public long[] TemplateIds { get; set; }
    public string Description { get; set; }
    public VideoAccess Access { get; set; }
    public VideoConversion ConversionStatus { get; set; }
    public long? SchoolTaskId { get; set; }
    public double QualityInitial { get; set; }
    public long? GeneratedTemplateId { get; set; }
    public int? StartListItem { get; set; }
    public string Language { get; set; }
    public string Country { get; set; }
    public int? PinOrder { get; set; }
    public Dictionary<string, string> Links { get; set; }
    public long[] ExternalSongIds { get; set; }
    public SongInfo[] SongInfo { get; set; } = { };
    public UserSoundInfo[] UserSoundInfo { get; set; } = { };
    public long PublishTypeId { get; set; }
    public bool AllowRemix { get; set; }
    public bool AllowComment { get; set; }
    public long[] RaceIds { get; set; }
    public long UniverseId { get; set; }
    public bool AvailableForRating { get; set; }
    public DateTime? RatingCompletedAt { get; set; }
    public Point Location { get; set; }
    public long? StyleId { get; set; }
    public long? TransformedFromVideoId { get; set; }
    public bool? TransformationCompleted { get; set; }

    public long? AiContentId { get; set; }

    public virtual Group Group { get; set; }
    public virtual Platform Platform { get; set; }
    public virtual Video RemixedFromVideo { get; set; }
    public virtual VerticalCategory VerticalCategory { get; set; }
    public virtual VideoKpi VideoKpi { get; set; }
    public virtual ICollection<Reposts> Reposts { get; set; }
    public virtual ICollection<VideoAndHashtag> VideoAndHashtag { get; set; }
    public virtual ICollection<VideoMention> VideoMentions { get; set; }
    public virtual ICollection<VideoGroupTag> VideoGroupTags { get; set; }
    public virtual ICollection<Like> Likes { get; set; }
}

public class GroupFirstVideoInfo
{
    public long Id { get; set; }

    public long GroupId { get; set; }

    public int Ordinal { get; set; }

    public DateTime CreatedTime { get; set; }
}

public class SongInfo
{
    public long Id { get; set; }

    public string Artist { get; set; }

    public string Title { get; set; }

    public bool IsExternal { get; set; }

    public string Isrc { get; set; }
}

public class UserSoundInfo
{
    public long Id { get; set; }

    public string Name { get; set; }

    public long EventId { get; set; }
}

public class VideoWithSong
{
    public long Id { get; set; }
    public long Key { get; set; }
    public string SongInfo { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}