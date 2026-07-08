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

    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    
    public bool IsKycVerified { get; set; } = false;
    public bool IsOwnerVerified { get; set; } = false;
    public bool IsFinanciallyVerified { get; set; } = false;
    public decimal? VerifiedMonthlyIncome { get; set; }

    // Role extension profiles
    public OwnerProfile? OwnerProfile { get; set; }

    // Stripe Mappings
    public StripeConnectedAccount? StripeConnectedAccount { get; set; }
    public StripeCustomerProfile? StripeCustomerProfile { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }

    public Guid? DeletedById { get; set; }
    public User? DeletedBy { get; set; }


    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<KycVerification> KycVerifications { get; set; } = new List<KycVerification>();
    public ICollection<KycVerification> ReviewedKycVerifications { get; set; } = new List<KycVerification>();
    public ICollection<TenantFinancialProfile> TenantFinancialProfiles { get; set; } = new List<TenantFinancialProfile>();
    public ICollection<TenantFinancialProfile> ReviewedTenantFinancialProfiles { get; set; } = new List<TenantFinancialProfile>();
    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<SavedProperty> SavedProperties { get; set; } = new List<SavedProperty>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
    public ICollection<Lease> LeasesAsTenant { get; set; } = new List<Lease>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
