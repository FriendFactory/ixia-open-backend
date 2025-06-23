using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.HashtagModeration.DataAccess;

public interface IHashtagRepository
{
    IQueryable<Hashtag> GetAll();
    Task<bool> SoftDeleteAsync(long hashtagId, CancellationToken token = default);
    Task<Hashtag> GetByIdAsync(long hashtagId, CancellationToken token = default);
    Task<bool> UpdateAsync(Hashtag hashtag, CancellationToken token = default);
}