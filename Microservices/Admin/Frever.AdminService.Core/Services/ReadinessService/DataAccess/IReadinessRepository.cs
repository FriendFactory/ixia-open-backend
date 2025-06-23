using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.ReadinessService.DataAccess;

public interface IReadinessRepository
{
    IQueryable<Readiness> GetAll();

    Task<Readiness> Add(Readiness readiness);

    Task<Readiness> Update(Readiness readiness);

    Task Delete(long id);
}