using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Throttling;

public class RpsThrottler
{
    private const int MaxTries = 30;

    public static readonly string AwsMediaConvertApiThrottle = "frever::throttle::aws::media-convert";

    private readonly IDatabase _cache;
    private readonly ILogger _logger;

    public RpsThrottler(IConnectionMultiplexer redis, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _cache = redis.GetDatabase();
        _logger = loggerFactory.CreateLogger("Frever.RpsThrottler");
    }


    public async Task<TResult> Throttle<TResult>(
        string key,
        int numberOfRequests,
        TimeSpan interval,
        Func<Task<TResult>> request,
        string requestDiagnosticsName
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        if (numberOfRequests <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfRequests));

        using var scope = _logger.BeginScope("Request [{rid}] {n}: ", Guid.NewGuid().ToString("N"), requestDiagnosticsName ?? "-");

        _logger.LogDebug("Request started");

        for (var i = 0; i < MaxTries; i++)
        {
            _logger.LogDebug("Tryout #{t}", i);

            var usedPerInterval = (int) _cache.StringGet(key);

            _logger.LogDebug("Request have {n} usages per {interval}", usedPerInterval, interval);

            if (usedPerInterval > numberOfRequests)
            {
                _logger.LogDebug("Usage {n} is above quote {q}, waiting", usedPerInterval, numberOfRequests);
                await Task.Delay(100);
                continue;
            }

            try
            {
                var result = await request();
                _logger.LogInformation("Request complete");
                _cache.StringIncrement(key);
                _cache.KeyExpire(key, interval + TimeSpan.FromMilliseconds(100));
                return result;
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name.Contains("TooManyRequestsException", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Quote hit, trying to wait");
                    await Task.Delay(300);
                }
                else
                {
                    _logger.LogError(ex, "Error due executing request");
                    throw;
                }
            }
        }

        _logger.LogError("Request weren't executed in {max} tryouts", MaxTries);
        throw new InvalidOperationException($"Max tryouts={MaxTries} reached");
    }

    public async Task<(bool, TResult)> TryThrottle<TResult>(string key, int requestPerSecond, Func<Task<TResult>> request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        if (requestPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestPerSecond));

        var usedPerSecond = _cache.StringIncrement(key);
        if (usedPerSecond == 1)
            _cache.KeyExpire(key, TimeSpan.FromSeconds(1));

        if (usedPerSecond > requestPerSecond)
        {
            await Task.Delay(100);
            return (false, default);
        }

        return (true, await request());
    }
}

public static class AwsRpsThrottlerExtensions
{
    public static Task<TResult> ThrottleAwsMediaConvert<TResult>(this RpsThrottler throttler, Func<Task<TResult>> request)
    {
        if (throttler == null)
            throw new ArgumentNullException(nameof(throttler));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return throttler.Throttle(
            RpsThrottler.AwsMediaConvertApiThrottle,
            1,
            TimeSpan.FromSeconds(2),
            request,
            "AwsMediaConvert"
        );
    }
}