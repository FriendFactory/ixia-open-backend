using System.Threading.Tasks;

namespace Frever.Video.Core.Features.MediaConversion.Client;

public interface IMediaConvertServiceClient
{
    Task CreateVideoConversionJob(
        long videoId,
        string sourceBucketPath,
        string destinationBucketPath,
        bool hasLandscapeOrientation = false
    );

    Task CreateMp3ExtractionJob(string sourceBucketPath, string destinationBucketPath);
}