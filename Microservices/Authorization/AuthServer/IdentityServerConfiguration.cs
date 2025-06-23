using FluentValidation;

namespace AuthServer;

public class IdentityServerConfiguration
{
    public string ClientSecret { get; set; }

    public string IssuerUrl { get; set; }

    public string CertificateContentBase64 { get; set; }

    public string CertificatePassword { get; set; }

    public string AllowedRedirectUrls { get; set; }

    public string Scopes => "friends_factory.creators_api offline_access";

    public string ClientId { get; set; }


    public void Validate()
    {
        var validator = new InlineValidator<IdentityServerConfiguration>();
        validator.RuleFor(e => e.ClientSecret).NotEmpty().MinimumLength(5);
        validator.RuleFor(e => e.IssuerUrl).NotEmpty().MinimumLength(5);
        validator.RuleFor(e => e.CertificateContentBase64).NotEmpty().MinimumLength(5);
        validator.RuleFor(e => e.AllowedRedirectUrls).NotEmpty().MinimumLength(5);
        validator.RuleFor(e => e.CertificatePassword).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}