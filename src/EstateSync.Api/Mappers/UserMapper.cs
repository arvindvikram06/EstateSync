using System;
using System.Collections.Generic;
using System.Linq;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.DTOs.OwnerVerification;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class UserMapper
{
    public DocumentResponse MapToDocumentResponse(Document d)
    {
        return new DocumentResponse
        {
            DocumentId = d.DocumentId,
            DocumentType = d.DocumentType.ToString(),
            DocumentUrl = d.DocumentUrl,
            OriginalFileName = d.OriginalFileName,
            MimeType = d.MimeType,
            FileSize = d.FileSize,
            ReviewStatus = d.ReviewStatus.ToString(),
            UploadedAt = d.UploadedAt
        };
    }

    public OwnerVerificationResponse MapToVerificationRequestResponse(OwnerVerificationRequest s)
    {
        return new OwnerVerificationResponse
        {
            RequestId = s.RequestId,
            UserId = s.UserId,
            FullName = s.User != null ? s.User.FullName : string.Empty,
            Email = s.User != null ? s.User.Email : string.Empty,
            Status = s.Status.ToString(),
            ReviewRemarks = s.ReviewRemarks,
            CreatedAt = s.CreatedAt,

            Documents = s.OwnerVerificationDocuments
                .Where(ovd => ovd.Document != null)
                .Select(ovd => MapToDocumentResponse(ovd.Document!))
                .ToList()
        };
    }

    public IdentityVerificationResponse MapToIdentityResponse(IdentityVerificationRequest s)
    {
        return new IdentityVerificationResponse
        {
            RequestId = s.RequestId,
            UserId = s.UserId,
            FullName = s.User != null ? s.User.FullName : string.Empty,
            Email = s.User != null ? s.User.Email : string.Empty,
            AadhaarNumber = s.AadhaarNumber,
            PanNumber = s.PanNumber,
            Status = s.Status.ToString(),
            ReviewRemarks = s.ReviewRemarks,
            CreatedAt = s.CreatedAt,
            Documents = s.IdentityVerificationDocuments
                .Where(ivd => ivd.Document != null)
                .Select(ivd => MapToDocumentResponse(ivd.Document!))
                .ToList()
        };
    }
}
