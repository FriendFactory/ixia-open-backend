using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

public interface IEntityLifeCycle<TEntity>
    where TEntity : class, IEntity
{
    Task OnCreated(TEntity entity);
    Task OnUpdated(TEntity entity);
    Task OnDeleted(TEntity entity);
}