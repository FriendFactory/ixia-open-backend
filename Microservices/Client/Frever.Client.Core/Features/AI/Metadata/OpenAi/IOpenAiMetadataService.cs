using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.Metadata;

public interface IOpenAiMetadataService
{
    Task<string> GetRandomOpenAiApiKey();

    Task<AiOpenAiAgent> GetOpenAiAgent(string key);
}

public class OpenAiMetadataService(IWriteDb db) : IOpenAiMetadataService
{
    private readonly Lazy<string> _openAiKey = new(
        () => db.AiOpenAiKey.ToArrayAsync().Result.RandomElement().ApiKey,
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    public Task<string> GetRandomOpenAiApiKey()
    {
        return Task.FromResult(_openAiKey.Value);
    }

    public async Task<AiOpenAiAgent> GetOpenAiAgent(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var agents = await db.AiOpenAiAgent.Where(a => a.Key == key).ToArrayAsync();
        if (!agents.Any())
            return null;

        var applicable = agents.RandomElement();
        return applicable;
    }
}