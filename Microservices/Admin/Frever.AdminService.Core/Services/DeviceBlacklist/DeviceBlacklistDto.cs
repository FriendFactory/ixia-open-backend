using System;

namespace Frever.AdminService.Core.Services.DeviceBlacklist;

public class DeviceBlacklistDto
{
    public string DeviceId { get; set; }

    public DateTime BlockedAt { get; set; }

    public long BlockedByGroupId { get; set; }

    public string BlockedByGroupName { get; set; }

    public string Reason { get; set; }
}