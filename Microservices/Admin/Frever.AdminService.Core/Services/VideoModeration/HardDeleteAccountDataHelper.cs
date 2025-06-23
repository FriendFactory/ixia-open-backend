using System;
using System.Threading.Tasks;
using Amazon.S3;
using AssetStoragePathProviding;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.AdminService.Core.Services.VideoModeration.DataAccess;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.VideoModeration;

public class HardDeleteAccountDataHelper(
    IVideoRepository videoRepository,
    ICache cache,
    VideoNamingHelper videoNamingHelper,
    IAmazonS3 s3,
    ILoggerFactory loggerFactory
)
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IAmazonS3 _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
    private readonly VideoNamingHelper _videoNamingHelper = videoNamingHelper ?? throw new ArgumentNullException(nameof(videoNamingHelper));
    private readonly IVideoRepository _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));

    private readonly ILogger _logger = loggerFactory.CreateLogger("Frever.HardDeleteAccountDataHelper");

    public async Task HardDeleteAccountData(long groupId)
    {
        await _videoRepository.MarkAccountVideosAsDeleted(groupId);
        await _videoRepository.EraseAccountComments(groupId);

        var publicInfix = VideoCacheKeys.PublicPrefix.GetKeyWithoutVersion();
        await _cache.DeleteKeysWithInfix(publicInfix);

        await DeleteAccountVideoFiles(groupId);
    }

    private async Task DeleteAccountVideoFiles(long groupId)
    {
        var folder = _videoNamingHelper.GetVideoMainFolderPathByGroupId(groupId);

        _logger.LogInformation("Deleting video files from folder {Folder}", folder);

        await _s3.DeleteFolder(_videoNamingHelper.VideoBucket, folder, m => _logger.LogInformation(m));
    }
}