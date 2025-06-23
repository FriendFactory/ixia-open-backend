using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Common.IntegrationTesting.Migrations;

public class IntegrationTestMigrationDbContext(DbContextOptions<IntegrationTestMigrationDbContext> options) : DbContext(options) { }

public static class IntegrationTestMigrationServiceConfiguration
{
    public static void AddIntegrationTestMigrations(this IServiceCollection services, string connectionString, List<string> logs)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

        services.AddEntityFrameworkNpgsql()
                .AddDbContext<IntegrationTestMigrationDbContext>(
                     options =>
                     {
                         options.UseNpgsql(
                             connectionString,
                             optionBuilder =>
                             {
                                 optionBuilder.CommandTimeout(60 * 10);
                                 optionBuilder.UseNetTopologySuite();
                             }
                         );
                         options.LogTo(message => logs.Add(message));
                         options.EnableSensitiveDataLogging();
                     }
                 );
    }
}