using FluentValidation;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Client.Core.Features.Sounds.UserSounds.DataAccess;
using Frever.Client.Core.Features.Sounds.UserSounds.Trending;
using Frever.Client.Core.Features.Sounds.UserSounds.Validators;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.Sounds.UserSounds;

public static class ServiceConfiguration
{
    public static void AddUserSoundAsset(this IServiceCollection services)
    {
        services.AddScoped<IUserSoundAssetRepository, UserSoundAssetRepository>();
        services.AddScoped<IUserSoundAssetService, UserSoundAssetService>();
        services.AddScoped<IValidator<UserSoundCreateModel>, UserSoundValidator>();
        services.AddScoped<ITrendingUserSoundService, TrendingUserSoundService>();

        services.AddEntityFileConfiguration<UserSoundFilesConfig>();

        services.AddFreverCaching(
            options =>
            {
                options.InMemory.Blob<UserSoundFullInfo[]>(SerializeAs.Protobuf, false, typeof(UserSound));

                options.Redis.PagedList<UserSoundFullInfo>(
                    SerializeAs.Protobuf,
                    200,
                    200,
                    false,
                    [typeof(UserSound)]
                );
            }
        );
    }
}