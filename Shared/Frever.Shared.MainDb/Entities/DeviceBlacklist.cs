using System;

namespace Frever.Shared.MainDb.Entities;

public class DeviceBlacklist
{
    public string DeviceId { get; set; }
    public DateTime BlockedAt { get; set; }
    public long BlockedByGroupId { get; set; }
    public string Reason { get; set; }
}