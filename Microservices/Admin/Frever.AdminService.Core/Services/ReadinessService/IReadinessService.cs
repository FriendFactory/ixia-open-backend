using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.ReadinessService;

public interface IReadinessService
{
    Task<Readiness> Create(ReadinessInfo readiness);

    Task<Readiness> Update(ReadinessInfo readiness);

    Task Delete(long id);
}