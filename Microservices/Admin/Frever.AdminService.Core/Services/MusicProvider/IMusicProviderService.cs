using System.Threading.Tasks;
using Common.Infrastructure.MusicProvider;

namespace Frever.AdminService.Core.Services.MusicProvider;

public interface IMusicProviderService
{
    Task<string> SendMusicProviderRequest(MusicProviderRequest request);
    Task<SignedRequestData> SignMusicProviderUrl(MusicProviderRequest request);
}