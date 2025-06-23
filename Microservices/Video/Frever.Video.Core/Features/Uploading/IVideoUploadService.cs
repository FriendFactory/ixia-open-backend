using System.Threading.Tasks;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Uploading.Models;

namespace Frever.Video.Core.Features.Uploading;

public interface IVideoUploadService
{
    Task<VideoUploadInfo> CreateVideoUpload();

    Task<Frever.Shared.MainDb.Entities.Video> CompleteNonLevelVideoUploading(
        string uploadId,
        CompleteNonLevelVideoUploadingRequest request
    );

    Task<Frever.Shared.MainDb.Entities.Video> PublishAiContent(CompleteNonLevelVideoUploadingRequest request);
}