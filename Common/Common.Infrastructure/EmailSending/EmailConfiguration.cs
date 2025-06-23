using FluentValidation;

namespace Common.Infrastructure.EmailSending;

public class EmailConfiguration
{
    public string FromEmail { get; set; }
}

public class EmailConfigurationValidator : AbstractValidator<EmailConfiguration>
{
    public EmailConfigurationValidator()
    {
        RuleFor(e => e.FromEmail).NotEmpty().Length(1, 1024);
    }
}