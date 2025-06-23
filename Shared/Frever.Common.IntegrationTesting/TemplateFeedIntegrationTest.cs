using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Common.IntegrationTesting;

public class CreateDatabaseIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    // Sergii: This test is used by CI to init database
    // Do not remove it as useless
    [Fact]
    public async Task CreateDatabaseTest()
    {
        var services = new ServiceCollection();
        services.AddIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<DataEnvironment>();
    }
}