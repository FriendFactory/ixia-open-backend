using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Infrastructure;

public abstract class BackgroundJobBase : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _runningCacheKey;
    private Timer _timer;

    protected BackgroundJobBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _runningCacheKey = $"frever::background-jobs::{GetType().Name}";
    }

    protected abstract TimeSpan RunInterval { get; }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, RunInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private async void DoWork(object _)
    {
        using var scope = _serviceProvider.CreateScope();

        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger($"Frever.BackgroundJobs.{GetType().Name}");

        log.LogInformation("Start to run job: {Name}", GetType().Name);

        var startTime = Stopwatch.GetTimestamp();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var runCount = db.StringIncrement(_runningCacheKey);

        if (runCount > 1)
        {
            log.LogInformation("Job: {Name} already run on another instance", GetType().Name);
            return;
        }

        var expireTimeout = RunInterval / 2;
        db.KeyExpire(_runningCacheKey, expireTimeout);

        await Run(scope);
        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        log.LogInformation("Finish running job: {Name}, took {ElapsedTime}", GetType().Name, elapsedTime);
    }

    protected abstract Task Run(IServiceScope scope);

    public void Dispose()
    {
        _timer?.Dispose();
    }
}