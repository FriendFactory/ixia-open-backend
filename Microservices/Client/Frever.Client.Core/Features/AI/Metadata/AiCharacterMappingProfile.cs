using AutoMapper;
using Frever.ClientService.Contract.Metadata;
using Frever.Shared.MainDb.Entities;
using Genre = Frever.Client.Core.Features.Sounds.Song.Models.Genre;

namespace Frever.Client.Core.Features.AI.Metadata;

// ReSharper disable once UnusedType.Global
public class AiMetadataMappingProfile : Profile
{
    public AiMetadataMappingProfile()
    {
        CreateMap<AiArtStyle, ArtStyleDto>();
        CreateMap<AiLanguageMode, LanguageModeDto>();
        CreateMap<AiSpeakerMode, SpeakerModeDto>();
        CreateMap<AiMakeUp, MakeUpDto>();
        CreateMap<AiMakeUpCategory, MakeUpCategoryDto>();
        CreateMap<Gender, GenderDto>();
        CreateMap<Genre, GenreDto>();
    }
}