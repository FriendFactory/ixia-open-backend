using System;
using System.Linq;
using Common.Infrastructure.MusicProvider;
using FluentValidation;

namespace Frever.Client.Core.Features.CommercialMusic;

public class SignUrlRequestValidator : AbstractValidator<SignUrlRequest>
{
    private static readonly string[] AllowedUrls =
    [
        "https://api.7digital.com/1.2/track/details",
        "https://api.7digital.com/1.2/playlists/",
        "https://api.7digital.com/1.2/track/details/batch",
        "https://api.7digital.com/1.2/track/search",
        "https://previews.7digital.com/clip/"
    ];

    private static readonly MusicProviderHttpMethod[] AllowedHttpMethods = {MusicProviderHttpMethod.Get};

    public SignUrlRequestValidator()
    {
        RuleFor(e => e.HttpMethod).Must(e => AllowedHttpMethods.Any(n => n.ToString("G").ToLower() == e.ToLower()));
        RuleFor(e => e.BaseUrl)
           .NotNull()
           .NotEmpty()
           .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var result) && result.Scheme == Uri.UriSchemeHttps)
           .Must(uri => { return AllowedUrls.Any(auri => uri.StartsWith(auri, StringComparison.OrdinalIgnoreCase)); });

        RuleFor(e => e.QueryParameters).Must(parameters => parameters != null);
    }
}