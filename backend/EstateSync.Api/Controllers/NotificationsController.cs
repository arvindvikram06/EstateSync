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
    public async Task<ActionResult<ApiResponse<object>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var notifications = await _notificationRepo.GetAllByUserIdAsync(userId);

        var response = notifications.Select(n => new NotificationResponse
        {
            NotificationId = n.NotificationId,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            EntityId = n.EntityId,
            EntityType = n.EntityType,
            IsRead = n.IsRead,
            // PostgreSQL "timestamp without time zone" values are materialized with
            // DateTimeKind.Unspecified. Mark the stored UTC value explicitly so JSON
            // includes the trailing "Z" and browsers do not treat it as local time.
            CreatedAt = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
        });

        return Ok(new ApiResponse<object> { Data = response });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkOneRead(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _notificationRepo.MarkOneReadAsync(userId, id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new ApiResponse<object> { Message = "Notification marked as read." });
    }

    [HttpPut("read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _notificationRepo.MarkAllReadAsync(userId);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new ApiResponse<object> { Message = "All notifications marked as read." });
    }
}
