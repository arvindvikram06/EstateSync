using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Common;
using EstateSync.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace EstateSync.Api.Data;

public partial class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private bool _isSaving;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<TenantFinancialProfile> TenantFinancialProfiles => Set<TenantFinancialProfile>();
    public DbSet<TenantFinancialDocument> TenantFinancialDocuments => Set<TenantFinancialDocument>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<LeaseDocument> LeaseDocuments => Set<LeaseDocument>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseBill> LeaseBills => Set<LeaseBill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RentIncreaseProposal> RentIncreaseProposals => Set<RentIncreaseProposal>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintImage> ComplaintImages => Set<ComplaintImage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<StripeConnectedAccount> StripeConnectedAccounts => Set<StripeConnectedAccount>();
    public DbSet<StripeCustomerProfile> StripeCustomerProfiles => Set<StripeCustomerProfile>();
    public DbSet<OwnerSettlement> OwnerSettlements => Set<OwnerSettlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all Configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global query filters for soft-deletable entities (BaseEntity)
        modelBuilder.Entity<User>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<KycVerification>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<TenantFinancialProfile>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Document>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Property>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<RentalApplication>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Lease>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Complaint>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Payment>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<StripeConnectedAccount>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<StripeCustomerProfile>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<OwnerSettlement>().HasQueryFilter(x => x.DeletedAt == null);

        modelBuilder.Entity<LeaseBill>().HasQueryFilter(x => x.DeletedAt == null && (x.Lease == null || x.Lease.DeletedAt == null));
        modelBuilder.Entity<RentIncreaseProposal>().HasQueryFilter(x => x.DeletedAt == null && (x.Lease == null || x.Lease.DeletedAt == null));
        modelBuilder.Entity<Notification>().HasQueryFilter(x => x.DeletedAt == null);

        modelBuilder.Entity<PropertyImage>().HasQueryFilter(x => x.Property != null && x.Property.DeletedAt == null);
        modelBuilder.Entity<SavedProperty>().HasQueryFilter(x => x.Property != null && x.Property.DeletedAt == null);
        modelBuilder.Entity<PropertyDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);
        modelBuilder.Entity<LeaseDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);

        modelBuilder.Entity<ComplaintImage>().HasQueryFilter(x => x.Complaint != null && x.Complaint.DeletedAt == null);
    
        modelBuilder.Entity<UserRole>().HasQueryFilter(x => x.User != null && x.User.DeletedAt == null);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(x => x.User != null && x.User.DeletedAt == null);
    
        modelBuilder.Entity<KycDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);
        modelBuilder.Entity<TenantFinancialDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);
    }
}
