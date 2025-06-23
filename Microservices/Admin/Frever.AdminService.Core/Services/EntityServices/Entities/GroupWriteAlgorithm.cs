using System.Collections.Generic;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.AdminService.Core.Services.ModelSettingsProviders;
using Frever.AdminService.Core.UoW;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.EntityServices;

internal class GroupWriteAlgorithm(
    UserInfo user,
    IEnumerable<IEntityValidator<Group>> validators,
    ILoggerFactory loggerFactory,
    AssetGroupProvider assetGroupProvider,
    IUnitOfWork unitOfWork,
    IEntityReadAlgorithm<Group> readAlgorithm,
    IEntityLifeCycle<Group> entityLifeCycle
) : DefaultEntityWriteAlgorithm<Group>(
    user,
    validators,
    loggerFactory,
    assetGroupProvider,
    unitOfWork,
    readAlgorithm,
    entityLifeCycle
)
{
    public override Task ModifyInputEntity(Group entity, CallContext context)
    {
        entity.NickName = entity.NickName?.ToLowerInvariant().Trim();

        return base.ModifyInputEntity(entity, context);
    }
}