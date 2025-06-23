using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.ReadinessService.DataAccess;

public class PersistentReadinessRepository(IWriteDb db) : IReadinessRepository
{
    private readonly IWriteDb _mainDb = db ?? throw new ArgumentNullException(nameof(db));

    public IQueryable<Readiness> GetAll()
    {
        return _mainDb.Readiness;
    }

    public async Task<Readiness> Add(Readiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);

        await _mainDb.Readiness.AddAsync(readiness);
        await _mainDb.SaveChangesAsync();

        return await GetAll().FirstOrDefaultAsync(r => r.Id == readiness.Id);
    }

    public async Task<Readiness> Update(Readiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);

        await _mainDb.SaveChangesAsync();

        return await GetAll().FirstOrDefaultAsync(r => r.Id == readiness.Id);
    }

    public async Task Delete(long id)
    {
        var readiness = await _mainDb.Readiness.FirstOrDefaultAsync(r => r.Id == id);
        if (readiness != null)
        {
            _mainDb.Readiness.Remove(readiness);
            await _mainDb.SaveChangesAsync();
        }
    }
}