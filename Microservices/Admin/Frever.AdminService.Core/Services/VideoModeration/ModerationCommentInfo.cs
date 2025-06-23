using System;

namespace Frever.AdminService.Core.Services.VideoModeration;

public class ModerationCommentInfo
{
    public long Id { get; set; }

    public long VideoId { get; set; }

    public long GroupId { get; set; }

    public DateTime Time { get; set; }

    public string Text { get; set; }

    public bool IsDeleted { get; set; }

    public string GroupNickname { get; set; }
}