using System;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Twilio;

namespace AuthServer.Services.SmsSender;

public static class Configuration
{
    public static void AddTwilio(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Twilio");

        if (section == null)
            throw new InvalidOperationException("Twilio settings is missing");

        var settings = new TwilioSmsSettings();

        section.Bind(settings);

        new TwilioSmsSettingsValidator().ValidateAndThrow(settings);

        TwilioClient.Init(settings.Sid, settings.Secret);

        services.AddSingleton(settings);
        services.AddSingleton<ISmsSender, TwilioSmsSender>();
    }
}