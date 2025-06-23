using System.Threading.Tasks;

namespace Frever.Shared.MainDb;

public interface IMigrator
{
    Task Migrate();
}