using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Client.Shared.AI.Metadata;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.AI.Billing;

public interface IAiBillingService
{
    Task<bool> TryPurchaseAiWorkflowRun(string workflow, string key, long aiContentId, decimal? units);

    Task RefundAiWorkflowRun(long aiContentId);
}

public class AiBillingService(
    UserInfo currentUser,
    IAiWorkflowMetadataService workflowMetadata,
    ILoggerFactory loggerFactory,
    IAiBillingRepository repo,
    IAssetStoreTransactionGenerationService transactionGenerator
) : IAiBillingService
{
    private readonly ILogger _log = loggerFactory.CreateLogger("Ixia.Billing");

    public async Task<bool> TryPurchaseAiWorkflowRun(string workflow, string key, long aiContentId, decimal? units)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflow);
        if (units is <= 0)
            throw new ArgumentException("Units must be positive", nameof(units));

        using var logScope = _log.BeginScope(
            "Ai Workflow Run Billing: Charge: GroupID={groupId} Workflow={wf} Units={units} : ",
            currentUser.UserMainGroupId,
            workflow,
            units
        );

        var allWorkflowPrices = await workflowMetadata.GetInternal();

        var workflows = allWorkflowPrices.Where(w => StringComparer.OrdinalIgnoreCase.Equals(w.AiWorkflow, workflow)).ToArray();
        if (workflows.Length == 0)
        {
            _log.LogDebug("Workflow is free to use");
            return false;
        }

        if (workflows.Length > 1 && !workflows.Any(w => StringComparer.OrdinalIgnoreCase.Equals(w.Key, key)))
            throw new ArgumentException($"Key is required for {workflow}", nameof(key));

        var price = workflows.Length > 1 ? workflows.First(w => w.Key == key) : workflows.First();
        if (price.HardCurrencyPrice == 0)
        {
            _log.LogDebug("Workflow is free to use");
            return false;
        }

        if (price.RequireBillingUnits && units == null)
            throw new ArgumentException($"Units is required for {workflow}", nameof(units));

        var amount = (int) Math.Ceiling(price.HardCurrencyPrice * (units ?? 1));

        _log.LogDebug("User is about to be charged for Amount={amount}", amount);

        await using var transaction = await repo.BeginTransaction();

        var balance = await repo.GetUserBalance(currentUser);

        using var logScope2 = _log.BeginScope("Amount={amount} Balance={balance} : ", amount, balance);

        if (balance < amount)
        {
            _log.LogError("Not enough balance");
            throw new AiWorkflowPurchaseNotEnoughBalanceException($"Requires {amount} but {balance} available");
        }

        var billingTransactions = await transactionGenerator.AiWorkflowRun(
                                      currentUser,
                                      workflow,
                                      aiContentId,
                                      units,
                                      amount
                                  );
        await repo.SaveBillingTransactions(billingTransactions);

        await transaction.Commit();
        return true;
    }

    public async Task RefundAiWorkflowRun(long aiContentId)
    {
        using var logScope = _log.BeginScope("Ai Workflow Refund: ContentId={aiContentId} : ", aiContentId);

        await using var transaction = await repo.BeginTransaction();

        var transactions = await repo.GetTransactionsInGroup(aiContentId);
        if (transactions.Length == 0)
        {
            _log.LogInformation("No transactions found for {ContentId}", aiContentId);
            return;
        }

        if (transactions.Any(t => t.TransactionType == AssetStoreTransactionType.AiWorkflowRunErrorRefund))
        {
            _log.LogWarning("Workflow run was refund before");
            return;
        }

        var amount = transactions.Where(t => t.TransactionType == AssetStoreTransactionType.AiWorkflowRun)
                                 .Sum(t => Math.Abs(t.HardCurrencyAmount));

        var transactionToRefund = transactions.First(t => t.TransactionType == AssetStoreTransactionType.AiWorkflowRun);

        var refundTransactions = await transactionGenerator.AiWorkflowRefund(
                                     aiContentId,
                                     transactionToRefund.GroupId,
                                     transactionToRefund.TransactionGroup,
                                     transactionToRefund.AiWorkflow,
                                     transactionToRefund.AiWorkflowBillingUnits,
                                     amount
                                 );

        await repo.SaveBillingTransactions(refundTransactions);

        await transaction.Commit();

        _log.LogInformation(
            "Refund AI Workflow run Workflow={wf} Units={units} Amount={amount}",
            transactionToRefund.AiWorkflow,
            transactionToRefund.AiWorkflowBillingUnits,
            amount
        );
    }
}

public class AiWorkflowPurchaseException(string message, HttpStatusCode statusCode) : AppErrorWithStatusCodeException(message, statusCode);

public class AiWorkflowPurchaseNotEnoughBalanceException(string message)
    : AiWorkflowPurchaseException(message, HttpStatusCode.PaymentRequired);