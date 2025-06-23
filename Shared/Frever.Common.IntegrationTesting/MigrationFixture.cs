using Common.Infrastructure.Database;
using Frever.Common.IntegrationTesting.Migrations;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Frever.Common.IntegrationTesting;

// ReSharper disable once ClassNeverInstantiated.Global
public class MigrationFixture
{
    public MigrationFixture()
    {
        var testOut = new Mock<ITestOutputHelper>();

        var services = new ServiceCollection();
        services.AddIntegrationTests(testOut.Object);

        {
            using var provider = services.BuildServiceProvider();

            var migrator = provider.GetRequiredService<IMigrator>();
            migrator.Migrate().Wait();
        }


        {
            using var provider = services.BuildServiceProvider();
            var dataEnv = provider.GetRequiredService<DataEnvironment>();
            dataEnv.ApplyScript("drop-all-foreign-keys").Wait();
        }

        ApplyTestMigrations();
    }

    private void ApplyTestMigrations()
    {
        var configuration = IntegrationTestServiceConfiguration.GetConfiguration();
        var dbConnectionConfig = configuration.GetDbConnectionConfiguration();
        var services = new ServiceCollection();

        var logs = new List<string>();
        services.AddIntegrationTestMigrations(dbConnectionConfig.MainDbWritable, logs);
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        var testMigrationDb = scope.ServiceProvider.GetRequiredService<IntegrationTestMigrationDbContext>();
        if (testMigrationDb.Database.CanConnect())
            testMigrationDb.Database.Migrate();
    }
}