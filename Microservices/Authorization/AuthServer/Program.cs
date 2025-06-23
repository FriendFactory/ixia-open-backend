// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Common.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AuthServer;

public class Program
{
    public static void Main(string[] args)
    {
        AWSEnvironmentSetup.Setup();
        var host = CreateWebHostBuilder(args).Build();

        var needCreateTables = false; //todo: create tables if tables are not existed
        if (needCreateTables)
        {
            DatabaseManager.CreateTables();

            return;
        }

        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
                      .UseSetting("detailedErrors", "true")
                      .UseStartup<Startup>()
                      .ConfigureLogging(
                           builder =>
                           {
                               builder.ClearProviders();
                               builder.AddSimpleConsole(
                                   options =>
                                   {
                                       options.IncludeScopes = true;
                                       options.UseUtcTimestamp = true;
                                       options.SingleLine = true;
                                       options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
                                       options.ColorBehavior = LoggerColorBehavior.Disabled;
                                   }
                               );
                           }
                       )
                      .UseIISIntegration();
    }
}