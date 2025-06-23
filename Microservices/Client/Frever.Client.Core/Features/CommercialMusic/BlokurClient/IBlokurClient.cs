using System.Threading.Tasks;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public interface IBlokurClient
{
    Task<BlokurStatusTestResponse> CheckRecordingStatus(BlokurStatusTestRequest request);

    Task DownloadTrackCsv(string fullPath);
    string MakeFullPathToTempFileName(string fileName);
}