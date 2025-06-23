using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.CommercialMusic;

// Moved to k8s cron job
public class RefreshLocalMusicWorker : IHostedService, IDisposable
{
    private static readonly string LoadSongCacheKeyPrefix = "workers::local-music::load-songs";
    private static readonly string LoadSongLockCacheKey = $"{LoadSongCacheKeyPrefix}::lock".FreverUnversionedCache();

    private static readonly TimeSpan LoadSongsInterval = TimeSpan.FromMinutes(120);

    private static readonly Random Rnd = new();
    private static DateTime LastSuccessfulRun;

    private readonly Thread _loadSongsWorker;
    private readonly ILogger _log;
    private readonly IDatabase _redis;
    private readonly IServiceProvider _services;

    private readonly Guid _wid = Guid.NewGuid();

    private bool _isRunning;

    public RefreshLocalMusicWorker(ILoggerFactory loggerFactory, IServiceProvider services, IConnectionMultiplexer redisConnection)
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));
        if (redisConnection == null)
            throw new ArgumentNullException(nameof(redisConnection));

        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = loggerFactory.CreateLogger("Frever.SongLicenseCheck");
        _redis = redisConnection.GetDatabase();


        _loadSongsWorker = new Thread(LoadSongsWorkingFunction) {Name = "LicenseCheck.LoadSongs"};
        _loadSongsWorker.Start();
    }

    private static string LoadSongAtDateCacheKey(DateTime date)
    {
        return $"{LoadSongCacheKeyPrefix}::{date.Date:yyyy-MM-dd}".FreverUnversionedCache();
    }

    private async Task Refresh()
    {
        if (!NeedToRun())
            return;

        try
        {
            using var scope = _services.CreateScope();

            var musicService = scope.ServiceProvider.GetRequiredService<IRefreshLocalMusicService>();

            _log.LogInformation("Start downloading all tracks CSV");

            var filePath = await musicService.DownloadTracksCsv();

            _log.LogInformation("CSV downloaded in {p}, size={s}MB", filePath, new FileInfo(filePath).Length / 1024 / 1024);

            await musicService.RefreshTrackInfoFromCsv(filePath);

            if (!Debugger.IsAttached)
                File.Delete(filePath);

            SetNeedToRun(false);
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error loading songs to queue");
            SetNeedToRun(true);
        }
    }

    #region Infrastructure

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Starting");
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        _log.LogInformation("Stopped");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _loadSongsWorker.Abort();
    }

    private void LoadSongsWorkingFunction(object _)
    {
        while (true)
        {
            if (!_isRunning)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                continue;
            }

            using var scope = _log.BeginScope("[{wid}:{tick}]:LoadSongs: ", _wid.ToString("N"), Guid.NewGuid().ToString("N"));

            Refresh().Wait();

            scope.Dispose();

            var randomDelay = TimeSpan.FromSeconds(Rnd.Next(0, 100)); // Random delay to distribute loading across instances
            Thread.Sleep(Debugger.IsAttached ? TimeSpan.FromSeconds(5) : LoadSongsInterval + randomDelay);
        }
    }

    private bool NeedToRun()
    {
        if (Debugger.IsAttached)
            return true;

        if (_redis.KeyExists(LoadSongAtDateCacheKey(DateTime.UtcNow)))
        {
            _log.LogDebug("Songs were loaded today");
            return false;
        }

        if (!_redis.LockTake(LoadSongLockCacheKey, true, TimeSpan.FromMinutes(120)))
        {
            _log.LogDebug("Songs are loading in other instance");
            return false;
        }

        return true;
    }

    private void SetNeedToRun(bool needToRun)
    {
        if (needToRun)
        {
            _redis.LockRelease(LoadSongLockCacheKey, true);
        }
        else
        {
            _redis.StringSet(LoadSongAtDateCacheKey(DateTime.UtcNow), "y");
            _redis.KeyExpire(LoadSongAtDateCacheKey(DateTime.UtcNow), TimeSpan.FromHours(24));
            _redis.LockRelease(LoadSongLockCacheKey, true);
            _log.LogInformation("Songs CSV were processed successfully");
        }
    }

    #endregion
}