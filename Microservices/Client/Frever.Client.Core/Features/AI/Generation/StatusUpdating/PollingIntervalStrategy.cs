using System;
using System.Collections.Generic;

namespace Frever.Client.Core.Features.AI.Generation.StatusUpdating;

public class ContentTypePollingConfig
{
    public TimeSpan InitialInterval { get; set; }
    public TimeSpan MaxInterval { get; set; }
    public double BackoffMultiplier { get; set; }
    public TimeSpan JitterMaximum { get; set; }
    public TimeSpan AbsoluteTimeout { get; set; }
}

public interface IPollingIntervalStrategy
{
    TimeSpan CalculateNextInterval(PollingJob job, ContentTypePollingConfig config);
    bool IsTimeout(PollingJob job, ContentTypePollingConfig config);
    ContentTypePollingConfig GetConfigForContentType(string contentType);
}

public class PollingIntervalStrategy : IPollingIntervalStrategy
{
    private static readonly Dictionary<string, ContentTypePollingConfig> PollingConfigs = SetupPollingConfigs();
    private readonly Random _random = new();

    public TimeSpan CalculateNextInterval(PollingJob job, ContentTypePollingConfig config)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(config);

        var baseInterval = config.InitialInterval;

        for (var i = 0; i < job.PollAttempts; i++)
            baseInterval = TimeSpan.FromMilliseconds(baseInterval.TotalMilliseconds * config.BackoffMultiplier);

        if (baseInterval > config.MaxInterval)
            baseInterval = config.MaxInterval;

        if (config.JitterMaximum.TotalMilliseconds > 0)
        {
            var jitterMs = _random.Next(0, (int) config.JitterMaximum.TotalMilliseconds);
            baseInterval = baseInterval.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        return baseInterval;
    }

    public bool IsTimeout(PollingJob job, ContentTypePollingConfig config)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(config);

        var jobAge = DateTime.UtcNow - job.CreatedAt;

        return jobAge > config.AbsoluteTimeout;
    }

    public ContentTypePollingConfig GetConfigForContentType(string contentType)
    {
        if (PollingConfigs.TryGetValue(contentType, out var config))
            return config;
        throw new KeyNotFoundException($"Polling configuration not found for content type: {contentType}");
    }

    private static Dictionary<string, ContentTypePollingConfig> SetupPollingConfigs()
    {
        return new Dictionary<string, ContentTypePollingConfig>
               {
                   {
                       PollingJob.PixVerse,
                       new ContentTypePollingConfig
                       {
                           InitialInterval = TimeSpan.FromSeconds(3),
                           MaxInterval = TimeSpan.FromSeconds(5),
                           BackoffMultiplier = 1.1,
                           JitterMaximum = TimeSpan.FromMilliseconds(500),
                           AbsoluteTimeout = TimeSpan.FromMinutes(2)
                       }
                   },
                   {
                       PollingJob.Image,
                       new ContentTypePollingConfig
                       {
                           InitialInterval = TimeSpan.FromSeconds(2),
                           MaxInterval = TimeSpan.FromSeconds(5),
                           BackoffMultiplier = 1.1,
                           JitterMaximum = TimeSpan.FromSeconds(1),
                           AbsoluteTimeout = TimeSpan.FromMinutes(3)
                       }
                   },
                   {
                       PollingJob.Video,
                       new ContentTypePollingConfig
                       {
                           InitialInterval = TimeSpan.FromSeconds(3),
                           MaxInterval = TimeSpan.FromSeconds(5),
                           BackoffMultiplier = 1.2,
                           JitterMaximum = TimeSpan.FromMilliseconds(250),
                           AbsoluteTimeout = TimeSpan.FromMinutes(5)
                       }
                   }
               };
    }
}