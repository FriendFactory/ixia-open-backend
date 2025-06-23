using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Comments;

public interface ICommentModificationService
{
    Task<UserCommentInfo> AddComment(long videoId, AddCommentRequest request);
    Task<UserCommentInfo> LikeComment(long videoId, long commentId);
    Task<UserCommentInfo> UnlikeComment(long videoId, long commentId);
    Task<UserCommentInfo> PinComment(long videoId, long commentId);
    Task<UserCommentInfo> UnPinComment(long videoId, long commentId);
}