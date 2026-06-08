using System;
using System.Collections.Generic;
using System.Linq;
using EstateSync.Api.DTOs.Users;
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
            ReviewNotes = d.ReviewNotes,
            UploadedAt = d.UploadedAt
        };
    }

    public VerificationRequestResponse MapToVerificationRequestResponse(OwnerVerificationRequest s)
    {
        return new VerificationRequestResponse
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
}
