using System;
using Amazon.SimpleEmail;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.EmailSending;

public static class EmailExtensions
{
    public static void AddEmailSending(this IServiceCollection services, Action<EmailConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddAWSService<IAmazonSimpleEmailService>();
        services.AddSingleton<IEmailSendingService, AwsSesEmailSendingService>();

        var emailConfiguration = new EmailConfiguration();
        configure(emailConfiguration);

        new EmailConfigurationValidator().ValidateAndThrow(emailConfiguration);

        services.AddSingleton(emailConfiguration);
    }

    public static void AddEmailSending(this IServiceCollection services, IConfiguration configuration)
    {
        var emailSection = configuration.GetSection("EmailSending");

        if (emailSection == null)
            throw new ArgumentException("Required configuration section EmailSending is missing");

        services.AddEmailSending(config => emailSection.Bind(config));
    }
}