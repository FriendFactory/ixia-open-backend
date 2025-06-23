using System.Threading.Tasks;
using Frever.Video.Core.Features.Shared;

namespace Frever.Video.Core.Features.MediaConversion.StatusUpdating;

public interface IVideoConversionStatusUpdateService
{
    Task<Frever.Shared.MainDb.Entities.Video> HandleVideoConversionCompletion(long videoId, VideoConversionType conversionType);
}