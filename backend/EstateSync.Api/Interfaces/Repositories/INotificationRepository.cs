using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
    Task<IEnumerable<Notification>> GetAllByUserIdAsync(Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task<bool> MarkOneReadAsync(Guid userId, Guid notificationId);

    // Reserved for background cleanup job — not yet implemented
    // Task DeleteNotificationsOlderThanDaysAsync(int days);
}
