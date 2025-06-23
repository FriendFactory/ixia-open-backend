using System;
using System.Collections.Generic;

#pragma warning disable CS8618

namespace Frever.Shared.MainDb.Entities;

public class Comment
{
    public long Id { get; set; }

    public long VideoId { get; set; }

    public long GroupId { get; set; }

    public DateTime Time { get; set; }

    public string Text { get; set; }

    public bool IsDeleted { get; set; }

    public List<Mention> Mentions { get; set; }

    public long? ReplyToCommentId { get; set; }

    public int ReplyCount { get; set; }

    public string Thread { get; set; }

    public long LikeCount { get; set; }

    public bool IsPinned { get; set; }

    public virtual Group Group { get; set; }
}

public class Mention
{
    public long GroupId { get; set; }

    public string Name { get; set; }

    public string Nickname { get; set; }
}