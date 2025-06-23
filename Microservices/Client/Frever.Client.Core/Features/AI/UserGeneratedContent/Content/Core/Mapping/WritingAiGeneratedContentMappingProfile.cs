using AutoMapper;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core.Mapping;

// ReSharper disable once UnusedType.Global
public class WritingAiGeneratedContentMappingProfile : Profile
{
    public WritingAiGeneratedContentMappingProfile()
    {
        CreateMap<AiGeneratedContentInput, AiGeneratedContent>()
           .ForMember(
                m => m.Type,
                opt => opt.MapFrom(src => src.Image != null ? AiGeneratedContent.KnownTypeImage : AiGeneratedContent.KnownTypeVideo)
            );

        CreateMap<AiGeneratedImageInput, AiGeneratedImage>().MapFileMetadata();
        CreateMap<AiGeneratedImagePersonInput, AiGeneratedImagePerson>().MapFileMetadata();
        CreateMap<AiGeneratedImageSourceInput, AiGeneratedImageSource>().MapFileMetadata();

        CreateMap<AiGeneratedVideoInput, AiGeneratedVideo>().MapFileMetadata();
        CreateMap<AiGeneratedVideoClipInput, AiGeneratedVideoClip>().MapFileMetadata();
    }
}