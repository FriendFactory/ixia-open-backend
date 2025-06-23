using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.PersonalFeed.DataAccess;

public interface IPersonalFeedRepository
{
    Task<IQueryable<VideoView>> GetVideoViews(long groupId);
}