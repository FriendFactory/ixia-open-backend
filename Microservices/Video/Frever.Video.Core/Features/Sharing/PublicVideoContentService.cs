using System.Collections.Generic;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using AuthServerShared;
using Frever.Video.Contract;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.Shared;

namespace Frever.Video.Core.Features.Sharing;

public interface IPublicVideoContentService
{
    Task<VideoContentInfo> GetMyOrPublicVideoContent(long videoId);
    Task<string> GetVideoPlayerUrl(long videoId);
    Task<VideoSharingInfo> GetVideoSharingInfo(long videoId);
    Task<string> GetVideoSingleFileUrl(long videoId);
}

public class PublicVideoContentService(
    IVideoAssetUrlGenerator urlGenerator,
    VideoNamingHelper namingHelper,
    IVideoShareService shareService,
    UserInfo currentUser,
    IOneVideoAccessor oneVideoAccessor
) : IPublicVideoContentService
{
    public async Task<VideoContentInfo> GetMyOrPublicVideoContent(long videoId)
    {
        var video = await GetVideo(videoId);

        if (video == null)
            return null;

        var cookies = await urlGenerator.CreateSignedCookie(video);

        var sharingUrl = urlGenerator.CreateSignedUrl(namingHelper.GetSharingVideoUrl(video));
        var videoContentInfo = new VideoContentInfo
                               {
                                   Id = video.Id,
                                   GroupId = video.GroupId,
                                   RedirectUrl = namingHelper.GetVideoUrl(video),
                                   SharingUrl = sharingUrl,
                                   PlayerUrl = sharingUrl,
                                   ThumbnailUrl = urlGenerator.CreateSignedUrl(namingHelper.GetVideoThumbnailUrl(video)),
                                   SingleFileVideoUrl =
                                       urlGenerator.CreateSignedUrl(namingHelper.GetSharingVideoUrl(video)),
                                   SignedCookies = new Dictionary<string, string>
                                                   {
                                                       {cookies.Policy.Key, cookies.Policy.Value},
                                                       {cookies.Signature.Key, cookies.Signature.Value},
                                                       {cookies.KeyPairId.Key, cookies.KeyPairId.Value}
                                                   }
                               };

        videoContentInfo.PlayerUrl = await shareService.GetSharedPlayerUrl(video, videoContentInfo.SingleFileVideoUrl);
        videoContentInfo.SharingUrl = videoContentInfo.PlayerUrl;

        return videoContentInfo;
    }

    public async Task<string> GetVideoPlayerUrl(long videoId)
    {
        var video = await GetVideo(videoId);

        if (video == null)
            return null;

        var singleFileVideoUrl = urlGenerator.CreateSignedUrl(namingHelper.GetSharingVideoUrl(video));

        var playerUrl = await shareService.GetSharedPlayerUrl(video, singleFileVideoUrl);

        return playerUrl;
    }

    public async Task<VideoSharingInfo> GetVideoSharingInfo(long videoId)
    {
        var video = await GetVideo(videoId);
        if (video == null)
            return null;

        var singleFileVideoUrl = urlGenerator.CreateSignedUrl(namingHelper.GetSharingVideoUrl(video));

        var result = await shareService.GetVideoSharingInfo(currentUser, video, singleFileVideoUrl);

        return result;
    }

    public async Task<string> GetVideoSingleFileUrl(long videoId)
    {
        var video = await GetVideo(videoId);

        return video == null ? null : urlGenerator.CreateSignedUrl(namingHelper.GetSharingVideoUrl(video));
    }

    private Task<VideoInfo> GetVideo(long id)
    {
        return oneVideoAccessor.GetVideo(FetchVideoInfoFrom.WriteDb, currentUser, id);
    }
}