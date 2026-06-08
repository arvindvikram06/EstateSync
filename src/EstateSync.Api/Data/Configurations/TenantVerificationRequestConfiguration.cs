using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class TenantVerificationRequestConfiguration : IEntityTypeConfiguration<TenantVerificationRequest>
{
    public void Configure(EntityTypeBuilder<TenantVerificationRequest> builder)
    {
        builder.ToTable("TenantVerificationRequests");
        builder.HasKey(x => x.RequestId).HasName("PK_TenantVerificationRequestId");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);

        builder.HasOne(x => x.Tenant)
            .WithMany(u => u.TenantVerificationRequests)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany(u => u.ReviewedTenantVerificationRequests)
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RentalApplication)
            .WithMany(a => a.TenantVerificationRequests)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}