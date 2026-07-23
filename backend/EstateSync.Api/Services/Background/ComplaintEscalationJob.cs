using EstateSync.Api.Data;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateSync.Api.Services.Background;

public sealed class ComplaintEscalationJob
{
    private readonly AppDbContext _db;
    private readonly INotificationSender _notifications;
    private readonly ILogger<ComplaintEscalationJob> _logger;

    public ComplaintEscalationJob(
        AppDbContext db,
        INotificationSender notifications,
        ILogger<ComplaintEscalationJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-48);
        var complaints = await _db.Complaints
            .Include(x => x.Lease).ThenInclude(x => x!.Property)
            .Where(x => x.Status == ComplaintStatus.Open &&
                        x.OwnerViewedAt == null &&
                        x.EscalatedAt == null &&
                        x.CreatedAt <= cutoff)
            .ToListAsync();
        if (complaints.Count == 0) return;

        var admins = await _db.UserRoles.AsNoTracking()
            .Where(x => x.Role != null && x.Role.Name == "Admin" && x.User != null && x.User.IsActive)
            .Select(x => x.User!)
            .Distinct()
            .ToListAsync();

        foreach (var complaint in complaints)
        {
            complaint.EscalatedAt = DateTime.UtcNow;
            complaint.UpdatedAt = DateTime.UtcNow;
            foreach (var admin in admins)
            {
                await _notifications.SendAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = admin.UserId,
                    Type = NotificationType.ComplaintEscalated,
                    Title = "Unattended Complaint",
                    Message = $"A complaint for '{complaint.Lease?.Property?.Title}' has not been opened by the owner for 48 hours.",
                    EntityId = complaint.ComplaintId.ToString(),
                    EntityType = "Complaint",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            _logger.LogWarning("Complaint {ComplaintId} escalated after 48 hours without owner view.", complaint.ComplaintId);
        }

        await _db.SaveChangesAsync();
    }
}
