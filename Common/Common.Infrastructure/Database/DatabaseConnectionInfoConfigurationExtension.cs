using System;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.Database;

public static class DatabaseConnectionInfoConfigurationExtension
{
    public static DatabaseConnectionConfiguration GetDbConnectionConfiguration(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var mainDbWritable = configuration.GetValue<string>("ConnectionStrings:MainDbWritable");
        var mainDbReadReplica = configuration.GetValue<string>("ConnectionStrings:MainDbReadReplica");
        var authDbWritable = configuration.GetValue<string>("ConnectionStrings:AuthDbWritable");

        var connectionInfo = new DatabaseConnectionConfiguration
                             {
                                 MainDbWritable = mainDbWritable, MainDbReadReplica = mainDbReadReplica, AuthDb = authDbWritable
                             };

        var validator = new InlineValidator<DatabaseConnectionConfiguration>();
        validator.RuleFor(o => o.AuthDb).NotEmpty().MinimumLength(3);
        validator.RuleFor(o => o.MainDbWritable).NotEmpty().MinimumLength(3);
        validator.RuleFor(o => o.MainDbReadReplica).NotEmpty().MinimumLength(3);

        validator.ValidateAndThrow(connectionInfo);

        return connectionInfo;
    }
}

public class DatabaseConnectionConfiguration
{
    public string MainDbWritable { get; internal set; }
    public string MainDbReadReplica { get; internal set; }
    public string AuthDb { get; internal set; }
}