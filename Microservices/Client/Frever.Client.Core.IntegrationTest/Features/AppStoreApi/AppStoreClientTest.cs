using FluentAssertions;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.Client.Core.IntegrationTest.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.AppStoreApi;

public class AppStoreClientTest(ITestOutputHelper testOut)
{
    [Fact]
    public async Task CheckSubscriptionStatus_ShouldWork()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClientIntegrationTests(testOut);

        await using var provider = serviceCollection.BuildServiceProvider();

        var appStoreClient = provider.GetRequiredService<IAppStoreApiClient>();

        var status = await appStoreClient.CheckSubscriptionStatus("2000000914055794");
        status.Should().NotBeNull();
        status.IsSubscriptionActive.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAppStoreTransactionStatus_ShouldWorkForSubscription()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClientIntegrationTests(testOut);

        await using var provider = serviceCollection.BuildServiceProvider();

        var appStoreClient = provider.GetRequiredService<IAppStoreApiClient>();

        var status = await appStoreClient.CheckAppStoreTransactionStatus("2000000912418180");
        status.Should().NotBeNull();
        status.IsValid.Should().BeTrue();
        status.Environment.Should().Be("Sandbox");
        status.InAppProductId.Should().Be("ixia_sub_s");
        status.IsSubscription.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAppStoreTransactionStatus_ShouldWorkForConsumable()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClientIntegrationTests(testOut);

        await using var provider = serviceCollection.BuildServiceProvider();

        var appStoreClient = provider.GetRequiredService<IAppStoreApiClient>();

        var status = await appStoreClient.CheckAppStoreTransactionStatus("2000000918793892");
        status.Should().NotBeNull();
        status.IsValid.Should().BeTrue();
        status.Environment.Should().Be("Sandbox");
        status.InAppProductId.Should().Be("token_refill_m");
        status.IsSubscription.Should().BeFalse();
    }

    [Fact]
    public async Task AppStoreClient_TransactionData()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClientIntegrationTests(testOut);

        await using var provider = serviceCollection.BuildServiceProvider();

        var appStoreClient = provider.GetRequiredService<IAppStoreApiClient>();

        var appleTransactionId = "2000000908842083";

        var transactionStatus = await appStoreClient.CheckAppStoreTransactionStatus(appleTransactionId);
        var subscriptionStatus = await appStoreClient.CheckSubscriptionStatus(appleTransactionId);
    }

    [Fact]
    public async Task AppStoreClient_TransactionHistory()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClientIntegrationTests(testOut);

        await using var provider = serviceCollection.BuildServiceProvider();

        var appStoreClient = provider.GetRequiredService<IAppStoreApiClient>();

        var appleTransactionId = "2000000908842083";

        var transactionHistory = await appStoreClient.TransactionHistory(appleTransactionId);
        var t = transactionHistory.First(a => a.TransactionId == "2000000908842083");

        transactionHistory.Should().NotBeNull();
        transactionHistory.Length.Should().BeGreaterThan(3);
    }
}