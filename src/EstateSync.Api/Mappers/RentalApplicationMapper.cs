using System;
using EstateSync.Api.DTOs.RentalApplications;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class RentalApplicationMapper
{
    public RentalApplicationResponse MapToResponse(RentalApplication app)
    {
        return new RentalApplicationResponse
        {
            ApplicationId = app.ApplicationId,
            PropertyId = app.PropertyId,
            PropertyTitle = app.Property != null ? app.Property.Title : string.Empty,
            TenantId = app.TenantId,
            TenantName = app.Tenant != null ? app.Tenant.FullName : string.Empty,
            Message = app.Message,
            DocumentCount = app.DocumentCount,
            ExpectedMoveInDate = app.ExpectedMoveInDate,
            DurationMonths = app.DurationMonths,
            CommuteDistance = app.CommuteDistance,
            Status = app.Status.ToString(),
            ReviewRemarks = app.ReviewRemarks,
            ReviewedAt = app.ReviewedAt,
            CreatedAt = app.CreatedAt
        };
    }
}
