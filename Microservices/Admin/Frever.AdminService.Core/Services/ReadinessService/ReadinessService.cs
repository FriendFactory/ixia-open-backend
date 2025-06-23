using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using FluentValidation;
using Frever.AdminService.Core.Services.ReadinessService.DataAccess;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.ReadinessService;

public class ReadinessService(IReadinessRepository repo, IUserPermissionService permissionService, IValidator<ReadinessInfo> validator)
    : IReadinessService
{
    private readonly IReadinessRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly IValidator<ReadinessInfo> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<Readiness> Create(ReadinessInfo readinessInfo)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        ArgumentNullException.ThrowIfNull(readinessInfo);

        await _validator.ValidateAndThrowAsync(readinessInfo);

        var anyReadinessById = _repo.GetAll().Any(r => r.Id == readinessInfo.Id);
        if (anyReadinessById)
            throw AppErrorWithStatusCodeException.BadRequest("Readiness with such id already exists", "ReadinessExists");

        var anyReadinessByName = _repo.GetAll().Any(r => r.Name == readinessInfo.Name);
        if (anyReadinessByName)
            throw AppErrorWithStatusCodeException.BadRequest("Readiness with such name already exists", "ReadinessExists");

        var readiness = new Readiness {Id = readinessInfo.Id, Name = readinessInfo.Name};

        return await _repo.Add(readiness);
    }

    public async Task<Readiness> Update(ReadinessInfo readinessInfo)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        ArgumentNullException.ThrowIfNull(readinessInfo);

        await _validator.ValidateAndThrowAsync(readinessInfo);

        var anyReadinessByName = _repo.GetAll().Any(r => r.Name == readinessInfo.Name);
        if (anyReadinessByName)
            throw AppErrorWithStatusCodeException.BadRequest("Readiness with such name already exists", "ReadinessExists");

        var readiness = await _repo.GetAll().FirstOrDefaultAsync(r => r.Id == readinessInfo.Id);
        if (readiness == null)
            throw AppErrorWithStatusCodeException.BadRequest("There is no readiness with such id", "ReadinessExists");

        readiness.Name = readinessInfo.Name;

        return await _repo.Update(readiness);
    }

    public async Task Delete(long id)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        await _repo.Delete(id);
    }
}