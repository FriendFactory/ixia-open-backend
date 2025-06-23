using AutoMapper;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core.Mapping;

// ReSharper disable once UnusedType.Global
public class ReadingAiGeneratedContentMappingProfile : Profile
{
    public ReadingAiGeneratedContentMappingProfile()
    {
        CreateMap<AiGeneratedVideo, AiGeneratedVideoFullInfo>();
        CreateMap<AiGeneratedVideoClip, AiGeneratedVideoClipFullInfo>();

        CreateMap<AiGeneratedContent, AiGeneratedContentFullInfo>();
        CreateMap<AiGeneratedImage, AiGeneratedImageFullInfo>();
        CreateMap<AiGeneratedImagePerson, AiGeneratedImagePersonFullInfo>();
        CreateMap<AiGeneratedImageSource, AiGeneratedImageSourceFullInfo>();
    }
}