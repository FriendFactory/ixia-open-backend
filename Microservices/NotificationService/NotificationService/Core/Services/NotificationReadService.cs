using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.Client.Shared.Social.Services;
using Microsoft.EntityFrameworkCore;
using NotificationService.DataAccess;
using NotificationService.Shared.Notifications;

namespace NotificationService.Core;

public interface INotificationReadService
{
    Task<NotificationBase[]> MyNotifications(int skip, int top = 50);

    Task MarkNotificationsAsRead(long[] notificationIds);
}

internal sealed class NotificationReadService(
    INotificationRepository repo,
    UserInfo currentUser,
    INotificationMapper notificationMapper,
    ISocialSharedService socialSharedService
) : INotificationReadService
{
    private readonly INotificationMapper _notificationMapper = notificationMapper ?? throw new ArgumentNullException(nameof(notificationMapper));
    private readonly INotificationRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly ISocialSharedService _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));

    public async Task<NotificationBase[]> MyNotifications(int skip, int top = 100)
    {
        var blockedGroup = await _socialSharedService.GetBlocked(currentUser);

        var notificationViews = await _repo.AllGroupNotifications(currentUser)
                                           .Where(
                                                n => (n.DataGroup == null || !blockedGroup.Contains(n.DataGroup.Id)) &&
                                                     (n.Notification.DataGroupId == null ||
                                                      !blockedGroup.Contains(n.Notification.DataGroupId.Value))
                                            )
                                           .OrderByDescending(view => view.Id)
                                           .Skip(skip)
                                           .Take(top)
                                           .ToArrayAsync();

        var result = new List<NotificationBase>();

        foreach (var item in notificationViews)
        {
            var notification = await _notificationMapper.Map(currentUser, item, blockedGroup);
            if (notification != null)
                result.Add(notification);
        }

        return result.ToArray();
    }

    public async Task MarkNotificationsAsRead(long[] notificationIds)
    {
        ArgumentNullException.ThrowIfNull(notificationIds);

        if (notificationIds.Length == 0)
            return;

        await _repo.MarkNotificationsAsRead(currentUser, notificationIds);
    }
}