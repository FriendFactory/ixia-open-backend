using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using FluentValidation;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.DeviceBlacklist;

public class DeviceBlacklistAdminService(UserInfo currentUser, IWriteDb db, IValidator<BlockDeviceParams> blockParamsValidator)
    : IDeviceBlacklistAdminService
{
    public async Task<DeviceBlacklistDto[]> GetDeviceBlacklist(string search, int skip, int take)
    {
        var devices = db.DeviceBlacklist.Join(
            db.Group,
            b => b.BlockedByGroupId,
            g => g.Id,
            (b, g) => new {BlockedDevice = b, BlockedBy = g}
        );

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            devices = devices.Where(
                d => d.BlockedDevice.DeviceId.Contains(search) || d.BlockedDevice.Reason.Contains(search) ||
                     d.BlockedBy.NickName.Contains(search)
            );
        }

        return await devices.OrderByDescending(a => a.BlockedDevice.BlockedAt)
                            .Skip(skip)
                            .Take(take)
                            .Select(
                                 a => new DeviceBlacklistDto
                                      {
                                          DeviceId = a.BlockedDevice.DeviceId,
                                          Reason = a.BlockedDevice.Reason,
                                          BlockedAt = a.BlockedDevice.BlockedAt,
                                          BlockedByGroupId = a.BlockedDevice.BlockedByGroupId,
                                          BlockedByGroupName = a.BlockedBy.NickName
                                      }
                             )
                            .ToArrayAsync();
    }

    public async Task<DeviceBlacklistDto> BlockDevice(BlockDeviceParams request)
    {
        await blockParamsValidator.ValidateAndThrowAsync(request);
        if (await db.DeviceBlacklist.AnyAsync(d => d.DeviceId == request.DeviceId))
            throw AppErrorWithStatusCodeException.BadRequest("Device already blocked", "DeviceBlocked");

        await db.DeviceBlacklist.AddAsync(
            new Shared.MainDb.Entities.DeviceBlacklist
            {
                Reason = request.Reason,
                BlockedAt = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                BlockedByGroupId = currentUser
            }
        );

        await db.SaveChangesAsync();

        return await db.DeviceBlacklist.Join(db.Group, b => b.BlockedByGroupId, g => g.Id, (b, g) => new {BlockedDevice = b, BlockedBy = g})
                       .Select(
                            a => new DeviceBlacklistDto
                                 {
                                     DeviceId = a.BlockedDevice.DeviceId,
                                     Reason = a.BlockedDevice.Reason,
                                     BlockedAt = a.BlockedDevice.BlockedAt,
                                     BlockedByGroupId = a.BlockedDevice.BlockedByGroupId,
                                     BlockedByGroupName = a.BlockedBy.NickName
                                 }
                        )
                       .FirstOrDefaultAsync(a => a.DeviceId == request.DeviceId);
    }

    public async Task UnblockDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(deviceId));

        var blocked = await db.DeviceBlacklist.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
        if (blocked != null)
        {
            db.DeviceBlacklist.Remove(blocked);
            await db.SaveChangesAsync();
        }
    }
}