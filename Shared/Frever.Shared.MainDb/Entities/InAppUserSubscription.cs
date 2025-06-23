using System;

namespace Frever.Shared.MainDb.Entities;

public class InAppUserSubscription
{
    public static readonly string KnownStatusActive = "Active";
    public static readonly string KnownStatusCanceled = "Canceled";
    public static readonly string KnownStatusUpgraded = "Upgraded";
    public static readonly string KnownStatusDowngraded = "Downgraded";
    public static readonly string KnownStatusComplete = "Complete";


    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid InAppPurchaseOrderId { get; set; }

    /// <summary>
    /// Information only. Use values from this entity in token calulcations
    /// </summary>
    public long RefInAppProductId { get; set; }

    public int DailyHardCurrency { get; set; }
    public int MonthlyHardCurrency { get; set; }

    public DateTime CreatedAt { get; set; }
}