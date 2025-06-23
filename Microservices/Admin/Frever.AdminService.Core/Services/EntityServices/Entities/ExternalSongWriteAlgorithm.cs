using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.AdminService.Core.Services.ModelSettingsProviders;
using Frever.AdminService.Core.UoW;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.EntityServices;

internal sealed class ExternalSongWriteAlgorithm(
    UserInfo user,
    IEnumerable<IEntityValidator<ExternalSong>> validators,
    ILoggerFactory loggerFactory,
    AssetGroupProvider assetGroupProvider,
    IUnitOfWork unitOfWork,
    IEntityReadAlgorithm<ExternalSong> readAlgorithm,
    IEntityLifeCycle<ExternalSong> lifeCycle,
    WriteDbContext dbContext
) : DefaultEntityWriteAlgorithm<ExternalSong>(
    user,
    validators,
    loggerFactory,
    assetGroupProvider,
    unitOfWork,
    readAlgorithm,
    lifeCycle
)
{
    private readonly WriteDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public override async Task<bool> CanSave(ExternalSong entity, CallContext context)
    {
        var exists = await ReadAlgorithm.Any(entity.Id);

        if (context.Operation == WriteOperation.Create)
            return !exists;

        return exists;
    }

    public override Task PreSave(ExternalSong entity, CallContext context)
    {
        if (context.Operation == WriteOperation.Create)
            _dbContext.Entry(entity).State = EntityState.Added;

        return base.PreSave(entity, context);
    }
}