using System.Linq;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.TenantFinancialProfile;
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
            UploadedAt = d.UploadedAt
        };
    }

    public KycVerificationResponse MapToKycResponse(KycVerification k)
    {
        return new KycVerificationResponse
        {
            KycVerificationId = k.KycVerificationId,
            UserId = k.UserId,
            FullName = k.User?.FullName ?? string.Empty,
            Email = k.User?.Email ?? string.Empty,
            AadhaarNumber = k.AadhaarNumber,
            PanNumber = k.PanNumber,
            Status = k.Status.ToString(),
            ReviewRemarks = k.ReviewRemarks,
            ReviewedAt = k.ReviewedAt,
            CreatedAt = k.CreatedAt,
            Documents = k.KycDocuments
                .Where(d => d.Document != null)
                .Select(d => MapToDocumentResponse(d.Document!))
                .ToList()
        };
    }

    public TenantFinancialProfileResponse MapToFinancialProfileResponse(TenantFinancialProfile p)
    {
        return new TenantFinancialProfileResponse
        {
            ProfileId = p.ProfileId,
            TenantId = p.TenantId,
            TenantName = p.Tenant?.FullName ?? string.Empty,
            TenantEmail = p.Tenant?.Email ?? string.Empty,
            EmploymentType = p.EmploymentType.ToString(),
            EmployerName = p.EmployerName,
            DeclaredMonthlyIncome = p.DeclaredMonthlyIncome,
            Status = p.Status.ToString(),
            ReviewRemarks = p.ReviewRemarks,
            ReviewedAt = p.ReviewedAt,
            CreatedAt = p.CreatedAt,
            Documents = p.TenantFinancialDocuments
                .Where(d => d.Document != null)
                .Select(d => MapToDocumentResponse(d.Document!))
                .ToList()
        };
    }
}
