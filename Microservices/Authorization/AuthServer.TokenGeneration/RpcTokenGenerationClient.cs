using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.TokenGeneration.Contract;
using Frever.Cache.PubSub;
using Frever.Protobuf;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthServer.TokenGeneration;

internal class RpcTokenGenerationClient : ITokenGenerationClient, IPubSubSubscriber, IDisposable
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

    private readonly Thread _cleanUpTimedOut;
    private readonly ILogger _log;

    private readonly IPubSubPublisher _pubSubPublisher;

    private readonly ConcurrentDictionary<Guid, TokenGenerationAwaitInfo> _waitingCalls = new();

    private bool _stopCleanUp;

    public RpcTokenGenerationClient(IPubSubPublisher pubSubPublisher, ILoggerFactory loggerFactory)
    {
        _pubSubPublisher = pubSubPublisher ?? throw new ArgumentNullException(nameof(pubSubPublisher));
        _log = loggerFactory.CreateLogger("Frever.TokenGenerationClient");
        _cleanUpTimedOut = new Thread(DoCleanUp);
        _cleanUpTimedOut.Start();
    }

    public void Dispose()
    {
        _log.LogDebug("Disposing token generation client");

        _stopCleanUp = true;
        _cleanUpTimedOut.Join(WaitTimeout);

        var old = _waitingCalls.ToArray();

        foreach (var kvp in old)
            if (_waitingCalls.TryRemove(kvp.Key, out var awaiter))
            {
                _log.LogDebug("Cleaning up awaiting request on disposing: {cid}", kvp.Key);
                awaiter.Completion.SetResult(new TokenGenerationResult {Ok = false, TimedOut = true, ErrorMessage = "Timed out"});
            }
    }

    public string SubscriptionKey => TokenGenerationCacheKeys.ResponseChannel;

    public Task OnMessage(RedisValue message)
    {
        var response = ProtobufConvert.DeserializeObject<TokenGenerationResponseMessage>(message);

        _log.LogDebug("Token generation response message received: {cid}", response.CorrelationId);

        if (_waitingCalls.TryRemove(response.CorrelationId, out var awaitInfo))
        {
            _log.LogDebug("Awaiting request found for {cid}", response.CorrelationId);

            var result = new TokenGenerationResult {Jwt = response.Jwt, Ok = response.Ok, ErrorMessage = response.ErrorMessage};
            awaitInfo.Completion.SetResult(result);
        }

        return Task.CompletedTask;
    }

    public async Task<TokenGenerationResult> GenerateTokenByGroupId(long groupId)
    {
        var request = new TokenGenerationRequestMessage {GroupId = groupId, CorrelationId = Guid.NewGuid()};

        var awaitInfo = new TokenGenerationAwaitInfo();
        _waitingCalls.AddOrUpdate(request.CorrelationId, _ => awaitInfo, (id, value) => value);

        _log.LogDebug("Awaiting request added for {cid}", request.CorrelationId);

        await _pubSubPublisher.Publish(TokenGenerationCacheKeys.RequestChannel, request);

        return await awaitInfo.Completion.Task;
    }

    private void DoCleanUp()
    {
        while (!_stopCleanUp)
        {
            var now = DateTime.Now;
            var old = _waitingCalls.Where(k => now - k.Value.StartedAt > WaitTimeout).ToArray();

            foreach (var kvp in old)
                if (_waitingCalls.TryRemove(kvp.Key, out var awaiter))
                {
                    _log.LogDebug("Cleaning up timed out awaiting request: {cid}", kvp.Key);
                    awaiter.Completion.SetResult(new TokenGenerationResult {Ok = false, TimedOut = true, ErrorMessage = "Timed out"});
                }

            Thread.Sleep(WaitTimeout / 2);
        }
    }

    private class TokenGenerationAwaitInfo
    {
        public TokenGenerationAwaitInfo()
        {
            StartedAt = DateTime.Now;
            Completion = new TaskCompletionSource<TokenGenerationResult>();
        }

        public DateTime StartedAt { get; }
        public TaskCompletionSource<TokenGenerationResult> Completion { get; }
    }
}