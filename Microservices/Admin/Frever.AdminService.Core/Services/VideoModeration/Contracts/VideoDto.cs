using System;
using AssetStoragePathProviding;
using Frever.AdminService.Core.Services.AiContent;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;

#pragma warning disable CS8618

namespace Frever.AdminService.Core.Services.VideoModeration.Contracts;

public class VideoDto : IVideoNameSource
{
    public long Size { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedTime { get; set; }
    public int CharactersCount { get; set; }
    public string GroupNickName { get; set; }
    public long? RemixedFromVideoId { get; set; }
    public long? RemixedFromLevelId { get; set; }
    public long? OriginalCreatorGroupId { get; set; }
    public string OriginalCreatorGroupNickName { get; set; }
    public long? ToplistPosition { get; set; }
    public bool IsRemixable { get; set; }
    public long[] TemplateIds { get; set; }
    public bool IsDeleted { get; set; }
    public long? DeletedByGroupId { get; set; }
    public string ThumbnailUrl { get; set; }
    public VideoAccess Access { get; set; }
    public string Description { get; set; }
    public long? SchoolTaskId { get; set; }
    public int? StartListItem { get; set; }
    public string Language { get; set; }
    public string Country { get; set; }
    public long[] ExternalSongIds { get; set; }
    public long PublishTypeId { get; set; }
    public long? LevelTypeId { get; set; }
    public bool AllowRemix { get; set; }
    public bool AllowComment { get; set; }
    public VideoKpiInfo Kpi { get; set; }
    public SongInfo[] Songs { get; set; }
    public UserSoundInfo[] UserSounds { get; set; }
    public HashtagInfo[] Hashtags { get; set; }
    public TaggedGroup[] TaggedGroups { get; set; }
    public TaggedGroup[] Mentions { get; set; }

    public long? AiGeneratedContentId { get; set; }
    public AiGeneratedContentDto AiGeneratedContent { get; set; }
    public long Id { get; set; }
    public long? LevelId { get; set; }
    public long GroupId { get; set; }
    public string Version { get; set; }
}