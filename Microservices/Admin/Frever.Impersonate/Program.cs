using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AuthServer.TokenGeneration;
using Common.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frever.Impersonate;

internal class Program
{
    private static void Main(string[] args)
    {
        while (true)
        {
            var serviceCollection = new ServiceCollection();

            Console.WriteLine("Do you want to use local or remote mode? (l - local, r - remote, <enter> - remote)");
            var mode = Console.ReadLine();

            var isLocal = StringComparer.OrdinalIgnoreCase.Equals(mode, "l");

            if (isLocal)
                Console.WriteLine("Run in LOCAL mode. Local auth server must be started.");
            else
                Console.WriteLine("Run in REMOTE mode. SSH connection to remote server must be active.");

            var connectionString = isLocal ? "localhost:6379" : "localhost:6388";
            var version = isLocal ? "0.0" : "1.8";

            // Client would work only if SSH tunnel to corresponding cluster is active
            serviceCollection.AddRedis(
                new RedisSettings {ClientIdentifier = "impersonate-cli", ConnectionString = connectionString},
                version
            );

            serviceCollection.AddLogging(
                config =>
                {
                    config.AddConsole();
                    config.SetMinimumLevel(LogLevel.Warning);
                }
            );

            serviceCollection.AddTokenGeneration();

            var provider = serviceCollection.BuildServiceProvider();
            var client = provider.GetRequiredService<ITokenGenerationClient>();

            var hostedServicesThreads = new List<Thread>();

            var hostedServices = provider.GetServices<IHostedService>();
            foreach (var hs in hostedServices)
            {
                var thread = new Thread(_ => { hs.StartAsync(CancellationToken.None).Wait(); });
                thread.Start();
                hostedServicesThreads.Add(thread);
            }

            Console.WriteLine("Enter group ID to generate token:");
            var groupIdStr = Console.ReadLine();

            if (!long.TryParse(groupIdStr, out var groupId))
            {
                Console.WriteLine("Value is not a valid group ID");
                continue;
            }

            try
            {
                var tokenResult = client.GenerateTokenByGroupId(groupId).Result;
                if (!tokenResult.Ok)
                {
                    Console.WriteLine($"Error generating token: {tokenResult.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine(tokenResult.Jwt);

                    Console.WriteLine();
                    Process.Start("/bin/bash", $"-c \"echo {tokenResult.Jwt} | pbcopy \"").WaitForExit(3000);
                    Console.WriteLine("Token copied to clipboard");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating token:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}