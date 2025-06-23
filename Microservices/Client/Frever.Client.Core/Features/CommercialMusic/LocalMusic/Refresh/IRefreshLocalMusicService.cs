using System.Threading.Tasks;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface IRefreshLocalMusicService
{
    Task<string> DownloadTracksCsv();
    Task RefreshTrackInfoFromCsv(string path);
}