#pragma warning disable CS8618

namespace Frever.Video.Core.Features.Comments;

public class AddCommentRequest
{
    public string Text { get; set; }

    public long? ReplyToCommentId { get; set; }
}