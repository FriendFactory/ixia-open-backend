using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

internal sealed class DefaultEntityLifeCycle<TEntity> : IEntityLifeCycle<TEntity>
    where TEntity : class, IEntity
{
    public Task OnCreated(TEntity entity)
    {
        return Task.CompletedTask;
    }

    public Task OnUpdated(TEntity entity)
    {
        return Task.CompletedTask;
    }

    public Task OnDeleted(TEntity entity)
    {
        return Task.CompletedTask;
    }
}