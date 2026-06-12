using System;
using System.Linq;
using EstateSync.Api.DTOs.TenantVerification;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class TenantVerificationMapper
{
    private readonly UserMapper _userMapper;

    public TenantVerificationMapper(UserMapper userMapper)
    {
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
    }

    public DocumentResponse MapToDocumentResponse(Document d)
    {
        return _userMapper.MapToDocumentResponse(d);
    }

    public TenantVerificationResponse MapToResponse(TenantVerificationRequest request)
    {
        return new TenantVerificationResponse
        {
            RequestId = request.RequestId,
            TenantId = request.TenantId,
            ApplicationId = request.ApplicationId,
            Status = request.Status.ToString(),
            ReviewRemarks = request.ReviewRemarks,
            CreatedAt = request.CreatedAt,
            Documents = request.TenantVerificationDocuments
                .Where(tvd => tvd.Document != null)
                .Select(tvd => MapToDocumentResponse(tvd.Document!))
                .ToList()
        };
    }
}
