using System;
using System.Collections.Generic;
using AssetStoragePathProviding;
using Common.Models.Database.Interfaces;
using Frever.ClientService.Contract.Social;
using Frever.Protobuf;
using Frever.Shared.MainDb.Entities;
using Newtonsoft.Json;

#pragma warning disable CS8618

namespace Frever.Video.Contract;

public class VideoInfo : IEntity, IVideoNameSource
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public long Size { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedTime { get; set; }
    public VideoKpi Kpi { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public int CharactersCount { get; set; }
    public long? RemixedFromVideoId { get; set; }
    public GroupShortInfo Owner { get; set; }
    public GroupShortInfo OriginalCreator { get; set; }
    public long TopListPosition { get; set; }
    public bool IsRemixable { get; set; }
    public bool IsDeleted { get; set; }
    public VideoAccess Access { get; set; }
    public List<HashtagInfo> Hashtags { get; set; }
    public List<TaggedGroup> Mentions { get; set; }
    public TaggedGroup[] TaggedGroups { get; set; }
    public TaggedGroup[] NonCharacterTaggedGroups { get; set; }
    public string ThumbnailUrl { get; set; }
    public string RedirectUrl { get; set; }
    public Dictionary<string, string> SignedCookies { get; set; }
    public Dictionary<string, string> Links { get; set; }
    public string Key { get; set; }
    public string Description { get; set; }
    public SongInfo[] Songs { get; set; }
    public UserSoundInfo[] UserSounds { get; set; }
    public bool AllowRemix { get; set; }
    public bool AllowComment { get; set; }
    public bool IsFriend { get; set; }
    public bool IsFollower { get; set; }
    public bool IsFollowed { get; set; }
    public bool IsFollowRecommended { get; set; }
    public long? AiContentId { get; set; }
    [ProtobufIgnore] public string Version { get; set; }
    [ProtobufIgnore] [JsonIgnore] public string Location { get; set; }
}