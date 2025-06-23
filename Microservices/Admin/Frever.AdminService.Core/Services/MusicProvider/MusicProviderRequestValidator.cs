using System;
using Common.Infrastructure.MusicProvider;
using FluentValidation;

namespace Frever.AdminService.Core.Services.MusicProvider;

public class MusicProviderRequestValidator : AbstractValidator<MusicProviderRequest>
{
    public MusicProviderRequestValidator()
    {
        RuleFor(e => e.BaseUrl).Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var result) && result.Scheme == Uri.UriSchemeHttps);
        RuleFor(e => e.QueryParameters).Must(e => e != null && e.Count != 0);
        RuleFor(e => e.HttpMethod).NotEmpty().Must(e => Enum.TryParse<MusicProviderHttpMethod>(e, true, out _));
        RuleFor(e => e.Body)
           .Must(e => !string.IsNullOrEmpty(e))
           .When(e => Enum.TryParse<MusicProviderHttpMethod>(e.HttpMethod, true, out var result) && result == MusicProviderHttpMethod.Post);
    }
}