using AutoMapper;
using Frever.ClientService.Contract.Locales;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Localizations;

// ReSharper disable once UnusedType.Global
public class LocaleMappingProfile : Profile
{
    public LocaleMappingProfile()
    {
        CreateMap<Country, CountryDto>()
           .ForMember(d => d.Iso2Code, d => d.MapFrom(s => s.ISO2Code))
           .ForMember(d => d.Iso3Code, d => d.MapFrom(s => s.ISOName));

        CreateMap<Language, LanguageDto>().ForMember(d => d.Iso2Code, d => d.MapFrom(s => s.ISO2Code));
    }
}