using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.ClientService.Api.Features.InAppPurchases;

public class AndroidRefundPurchaseWatcher : IHostedService
{
    private const long CheckInterval = 30 * 60 * 1000; // 30 min
    private static readonly Guid InstanceId = Guid.NewGuid();
    private static readonly string AndroidRefundPurchaseWatcherSyncKey = "frever::workers::android-purchase-refund";
    private readonly ICache _cache;
    private readonly ILogger _log;
    private readonly Timer _runTimer;
    private readonly IServiceProvider _serviceProvider;

    public AndroidRefundPurchaseWatcher(ICache cache, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _serviceProvider = serviceProvider;
        _runTimer = new Timer(OnCheckVoidedPurchase, null, Timeout.Infinite, CheckInterval);

        _log = loggerFactory.CreateLogger("Frever.InAppPurchase.Refund");
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runTimer.Change(0, CheckInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _runTimer.Change(Timeout.Infinite, CheckInterval);
        return Task.CompletedTask;
    }

    private async void OnCheckVoidedPurchase(object _)
    {
        if (await _cache.Db().LockTakeAsync(AndroidRefundPurchaseWatcherSyncKey, InstanceId.ToString(), TimeSpan.FromHours(3)))
            try
            {
                await CheckVoidedPurchaseCore();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing in-app purchase");
            }
            finally
            {
                await _cache.Db().LockReleaseAsync(AndroidRefundPurchaseWatcherSyncKey, InstanceId.ToString());
            }
    }

    private async Task CheckVoidedPurchaseCore()
    {
        using var _ = _log.BeginScope("Android Voided Purchase Worker {iid}: ", InstanceId);
        _log.LogInformation("Start voided purchase check");

        using var scope = _serviceProvider.CreateScope();

        var extractor = scope.ServiceProvider.GetRequiredService<GoogleVoidedPurchaseExtractor>();
        var since = DateTime.UtcNow.AddDays(-10);

        var voidedPurchases = await extractor.GetVoidedPurchase(since);

        _log.LogInformation("Voided purchases received: {r}", JsonConvert.SerializeObject(voidedPurchases));

        var refundService = scope.ServiceProvider.GetRequiredService<IRefundInAppPurchaseService>();

        var storeOrderIdentifiers = voidedPurchases.Where(a => !string.IsNullOrWhiteSpace(a.OrderId)).Select(a => a.OrderId).ToArray();

        var orders = await refundService.GetNotRefundOrders(Platform.Android, storeOrderIdentifiers).ToArrayAsync();

        _log.LogInformation("Non-refunded orders: {nro}", JsonConvert.SerializeObject(orders));

        foreach (var order in orders)
        {
            _log.LogInformation("Refunding order {id} {o}", order.Id, order.StoreOrderIdentifier);

            await refundService.RefundInAppPurchase(
                new RefundInAppPurchaseRequest {Platform = Platform.Android, StoreOrderIdentifier = order.StoreOrderIdentifier}
            );

            _log.LogInformation("Order has been refunded {id} {o}", order.Id, order.StoreOrderIdentifier);
        }

        _log.LogInformation("Complete voided purchase check");
    }
}