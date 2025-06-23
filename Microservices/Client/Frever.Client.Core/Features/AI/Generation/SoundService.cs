using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AssetStoragePathProviding;
using Common.Infrastructure;
using Frever.Client.Core.Features.CommercialMusic;
using Frever.Client.Shared.Files;
using Frever.Videos.Shared.MusicGeoFiltering;

namespace Frever.Client.Core.Features.AI.Generation;

public interface ISoundService
{
    Task<string> GetSoundServerPath(long? songId, long? externalSongId, long? userSoundId);
}

public class SoundService(
    IAmazonS3 s3,
    IGenerationRepository repo,
    VideoNamingHelper namingHelper,
    ICurrentLocationProvider locationProvider,
    IMusicGeoFilter musicGeoFilter,
    IMusicProviderService musicProviderService
) : ISoundService
{
    public const string LipSyncAudioFolder = "Ai/latent-sync";

    public async Task<string> GetSoundServerPath(long? songId, long? externalSongId, long? userSoundId)
    {
        if(songId == null && externalSongId == null && userSoundId == null)
            return null;

        if (await musicGeoFilter.AreAnySongUnavailable(
                (await locationProvider.Get()).CountryIso3Code,
                externalSongId.HasValue ? [externalSongId.Value] : [],
                songId.HasValue ? [songId.Value] : []
            ))
            throw AppErrorWithStatusCodeException.BadRequest("Sound not available in your country", "MusicNotAvailable");

        if (songId.HasValue)
        {
            var song = await repo.GetSongById(songId.Value);
            return song.Files.Main()?.Path;
        }

        if (userSoundId.HasValue)
        {
            var userSound = await repo.GetUserSoundById(userSoundId.Value);
            return userSound.Files.Main()?.Path;
        }

        return await GetExternalSongFilePath(externalSongId!.Value);
    }

    private async Task<string> GetExternalSongFilePath(long id)
    {
        var externalSong = await repo.GetExternalSongById(id);
        if (externalSong == null)
            throw AppErrorWithStatusCodeException.NotFound("External song not found", "SoundNotFound");

        var bucketName = namingHelper.VideoBucket;
        var name = $"{externalSong.Id}-{externalSong.ArtistName.ToLower()}-{externalSong.SongName.ToLower()}".Replace(' ', '-');
        var path = $"{LipSyncAudioFolder}/external-songs/{name}.mp3";

        if (await FileExists(bucketName, path))
            return path;

        var bytes = await musicProviderService.DownloadExternalSongById(id);

        using var stream = new MemoryStream(bytes);

        var putRequest = new PutObjectRequest
                         {
                             Key = path,
                             BucketName = bucketName,
                             InputStream = stream,
                             ContentType = "audio/mpeg"
                         };

        await s3.PutObjectAsync(putRequest);

        return path;
    }

    private async Task<bool> FileExists(string bucketName, string path)
    {
        var response = await s3.ListObjectsV2Async(new ListObjectsV2Request {BucketName = bucketName, Prefix = path, MaxKeys = 1});
        return response.S3Objects.Any(obj => obj.Key == path);
    }
}