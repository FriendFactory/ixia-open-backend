using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Common.Infrastructure.Caching.CacheKeys;
using FluentValidation;
using Frever.Cache;
using Frever.Cache.Throttling;
using Frever.Video.Core.Features.Caching;
using Frever.Video.Core.Features.MediaConversion.DataAccess;
using Frever.Video.Core.Features.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService;
using StackExchange.Redis;

#pragma warning disable CS8618, CS80600, CS8603, CS8600

namespace Frever.Video.Core.Features.MediaConversion.StatusUpdating;

/// <summary>
///     Updates status of video being converted by polling list of AWS media convert jobs.
///     Service consists of two parts:
///     - first maintains a snapshot of media convert jobs
///     - second gets a list of non-converted videos and check status against job queue
///     First part works by requesting a lists of jobs from API and update an Redis hash with job status
///     Second part works by getting list of recent* videos from DB and check against snapshot of job queue.
///     *recent is configurable, see Recent field for exact value.
/// </summary>
public partial class JobPollingVideoConversionStatusUpdater : IHostedService
{
    private static readonly TimeSpan Recent = TimeSpan.FromMinutes(20);
    private readonly IDictionaryCache<long, AwsMediaConvertJobCacheInfo> _awsConversionSnapshot;
    private readonly IAmazonMediaConvert _awsMediaConvert;
    private readonly JobPollingStatusUpdaterConfiguration _config;
    private readonly ILogger _logger;
    private readonly IDatabase _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly RpsThrottler _throttle;

    private volatile bool _isStarted;
    private Thread _refreshJobSnapshotWorker;
    private Thread _refreshVideoStatusesWorker;


    public JobPollingVideoConversionStatusUpdater(
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        JobPollingStatusUpdaterConfiguration config,
        IAmazonMediaConvert awsMediaConvert,
        IDictionaryCache<long, AwsMediaConvertJobCacheInfo> awsConversionSnapshot,
        IConnectionMultiplexer redisConnection,
        RpsThrottler throttle
    )
    {
        ArgumentNullException.ThrowIfNull(redisConnection);

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _awsMediaConvert = awsMediaConvert ?? throw new ArgumentNullException(nameof(awsMediaConvert));
        _awsConversionSnapshot = awsConversionSnapshot ?? throw new ArgumentNullException(nameof(awsConversionSnapshot));
        _throttle = throttle ?? throw new ArgumentNullException(nameof(throttle));

        _logger = loggerFactory.CreateLogger("Frever.JobPollingVideoConversion");
        _redis = redisConnection.GetDatabase();

        _config.Validate();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobPollingVideoConversionStatusUpdater.Start");
        _isStarted = true;

        ConfigureAwsMediaConvertService().Wait();

        _refreshJobSnapshotWorker = new Thread(
            _ =>
            {
                while (_isStarted)
                {
                    using var logScope = _logger.BeginScope("[{guid}] RefreshAwsJobSnapshot: ", Guid.NewGuid().ToString("N"));
                    try
                    {
                        RefreshAwsConversionJobSnapshotOnce().Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing AWS MediaConvert Job snapshot");
                        Thread.Sleep(1000);
                    }

                    Thread.Sleep(2000);
                }
            }
        );
        _refreshVideoStatusesWorker = new Thread(
            _ =>
            {
                while (_isStarted)
                {
                    using var logScope = _logger.BeginScope("[{guid}] RefreshVideoStatuses: ", Guid.NewGuid().ToString("N"));

                    try
                    {
                        RefreshVideoStatusesOnce().Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing video statuses");
                    }

                    Thread.Sleep(2000);
                }
            }
        );

        _refreshJobSnapshotWorker.Start();
        _refreshVideoStatusesWorker.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isStarted = false;
        _refreshJobSnapshotWorker?.Join(3000);
        _refreshVideoStatusesWorker?.Join(3000);

        _logger.LogInformation("JobPollingVideoConversionStatusUpdater.Stop");

        return Task.CompletedTask;
    }

    private async Task ConfigureAwsMediaConvertService()
    {
        var endpoints = await _awsMediaConvert.DescribeEndpointsAsync(new DescribeEndpointsRequest());
        var endpoint = endpoints.Endpoints[0].Url;
        ((AmazonMediaConvertConfig) _awsMediaConvert.Config).ServiceURL = endpoint;
    }

    private static IVideoConversionStatusUpdateService CreateStatusUpdateService(IServiceScope scope)
    {
        var updater = new VideoConversionStatusUpdateService(
            scope.ServiceProvider.GetRequiredService<IVideoStatusUpdateRepository>(),
            scope.ServiceProvider.GetRequiredService<INotificationAddingService>(),
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>(),
            scope.ServiceProvider.GetRequiredService<IVideoCachingService>()
        );

        return updater;
    }

    private Task<bool> IsVideoProcessingActive()
    {
        return Task.FromResult(!_redis.KeyExists(VideoCacheKeys.VideoJobWatchingSuspendedKey()));
    }

    private Task StopVideoProcessing()
    {
        _logger.LogInformation("Video status watching suspend");
        _redis.StringSet(VideoCacheKeys.VideoJobWatchingSuspendedKey(), true, TimeSpan.FromMinutes(60));
        return Task.CompletedTask;
    }

    private Task StartVideoProcessing()
    {
        _logger.LogInformation("Video status watching resumed");
        _redis.KeyDelete(VideoCacheKeys.VideoJobWatchingSuspendedKey());
        return Task.CompletedTask;
    }
}

public class JobPollingStatusUpdaterConfiguration
{
    public string MediaConvertQueue { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<JobPollingStatusUpdaterConfiguration>();
        validator.RuleFor(e => e.MediaConvertQueue).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}