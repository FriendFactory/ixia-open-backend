using System;
using System.Threading.Tasks;
using AuthServer.TokenGeneration.Contract;
using Common.Infrastructure.Caching;
using Frever.Cache.PubSub;
using Frever.Protobuf;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthServer.Services.TokenGeneration;

public class TokenGenerationRpcListener : IPubSubSubscriber
{
    private readonly ICache _cache;
    private readonly ILogger _log;
    private readonly IPubSubPublisher _publisher;
    private readonly ITokenGenerationService _tokenGenerationService;

    public TokenGenerationRpcListener(
        ITokenGenerationService tokenGenerationService,
        IPubSubPublisher publisher,
        ICache cache,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _tokenGenerationService = tokenGenerationService ?? throw new ArgumentNullException(nameof(tokenGenerationService));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _cache = cache;
        _log = loggerFactory.CreateLogger("Frever.TokenGenerationRpcListener");
    }

    public string SubscriptionKey => TokenGenerationCacheKeys.RequestChannel;

    public async Task OnMessage(RedisValue message)
    {
        var request = ProtobufConvert.DeserializeObject<TokenGenerationRequestMessage>(message);

        _log.LogDebug("Received token generation request {CorrelationId}", request.CorrelationId);

        if (!await _cache.Db()
                         .LockTakeAsync(TokenGenerationCacheKeys.LockCorrelationKey(request.CorrelationId), true, TimeSpan.FromMinutes(5)))
        {
            _log.LogDebug("Request {CorrelationId} is processing by another instance", request.CorrelationId);
            return;
        }

        try
        {
            var token = await _tokenGenerationService.GenerateByGroupId(request.GroupId);

            await _publisher.Publish(
                TokenGenerationCacheKeys.ResponseChannel,
                new TokenGenerationResponseMessage {CorrelationId = request.CorrelationId, Jwt = token, Ok = true}
            );

            _log.LogDebug("Token generated and response send for request {CorrelationId}", request.CorrelationId);
        }
        catch (Exception ex)
        {
            await _publisher.Publish(
                TokenGenerationCacheKeys.ResponseChannel,
                new TokenGenerationResponseMessage {CorrelationId = request.CorrelationId, Ok = false, ErrorMessage = ex.Message}
            );

            _log.LogError(ex, "Token generation exception {CorrelationId}", request.CorrelationId);
        }
    }
}