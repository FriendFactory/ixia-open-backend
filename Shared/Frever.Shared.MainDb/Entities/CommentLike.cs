using System;

namespace Frever.Shared.MainDb.Entities;

public class CommentLike
{
    public long Id { get; set; }

    public long VideoId { get; set; }

    public long CommentId { get; set; }

    public long GroupId { get; set; }

    public DateTime Time { get; set; }
}