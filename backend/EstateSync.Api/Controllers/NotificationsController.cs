using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Notifications;
using EstateSync.Api.Extensions;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(INotificationRepository notificationRepo, IUnitOfWork unitOfWork)
    {
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetUnread(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var notifications = await _notificationRepo.GetUnreadByUserIdAsync(userId);

        var response = notifications.Select(n => new NotificationResponse
        {
            NotificationId = n.NotificationId,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            EntityId = n.EntityId,
            EntityType = n.EntityType,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        });

        return Ok(new ApiResponse<object> { Data = response });
    }

    [HttpPut("read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _notificationRepo.MarkAllReadAsync(userId);
        return Ok(new ApiResponse<object> { Message = "All notifications marked as read." });
    }
}
