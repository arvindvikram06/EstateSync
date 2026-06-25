using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        // SaveChangesAsync will be called by UnitOfWork, so just modify in memory.
    }

    // Reserved for background cleanup job — not yet implemented
    // public async Task DeleteNotificationsOlderThanDaysAsync(int days)
    // {
    //     var thresholdDate = DateTime.UtcNow.AddDays(-days);
    //     var oldNotifications = await _dbSet
    //         .Where(n => n.CreatedAt < thresholdDate)
    //         .ToListAsync();
    //     
    //     _dbSet.RemoveRange(oldNotifications);
    // }
}