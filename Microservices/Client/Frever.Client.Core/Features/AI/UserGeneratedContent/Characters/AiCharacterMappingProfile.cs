using AutoMapper;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;

// ReSharper disable once UnusedType.Global
public class AiCharacterMappingProfile : Profile
{
    public AiCharacterMappingProfile()
    {
        CreateMap<AiCharacterInput, AiCharacter>();
        CreateMap<AiCharacterImageInput, AiCharacterImage>();
        CreateMap<AiCharacter, AiCharacterDto>();
        CreateMap<AiCharacterImage, AiCharacterImageDto>();
    }
}