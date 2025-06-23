using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.CommercialMusic;

// Moved to k8s cron job
public class RefreshSpotifyPopularityWorker : IHostedService, IDisposable
{
    private static readonly string SpotifyPopularityCacheKeyPrefix = "workers::local-music::spotify-popularity";
    private static readonly string SpotifyPopularityLockCacheKey = $"{SpotifyPopularityCacheKeyPrefix}::lock".FreverUnversionedCache();

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(120);

    private static readonly Random Rnd = new();
    private static DateTime LastSuccessfulRun;

    private readonly Thread _loadSongsWorker;
    private readonly ILogger _log;
    private readonly IDatabase _redis;
    private readonly IServiceProvider _services;

    private readonly Guid _wid = Guid.NewGuid();

    private bool _isRunning;

    public RefreshSpotifyPopularityWorker(ILoggerFactory loggerFactory, IServiceProvider services, IConnectionMultiplexer redisConnection)
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));
        if (redisConnection == null)
            throw new ArgumentNullException(nameof(redisConnection));

        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = loggerFactory.CreateLogger("Frever.SpotifyPopularity");
        _redis = redisConnection.GetDatabase();


        _loadSongsWorker = new Thread(RefreshSpotifyPopularityWorkingFunction) {Name = "LicenseCheck.LoadSongs"};
        _loadSongsWorker.Start();
    }

    private static string RefreshSpotifyPopularityAtDateCacheKey(DateTime date)
    {
        return $"{SpotifyPopularityCacheKeyPrefix}::{date.Date:yyyy-MM-dd}".FreverUnversionedCache();
    }

    private async Task RefreshSpotifyPopularity()
    {
        if (!NeedToRun())
            return;

        try
        {
            using var scope = _services.CreateScope();

            var spotifyService = scope.ServiceProvider.GetRequiredService<ISpotifyPopularityService>();

            _log.LogInformation("Start refreshing Spotify popularity score");
            await spotifyService.RefreshSpotifyPopularity();

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

    private void RefreshSpotifyPopularityWorkingFunction(object _)
    {
        while (true)
        {
            if (!_isRunning)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                continue;
            }

            using var scope = _log.BeginScope("[{wid}:{tick}]:RefreshSpotifyPop: ", _wid.ToString("N"), Guid.NewGuid().ToString("N"));

            RefreshSpotifyPopularity().Wait();

            scope.Dispose();

            var randomDelay = TimeSpan.FromSeconds(Rnd.Next(0, 100)); // Random delay to distribute loading across instances
            Thread.Sleep(Debugger.IsAttached ? TimeSpan.FromSeconds(5) : CheckInterval + randomDelay);
        }
    }

    private bool NeedToRun()
    {
        if (Debugger.IsAttached)
            return true;

        if (_redis.KeyExists(RefreshSpotifyPopularityAtDateCacheKey(DateTime.UtcNow)))
        {
            _log.LogDebug("Spotify popularity were loaded today");
            return false;
        }

        if (!_redis.LockTake(SpotifyPopularityLockCacheKey, true, TimeSpan.FromMinutes(120)))
        {
            _log.LogDebug("Spotify popularity are loading in other instance");
            return false;
        }

        return true;
    }

    private void SetNeedToRun(bool needToRun)
    {
        if (needToRun)
        {
            _redis.LockRelease(SpotifyPopularityLockCacheKey, true);
        }
        else
        {
            _redis.StringSet(RefreshSpotifyPopularityAtDateCacheKey(DateTime.UtcNow), "y");
            _redis.LockRelease(SpotifyPopularityLockCacheKey, true);
            _log.LogInformation("Spotify popularity CSV were processed successfully");
        }
    }

    #endregion
}