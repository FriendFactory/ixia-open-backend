using System;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServerShared;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Features.CredentialUpdate;

public static class ServiceConfiguration
{
    public static void AddCredentialUpdateService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddUserInfo();

        services.AddScoped<ICredentialHandler, AppleIdHandler>();
        services.AddScoped<ICredentialHandler, GoogleIdHandler>();
        services.AddScoped<ICredentialHandler, EmailHandler>();
        services.AddScoped<ICredentialHandler, PhoneNumberHandler>();
        services.AddScoped<ICredentialHandler, PasswordHandler>();

        services.AddScoped<ICredentialUpdateService, CredentialUpdateService>();
        services.AddScoped<ITokenProvider, TokenProvider>();

        services.AddScoped<IValidator<VerifyUserRequest>, VerifyUserRequestValidator>();
        services.AddScoped<IValidator<AddCredentialsRequest>, AddCredentialsRequestValidator>();
        services.AddScoped<IValidator<UpdateCredentialsRequest>, UpdateCredentialsRequestValidator>();
    }
}