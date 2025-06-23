using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

public interface IReadEntityService<TEntity> : IDisposable
    where TEntity : class, IEntity
{
    Task<TEntity> GetOne(long id);

    Task<IQueryable<TEntity>> GetAll(Func<IQueryable<TEntity>, IQueryable<TEntity>> processQuery, GetAllParams parameters = default);
}

public struct GetAllParams
{
    public string Expand { get; set; }
}