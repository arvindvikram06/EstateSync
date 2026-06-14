using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class User : BaseEntity
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    
    public string? AadhaarNumber { get; set; }
    public string? PanNumber { get; set; }
    public bool IsIdentityVerified { get; set; } = false;

    // Stripe Mappings
    public StripeConnectedAccount? StripeConnectedAccount { get; set; }
    public StripeCustomerProfile? StripeCustomerProfile { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }

    public Guid? DeletedById { get; set; }
    public User? DeletedBy { get; set; }

    public OwnerProfile? OwnerProfile { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<OwnerVerificationRequest> OwnerVerificationRequests { get; set; } = new List<OwnerVerificationRequest>();
    public ICollection<IdentityVerificationRequest> IdentityVerificationRequests { get; set; } = new List<IdentityVerificationRequest>();
    public ICollection<IdentityVerificationRequest> ReviewedIdentityVerificationRequests { get; set; } = new List<IdentityVerificationRequest>();
    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<SavedProperty> SavedProperties { get; set; } = new List<SavedProperty>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
    public ICollection<Lease> LeasesAsTenant { get; set; } = new List<Lease>();
    public ICollection<TenantVerificationRequest> TenantVerificationRequests { get; set; } = new List<TenantVerificationRequest>();
    public ICollection<TenantVerificationRequest> ReviewedTenantVerificationRequests { get; set; } = new List<TenantVerificationRequest>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
