using System;
using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Shared.MainDb;

public static class ServiceConfiguration
{
    public static void AddFreverDataWritingAccess(
        this IServiceCollection services,
        DatabaseConnectionConfiguration dbConnectionConfiguration
    )
    {
        ArgumentNullException.ThrowIfNull(dbConnectionConfiguration);
        MainDbContext.RegisterGlobalTypes();


        services.AddEntityFrameworkNpgsql()
                .AddDbContext<WriteDbContext>(
                     options =>
                     {
                         options.UseNpgsql(
                             dbConnectionConfiguration.MainDbWritable,
                             optionBuilder =>
                             {
                                 optionBuilder.CommandTimeout(60);
                                 optionBuilder.UseNetTopologySuite();
                             }
                         );
                         options.EnableSensitiveDataLogging();
                     }
                 );

        services.AddScoped<IWriteDb, WriteDbContext>();
    }

    public static void AddFreverCachedDataAccess(
        this IServiceCollection services,
        DatabaseConnectionConfiguration dbConnectionConfiguration
    )
    {
        ArgumentNullException.ThrowIfNull(dbConnectionConfiguration);

        MainDbContext.RegisterGlobalTypes();

        services.AddEntityFrameworkNpgsql()
                .AddDbContext<ReadDbContext>(
                     options =>
                     {
                         options.UseNpgsql(
                             dbConnectionConfiguration.MainDbReadReplica,
                             optionBuilder =>
                             {
                                 optionBuilder.CommandTimeout(60);
                                 optionBuilder.UseNetTopologySuite();
                             }
                         );
                         options.EnableSensitiveDataLogging();
                     }
                 );

        services.AddScoped<IReadDb, ReadDbContext>();
    }

    public static void AddFreverDatabaseMigrations(
        this IServiceCollection services,
        DatabaseConnectionConfiguration dbConnectionConfiguration
    )
    {
        ArgumentNullException.ThrowIfNull(dbConnectionConfiguration);
        ArgumentNullException.ThrowIfNull(services);

        services.AddFreverDataWritingAccess(dbConnectionConfiguration);
        services.AddScoped<IMigrator, WriteDbContext>();
    }
}