using System.Diagnostics.CodeAnalysis;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Common.IntegrationTesting.Data.Video;

public static class VideoDataEnv
{
    public static async Task RefreshVideoKpi(this DataEnvironment dataEnv)
    {
        await dataEnv.WithScript("update-video-stats");
    }

    public static async Task<Shared.MainDb.Entities.Video[]> WithVideo(this DataEnvironment dataEnvironment, params VideoInput[] videos)
    {
        var created = await dataEnvironment.WithEntityCollection<Shared.MainDb.Entities.Video>("create-video-with-stats", videos);
        var hashtags = created.Zip(videos)
                              .Where(r => r.Second.Hashtags.Length != 0)
                              .Select(r => new VideoHashtagInput {VideoId = r.First.Id, Hashtags = r.Second.Hashtags})
                              .ToArray();

        if (hashtags.Length != 0)
            await dataEnvironment.AssignHashtags(hashtags);

        return created;
    }

    private static async Task AssignHashtags(this DataEnvironment dataEnvironment, params VideoHashtagInput[] hashtags)
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);
        ArgumentNullException.ThrowIfNull(hashtags);

        var existingHashtags = await dataEnvironment.Db.Hashtag.ToArrayAsync();
        var missingHashtags = hashtags.SelectMany(v => v.Hashtags).Where(h => !existingHashtags.Any(e => e.Name == h)).Distinct().ToArray();

        dataEnvironment.Db.Hashtag.AddRange(missingHashtags.Select(i => new Hashtag {Name = i}));
        await dataEnvironment.Db.SaveChangesAsync();

        existingHashtags = await dataEnvironment.Db.Hashtag.AsNoTracking().ToArrayAsync();

        var videoAndHt = await dataEnvironment.Db.VideoAndHashtag.ToArrayAsync();

        dataEnvironment.Db.VideoAndHashtag.AddRange(
            hashtags
               .SelectMany(i => i.Hashtags.Select(h => new {i.VideoId, HashtagId = existingHashtags.FirstOrDefault(a => a.Name == h)?.Id}))
               .Where(a => a.HashtagId != null)
               .Where(a => !videoAndHt.Any(vht => vht.HashtagId == a.HashtagId.Value && vht.VideoId == a.VideoId))
               .Select(a => new VideoAndHashtag {VideoId = a.VideoId, HashtagId = a.HashtagId.Value})
        );

        await dataEnvironment.Db.SaveChangesAsync();
    }

    private static void FillUpMissingVideo(VideoInput video)
    {
        video.Language ??= "swe";
        video.Country ??= "swe";
    }
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class VideoInput
{
    public long GroupId { get; set; }

    public long? LevelId { get; set; }

    public long? AiContentId { get; set; }

    public VideoAccess Access { get; set; } = VideoAccess.Public;

    public string Version { get; set; } = Guid.NewGuid().ToString("N");

    public string Language { get; set; } = "swe";

    public string Country { get; set; } = "swe";

    public int? ToplistPosition { get; set; }

    public long[] TemplateIds { get; set; } = [];

    public string Description { get; set; }

    public bool IsRemixable { get; set; } = true;

    public bool AllowRemix { get; set; } = true;

    public bool AllowComment { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    [SqlParamJson] public SongInfo[] Songs { get; set; } = [];

    [SqlParamFlattenNested] public VideoKpiInput Kpi { get; set; } = new();

    public string[] Hashtags { get; set; } = [];

    public long? SchoolTaskId { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public bool AvailableForRating { get; set; }

    public bool RatingCompleted { get; set; }
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class VideoKpiInput
{
    public int Likes { get; set; }

    public int Views { get; set; }

    public int Comments { get; set; }

    public int Shares { get; set; }

    public int Remixes { get; set; }

    public int BattlesWon { get; set; }

    public int BattlesLost { get; set; }
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class VideoHashtagInput
{
    public long VideoId { get; set; }

    public string[] Hashtags { get; set; }
}