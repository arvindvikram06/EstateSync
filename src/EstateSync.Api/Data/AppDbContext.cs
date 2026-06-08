using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Common;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OwnerVerificationRequest> OwnerVerificationRequests => Set<OwnerVerificationRequest>();
    public DbSet<TenantVerificationRequest> TenantVerificationRequests => Set<TenantVerificationRequest>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<OwnerVerificationDocument> OwnerVerificationDocuments => Set<OwnerVerificationDocument>();
    public DbSet<TenantVerificationDocument> TenantVerificationDocuments => Set<TenantVerificationDocument>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<LeaseDocument> LeaseDocuments => Set<LeaseDocument>();
    public DbSet<AgreementType> AgreementTypes => Set<AgreementType>();
    public DbSet<AgreementTemplate> AgreementTemplates => Set<AgreementTemplate>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<AgreementAcknowledgement> AgreementAcknowledgements => Set<AgreementAcknowledgement>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RentIncreaseProposal> RentIncreaseProposals => Set<RentIncreaseProposal>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintImage> ComplaintImages => Set<ComplaintImage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all Configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global query filters for soft-deletable entities (BaseEntity)
        modelBuilder.Entity<User>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<OwnerVerificationRequest>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<TenantVerificationRequest>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Document>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Property>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<RentalApplication>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Lease>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Complaint>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Payment>().HasQueryFilter(x => x.DeletedAt == null);

        modelBuilder.Entity<PropertyImage>().HasQueryFilter(x => x.Property != null && x.Property.DeletedAt == null);
        modelBuilder.Entity<SavedProperty>().HasQueryFilter(x => x.Property != null && x.Property.DeletedAt == null);
        modelBuilder.Entity<PropertyDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);

        modelBuilder.Entity<Charge>().HasQueryFilter(x => x.Lease != null && x.Lease.DeletedAt == null);
        modelBuilder.Entity<RentIncreaseProposal>().HasQueryFilter(x => x.Lease != null && x.Lease.DeletedAt == null);
        modelBuilder.Entity<AgreementAcknowledgement>().HasQueryFilter(x => x.Lease != null && x.Lease.DeletedAt == null);
        modelBuilder.Entity<LeaseDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);

        modelBuilder.Entity<ComplaintImage>().HasQueryFilter(x => x.Complaint != null && x.Complaint.DeletedAt == null);
    
        modelBuilder.Entity<UserRole>().HasQueryFilter(x => x.User != null && x.User.DeletedAt == null);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(x => x.User != null && x.User.DeletedAt == null);
        modelBuilder.Entity<Notification>().HasQueryFilter(x => x.User != null && x.User.DeletedAt == null);
    
        modelBuilder.Entity<OwnerVerificationDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);
        modelBuilder.Entity<TenantVerificationDocument>().HasQueryFilter(x => x.Document != null && x.Document.DeletedAt == null);
    }

    private void OnBeforeSave()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }
    }

    public override int SaveChanges()
    {
        OnBeforeSave();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
