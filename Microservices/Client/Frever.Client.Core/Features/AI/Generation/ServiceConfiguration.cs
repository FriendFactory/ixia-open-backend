using System;
using Frever.Client.Core.Features.AI.Generation.StatusUpdating;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.AI.PixVerse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AI.Generation;

public static class ServiceConfiguration
{
    public static void AddAiGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddPixVerseProxy(configuration);

        services.AddScoped<IGenerationRepository, GenerationRepository>();
        services.AddScoped<ISoundService, SoundService>();
        services.AddScoped<IAiGenerationService, AiGenerationService>();

        services.AddScoped<IPollingIntervalStrategy, PollingIntervalStrategy>();
        services.AddScoped<IPollingJobManager, RedisPollingJobManager>();
        services.AddScoped<IGeneratedContentUploadingRepository, GeneratedContentUploadingRepository>();
        services.AddScoped<IAiGeneratedContentUploadingService, AiGeneratedContentUploadingService>();
        services.AddScoped<IComfyUiMessageHandlingService, AiGeneratedContentUploadingService>();

        services.AddHostedService<AiContentGenerationPollingService>();

        var settings = new ComfyUiApiSettings();
        configuration.Bind("ComfyUiApiSettings", settings);
        settings.Validate();
        services.AddSingleton(settings);

        services.AddAWSMessageBus(builder =>
                                  {
                                      builder.AddSQSPoller(
                                          settings.ResponseQueueUrl,
                                          options =>
                                          {
                                              options.MaxNumberOfConcurrentMessages = 10;
                                              options.WaitTimeSeconds = 20;
                                          }
                                      );
                                      builder.AddMessageHandler<ComfyUiResultMessageHandler, ComfyUiResult>();
                                      builder.AddMessageHandler<ComfyUiErrorMessageHandler, ComfyUiError>();
                                  }
        );
    }
}