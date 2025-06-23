using System.Threading.Tasks;

namespace Frever.Client.Core.Features.CommercialMusic.LicenseChecking;

public interface IMusicLicenseCheckService
{
    Task LoadUncheckedSongsToQueue();

    Task RefreshExternalSongLicensingInfo(long externalSongId);

    Task ProcessSongQueue();
}