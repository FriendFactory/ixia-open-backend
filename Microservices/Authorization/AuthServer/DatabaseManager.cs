// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using AuthServer.Data;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer;

public class DatabaseManager
{
    public static void CreateTables()
    {
        var services = new ServiceCollection();

        var startup = new Startup();
        startup.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var authContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        authContext.Database.Migrate();
        var grandContext = scope.ServiceProvider.GetService<PersistedGrantDbContext>();
        grandContext.Database.Migrate();
    }
}