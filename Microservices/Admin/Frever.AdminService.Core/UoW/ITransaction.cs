using System;
using System.Threading.Tasks;

namespace Frever.AdminService.Core.UoW;

public interface ITransaction : IDisposable, IAsyncDisposable
{
    Task Commit();

    Task Rollback();
}