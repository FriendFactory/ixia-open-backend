using System.Reflection;
using System.Text;
using Amazon.S3;
using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Frever.Common.IntegrationTesting.Migrations;

public abstract class MigrationFromS3 : MigrationBase
{
    protected sealed override void Up(MigrationBuilder migrationBuilder)
    {
        try
        {
            var attr = (MigrationAttribute) GetType().GetCustomAttribute(typeof(MigrationAttribute));
            if (attr == null)
                return;


            var s3Path = attr.Id;
            s3Path = s3Path.Replace("s3://", string.Empty);

            var parts = s3Path.Split("/");

            var bucket = parts[0];
            var key = string.Join("/", parts[1..]);

            var sqlQuery = DownloadFile(bucket, key);

            migrationBuilder.Sql(sqlQuery);
        }
        catch (Exception ex) when (ex.GetType() != typeof(MigrationFileNotFoundException))
        {
            var typeName = GetType().Name;

            throw new Exception($"Failed apply migration {typeName}.\n\r {ex.Message}", ex);
        }
    }

    private string DownloadFile(string bucket, string key)
    {
        var provider = GetServiceProvider();
        var s3 = provider.GetRequiredService<IAmazonS3>();

        var response = s3.GetObjectAsync(bucket, key).Result;
        var reader = new StreamReader(response.ResponseStream);

        var builder = new StringBuilder();

        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--", StringComparison.InvariantCultureIgnoreCase) ||
                line.StartsWith("SET ", StringComparison.InvariantCultureIgnoreCase) || line.StartsWith(
                    "SELECT pg_catalog.set_config('search_path'",
                    StringComparison.InvariantCultureIgnoreCase
                ) || line.StartsWith("begin", StringComparison.InvariantCultureIgnoreCase) || line.StartsWith(
                    "commit",
                    StringComparison.InvariantCultureIgnoreCase
                ))
                continue;

            builder.AppendLine(line);
        }


        return builder.ToString();
    }

    private IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();

        var mock = new Mock<ITestOutputHelper>();

        services.AddIntegrationTests(mock.Object);

        return services.BuildServiceProvider();
    }
}