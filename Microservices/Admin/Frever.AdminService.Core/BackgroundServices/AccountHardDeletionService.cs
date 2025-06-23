using System;
using System.Threading;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.AccountModeration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.BackgroundServices;

internal sealed class AccountHardDeletionService(
    ILogger<AccountHardDeletionService> logger,
    IAccountHardDeletionService accountHardDeletionService
) : IHostedService, IDisposable
{
    private const int RefreshRangeHours = 24;

    private readonly IAccountHardDeletionService _accountHardDeletionService =
        accountHardDeletionService ?? throw new ArgumentNullException(nameof(accountHardDeletionService));

    private readonly ILogger<AccountHardDeletionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private Timer _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(HardDeleteAccounts, null, TimeSpan.Zero, TimeSpan.FromHours(RefreshRangeHours));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AccountHardDeletionService is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private async void HardDeleteAccounts(object state)
    {
        _logger.LogInformation("AccountHardDeletionService started");
        await _accountHardDeletionService.HardDeleteGroups();
        _logger.LogInformation("AccountHardDeletionService executed");
    }
}