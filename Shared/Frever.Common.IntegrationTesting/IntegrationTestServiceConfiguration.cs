using System.Data;
using Amazon.S3;
using AuthServer.Permissions;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Database;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.RequestId;
using Frever.Common.Testing;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit.Abstractions;

namespace Frever.Common.IntegrationTesting;

public static class IntegrationTestServiceConfiguration
{
    /// <summary>
    ///     This should be last call on service configuration to overwrite IReadDb/IWriteDb registration from AddXXX business
    ///     methods.
    /// </summary>
    public static void AddIntegrationTests(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(testOut);

        services.AddCommonTestServices(testOut);

        var configuration = GetConfiguration();
        var envInfo = configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var dbConnectionConfig = configuration.GetDbConnectionConfiguration();

        services.AddConfiguredAWSOptions(configuration);
        services.AddAWSService<IAmazonS3>();
        services.AddSingleton(dbConnectionConfig);
        services.AddFreverPermissions(dbConnectionConfig);
        services.AddRequestIdAccessor();

        AddEmailSending(services);

        MainDbContext.RegisterGlobalTypes();

        services.AddSingleton(
            provider =>
            {
                var opts = provider.GetRequiredService<DatabaseConnectionConfiguration>();
                var connection = new NpgsqlConnection(opts.MainDbWritable);
                return connection;
            }
        );
        services.AddSingleton(
            provider =>
            {
                var connection = provider.GetRequiredService<NpgsqlConnection>();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                var transaction = connection.BeginTransaction();
                return transaction;
            }
        );

        services.AddEntityFrameworkNpgsql()
                .AddDbContext<WriteDbContext>(
                     options =>
                     {
                         options.UseNpgsql(
                             dbConnectionConfig.MainDbWritable,
                             optionBuilder =>
                             {
                                 optionBuilder.CommandTimeout(60);
                                 optionBuilder.UseNetTopologySuite();
                                 optionBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                             }
                         );
                         options.EnableSensitiveDataLogging();
                     }
                 )
                .AddDbContext<ReadDbContext>(
                     options =>
                     {
                         options.UseNpgsql(
                             dbConnectionConfig.MainDbWritable,
                             optionBuilder =>
                             {
                                 optionBuilder.CommandTimeout(60);
                                 optionBuilder.UseNetTopologySuite();
                                 optionBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                             }
                         );
                         options.EnableSensitiveDataLogging();
                     }
                 );

        services.AddSingleton(
            provider =>
            {
                var transaction = provider.GetRequiredService<NpgsqlTransaction>();
                var options = provider.GetRequiredService<DbContextOptions<WriteDbContext>>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                var instance = new WriteDbContext(options, loggerFactory);

                instance.Database.SetDbConnection(transaction.Connection);
                instance.Database.UseTransaction(transaction);
                return instance;
            }
        );

        services.AddSingleton(
            provider =>
            {
                var transaction = provider.GetRequiredService<NpgsqlTransaction>();
                var options = provider.GetRequiredService<DbContextOptions<ReadDbContext>>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                var instance = new ReadDbContext(options, loggerFactory);

                instance.Database.SetDbConnection(transaction.Connection);
                instance.Database.UseTransaction(transaction);
                return instance;
            }
        );

        services.AddSingleton<IWriteDb>(provider => provider.GetRequiredService<WriteDbContext>());
        services.AddSingleton<IReadDb>(provider => provider.GetRequiredService<ReadDbContext>());
        services.AddSingleton<IMigrator, WriteDbContext>();

        services.AddSingleton<DataEnvironment>();
    }

    public static IConfiguration GetConfiguration()
    {
        var tmpConfig = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var testDbWritable = tmpConfig.GetValue<string>("ConnectionStrings:TestDbWritable");
        var testDbReadReplica = tmpConfig.GetValue<string>("ConnectionStrings:TestDbReadReplica");

        return new ConfigurationBuilder().AddEnvironmentVariables()
                                         .AddInMemoryCollection(
                                              new Dictionary<string, string>
                                              {
                                                  {"ConnectionStrings:MainDbWritable", testDbWritable},
                                                  {"ConnectionStrings:MainDbReadReplica", testDbReadReplica}
                                              }
                                          )
                                         .Build();
    }

    private static void AddEmailSending(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var emailSending = new Mock<IEmailSendingService>();

        services.AddSingleton(emailSending);
        services.AddSingleton(emailSending.Object);
    }
}