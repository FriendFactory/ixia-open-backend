using FluentValidation;

namespace Frever.AdminService.Core.Services.GeoClusters;

public class GeoClusterValidator : AbstractValidator<GeoClusterDto>
{
    public GeoClusterValidator()
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.IncludeVideoFromCountry).NotNull();
        RuleFor(e => e.IncludeVideoWithLanguage).NotNull();
        RuleFor(e => e.ExcludeVideoFromCountry).NotNull();
        RuleFor(e => e.ExcludeVideoWithLanguage).NotNull();
        RuleFor(e => e.ShowForUserWithLanguage).NotNull();
        RuleFor(e => e.ShowToUserFromCountry).NotNull();
        RuleFor(e => e.HideForUserWithLanguage).NotNull();
        RuleFor(e => e.HideForUserFromCountry).NotNull();
    }
}