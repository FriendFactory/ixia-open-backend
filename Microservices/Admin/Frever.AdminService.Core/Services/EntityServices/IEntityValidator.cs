using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

public interface IEntityValidator<in TEntity>
    where TEntity : class, IEntity
{
    Task<ValidationResult> Validate(TEntity entity, CallContext context);
}