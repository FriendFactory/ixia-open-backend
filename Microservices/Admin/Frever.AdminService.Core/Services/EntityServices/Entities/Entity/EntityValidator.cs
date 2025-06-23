using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

internal class EntityValidator<TEntity> : IEntityValidator<TEntity>
    where TEntity : class, IEntity
{
    public Task<ValidationResult> Validate(TEntity entity, CallContext context)
    {
        // Validates identifier only for root call
        // Nested entities could be updated due create and vice versa
        if (context.OriginalEntity != entity)
            return Task.FromResult(ValidationResult.Valid);

        switch (context.Operation)
        {
            case WriteOperation.Create:
                if (entity.Id != 0)
                    return Task.FromResult(ValidationResult.Fail("Id should not be provided on creating entity"));

                break;
            case WriteOperation.Update:
                if (entity.Id == 0)
                    return Task.FromResult(ValidationResult.Fail("Entity should have ID set to update"));

                break;
            case WriteOperation.Delete:
                if (entity.Id == 0)
                    return Task.FromResult(ValidationResult.Fail("Id should be provided for deleting entity"));

                break;
        }

        return Task.FromResult(ValidationResult.Valid);
    }
}