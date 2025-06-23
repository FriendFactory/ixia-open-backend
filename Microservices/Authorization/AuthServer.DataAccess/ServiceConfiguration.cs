using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.DataAccess;

public static class ServiceConfiguration
{
    public static void AddFreverAuthDbDataAccess(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

        services.AddDbContext<AuthServerDbContext>(options => { options.UseNpgsql(connectionString); });
    }
}