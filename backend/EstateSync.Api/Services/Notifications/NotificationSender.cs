using EstateSync.Api.DTOs.Notifications;
using EstateSync.Api.Hubs;
using EstateSync.Api.Interfaces.Services.Email;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Email;
using Microsoft.AspNetCore.SignalR;

namespace EstateSync.Api.Services.Notifications;

public class NotificationSender : INotificationSender
{
    // Notification types important enough to also email. Chatty/low-stakes
    // types (General, ComplaintUpdated, DocumentUploaded, ...) stay in-app only.
    private static readonly HashSet<NotificationType> EmailWorthyTypes = new()
    {
        NotificationType.LeaseCreated,
        NotificationType.LeasePendingAdminApproval,
        NotificationType.LeaseApprovedByAdmin,
        NotificationType.LeaseRejectedByAdmin,
        NotificationType.LeaseVoided,
        NotificationType.LeaseTerminated,
        NotificationType.LeaseExpiring,
        NotificationType.LeaseExpired,
        NotificationType.LeaseRenewalRequested,
        NotificationType.LeaseRenewalApproved,
        NotificationType.LeaseRenewalRejected,
        NotificationType.RentDue,
        NotificationType.PaymentReceived,
        NotificationType.PaymentFailed,
        NotificationType.ApplicationApproved,
        NotificationType.ApplicationRejected,
        NotificationType.ComplaintRaised,
        NotificationType.VerificationApproved,
        NotificationType.VerificationRejected,
        NotificationType.DepositForfeited,
        NotificationType.DepositReturnReminder,
    };

    private readonly INotificationRepository _notificationRepo;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUserRepository _userRepo;
    private readonly IEmailQueue _emailQueue;
    private readonly string _frontendUrl;

    public NotificationSender(
        INotificationRepository notificationRepo,
        IHubContext<NotificationHub> hubContext,
        IUserRepository userRepo,
        IEmailQueue emailQueue,
        IConfiguration configuration)
    {
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
        _frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:4200";
    }

    public async Task SendAsync(Notification notification, CancellationToken ct = default)
    {
        await _notificationRepo.AddAsync(notification);

        var dto = new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            EntityId = notification.EntityId,
            EntityType = notification.EntityType,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        await _hubContext.Clients
            .User(notification.UserId.ToString())
            .SendAsync("ReceiveNotification", dto, ct);

        // Third channel: email for important events. Enqueue is instant;
        // actual SMTP work happens in the EmailDispatcher background service.
        if (EmailWorthyTypes.Contains(notification.Type))
        {
            var user = await _userRepo.GetByIdAsync(notification.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailQueue.EnqueueAsync(new EmailMessage
                {
                    To = user.Email,
                    Subject = notification.Title,
                    Heading = notification.Title,
                    Body = notification.Message,
                    CtaLabel = "Open EstateSync",
                    CtaUrl = _frontendUrl,
                }, ct);
            }
        }
    }
}
