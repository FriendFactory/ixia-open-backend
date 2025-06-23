using System;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Metadata;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AI.Metadata;

public static class ServiceConfiguration
{
    public static void AddAiMetadata(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<ArtStyleFileConfig>();
        services.AddEntityFileConfiguration<MakeUpFileConfig>();
        services.AddEntityFileConfiguration<LanguageModeFileConfig>();
        services.AddEntityFileConfiguration<SpeakerModeFileConfig>();

        services.AddScoped<IAiMetadataRepository, AiMetadataRepository>();
        services.AddScoped<IAiMetadataService, AiMetadataService>();
        services.AddScoped<IOpenAiMetadataService, OpenAiMetadataService>();

        services.AddFreverCaching(
            options =>
            {
                options.InMemory.Blob<AiArtStyle[]>(SerializeAs.Protobuf, false, typeof(AiArtStyle));
                options.InMemory.Blob<AiLlmPrompt[]>(SerializeAs.Protobuf, false, typeof(AiLlmPrompt));
                options.InMemory.Blob<AiSpeakerMode[]>(SerializeAs.Protobuf, false, typeof(AiSpeakerMode));
                options.InMemory.Blob<AiLanguageMode[]>(SerializeAs.Protobuf, false, typeof(AiLanguageMode));
                options.InMemory.Blob<GenreDto[]>(SerializeAs.Protobuf, false, typeof(Genre));
                options.InMemory.Blob<GenderDto[]>(SerializeAs.Protobuf, false, typeof(Gender));
                options.InMemory.Blob<AiMakeUp[]>(SerializeAs.Protobuf, false, typeof(AiMakeUp));
            }
        );
    }
}

public class LanguageModeFileConfig : DefaultFileMetadataConfiguration<AiLanguageMode>
{
    public LanguageModeFileConfig()
    {
        AddMainFile("mp3", true);
    }
}

public class SpeakerModeFileConfig : DefaultFileMetadataConfiguration<AiSpeakerMode>
{
    public SpeakerModeFileConfig()
    {
        AddMainFile("mp3", true);
    }
}

public class ArtStyleFileConfig : DefaultFileMetadataConfiguration<AiArtStyle>
{
    public ArtStyleFileConfig()
    {
        AddThumbnail(512, "jpg");
    }
}

public class MakeUpFileConfig : DefaultFileMetadataConfiguration<AiMakeUp>
{
    public MakeUpFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
    }
}