using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using Common.Infrastructure.Aws.Crypto;
using Microsoft.Extensions.Caching.Memory;

namespace Frever.Video.Core.Features.AssetUrlGeneration;

public interface IVideoAssetUrlGenerator
{
    string GetThumbnailUrl(IVideoNameSource video);

    Task<FreverAmazonCloudFrontCookiesForCustomPolicy> CreateSignedCookie(IVideoNameSource video);

    string CreateSignedUrl(string url);
}

public class CloudFrontVideoAssetUrlGenerator(VideoServerOptions videoServerOptions, VideoNamingHelper videoNamingHelper)
    : IVideoAssetUrlGenerator
{
    private static readonly IMemoryCache ResourceUrlToThumbnailUrl = new MemoryCache(
        new MemoryCacheOptions {SizeLimit = 2048, ExpirationScanFrequency = TimeSpan.FromMinutes(30)}
    );

    private static readonly IMemoryCache ResourcePathToSignedCookies = new MemoryCache(
        new MemoryCacheOptions {SizeLimit = 2048, ExpirationScanFrequency = TimeSpan.FromMinutes(30)}
    );

    private static readonly IMemoryCache UrlToSignedUrl = new MemoryCache(
        new MemoryCacheOptions {SizeLimit = 2048, ExpirationScanFrequency = TimeSpan.FromMinutes(30)}
    );


    public string GetThumbnailUrl(IVideoNameSource video)
    {
        var resourceUrl = videoNamingHelper.GetVideoThumbnailUrl(video);

        if (ResourceUrlToThumbnailUrl.TryGetValue(resourceUrl, out string thumbnailUrl))
            return thumbnailUrl;

        var thumbnail = FreverAmazonCloudFrontSigner.SignUrlCanned(
            resourceUrl,
            videoServerOptions.CloudFrontCertKeyPairId,
            DateTime.Now.AddDays(10)
        );

        var result = ResourceUrlToThumbnailUrl.GetOrCreate(
            resourceUrl,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                entry.SetSize(1);
                return thumbnail;
            }
        );

        return result;
    }

    public Task<FreverAmazonCloudFrontCookiesForCustomPolicy> CreateSignedCookie(IVideoNameSource video)
    {
        var resourcePath = videoNamingHelper.GetSignedCookieResourcePath(video);

        if (ResourcePathToSignedCookies.TryGetValue(resourcePath, out FreverAmazonCloudFrontCookiesForCustomPolicy cookies))
            return Task.FromResult(cookies);

        return Task.Run(
            () =>
            {
                var cookiesForCustomPolicy = FreverAmazonCloudFrontSigner.GetCookiesForCustomPolicy(
                    resourcePath,
                    videoServerOptions.CloudFrontCertKeyPairId,
                    DateTime.Now.AddMinutes(videoServerOptions.CloudFrontSignedCookieLifetimeMinutes),
                    DateTime.Now - new TimeSpan(
                        0,
                        1,
                        0
                    ), //needed to remove at least few seconds, because there was the issue when client start use this cookies immediately
                    null
                );
                var result = ResourcePathToSignedCookies.GetOrCreate(
                    resourcePath,
                    entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                        entry.SetSize(1);
                        return cookiesForCustomPolicy;
                    }
                );
                return result;
            }
        );
    }

    public string CreateSignedUrl(string url)
    {
        if (UrlToSignedUrl.TryGetValue(url, out string urlCaned))
            return urlCaned;

        var signUrlCanned = FreverAmazonCloudFrontSigner.SignUrlCanned(
            url,
            videoServerOptions.CloudFrontCertKeyPairId,
            DateTime.Now.AddDays(10)
        );

        var result = UrlToSignedUrl.GetOrCreate(
            url,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                entry.SetSize(1);
                return signUrlCanned;
            }
        );

        return result;
    }

    public static Dictionary<string, string> NormalizeLinks(Dictionary<string, string> links)
    {
        if (links == null || links.Count == 0)
            return null;

        var result = new Dictionary<string, string>();
        foreach (var (lt, url) in links)
        {
            var normalizedType = lt.ToLowerInvariant().Trim();
            var normalizedUrl = url.Trim();


            if (!string.IsNullOrEmpty(normalizedType))
                result[normalizedType] = normalizedUrl;
        }

        return result;
    }
}