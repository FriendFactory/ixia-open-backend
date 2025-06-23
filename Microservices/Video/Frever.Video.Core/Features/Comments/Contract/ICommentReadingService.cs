using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

#pragma warning disable CS8603, CS8625

namespace Frever.Video.Core.Features.Comments;

public interface ICommentReadingService
{
    Task<UserCommentInfo> GetCommentById(long videoId, long commentId);

    Task<IQueryable<long>> GetWhoCommented(long videoId);

    Task<UserCommentInfo[]> GetRootComments(long videoId, string key = null, int takeOlder = 20, int takeNewer = 0);

    Task<UserCommentInfo[]> GetThreadComments(
        long videoId,
        string rootCommentKey,
        string key = null,
        int takeOlder = 20,
        int takeNewer = 20
    );

    Task<UserCommentInfo[]> GetPinnedComments(long videoId);
}