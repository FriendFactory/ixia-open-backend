using System.Threading.Tasks;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Core.Utils;

namespace Frever.AdminService.Core.Services.EntityServices;

public static class EntityReadAlgorithmExtensions
{
    public static Task<bool> Any<TEntity>(this IEntityReadAlgorithm<TEntity> service, long id)
        where TEntity : class, IEntity
    {
        return service.GetOne(id).AnyAsyncSafe();
    }
}