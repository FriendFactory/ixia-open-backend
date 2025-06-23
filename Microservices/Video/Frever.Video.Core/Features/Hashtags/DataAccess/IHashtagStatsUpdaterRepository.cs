using System.Threading.Tasks;

namespace Frever.Video.Core.Features.Hashtags.DataAccess;

public interface IHashtagStatsUpdaterRepository
{
    Task RefreshHashtagViewsCountAsync();
    Task RefreshHashtagsVideoCountAsync();
}