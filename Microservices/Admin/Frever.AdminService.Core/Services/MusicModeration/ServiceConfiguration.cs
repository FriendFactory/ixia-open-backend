using System;
using FluentValidation;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.AdminService.Core.Services.MusicModeration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.MusicModeration;

public static class ServiceConfiguration
{
    public static void AddMusicModeration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMusicIsrcService, SevenDigitalMusicIsrcService>();
        services.AddScoped<IMusicDeleteService, MusicDeleteService>();
        services.AddScoped<ISoundModerationService, SoundModerationService>();
        services.AddScoped<ISoundMetadataService, SoundMetadataService>();

        services.AddScoped<IValidator<SongDto>, SongValidator>();
        services.AddScoped<IValidator<UserSoundDto>, UserSoundValidator>();
        services.AddScoped<IValidator<PromotedSongDto>, PromotedSongValidator>();
    }
}