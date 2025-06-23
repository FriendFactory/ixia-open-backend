using Frever.ClientService.Api;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Frever.Client.Core.Test.Utils;

public static class TestServiceConfiguration
{
    public static void AddTestClientServices(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(testOut);

        services.AddUnitTestServices(testOut);

        services.AddAutoMapper(typeof(Startup));
    }
}