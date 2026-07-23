using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.ToTable("Leases");
        builder.HasKey(x => x.LeaseId).HasName("PK_LeaseId");

        builder.Property(x => x.PaymentFrequency).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.AgreementText).HasMaxLength(4000);
        builder.Property(x => x.SignedAgreementUrl).HasMaxLength(500);
        builder.Property(x => x.TenantSignature).HasMaxLength(250);
        builder.Property(x => x.OwnerSignature).HasMaxLength(250);

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.EndDate);

        // At most ONE non-terminal lease per rental application (history of
        // voided/expired/terminated leases remains allowed). Backs up the
        // service-level guard in CreateLeaseAsync against concurrent creates.
        builder.HasIndex(x => x.ApplicationId)
            .IsUnique()
            .HasFilter("\"Status\" NOT IN ('Expired', 'Terminated', 'Voided')");

        builder.HasOne(x => x.Property)
            .WithMany(p => p.Leases)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany(u => u.LeasesAsTenant)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RentalApplication)
            .WithMany(a => a.Leases)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
