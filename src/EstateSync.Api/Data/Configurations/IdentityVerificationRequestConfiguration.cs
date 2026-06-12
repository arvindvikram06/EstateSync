using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class IdentityVerificationRequestConfiguration : IEntityTypeConfiguration<IdentityVerificationRequest>
{
    public void Configure(EntityTypeBuilder<IdentityVerificationRequest> builder)
    {
        builder.ToTable("IdentityVerificationRequests");
        builder.HasKey(x => x.RequestId).HasName("PK_IdentityVerificationRequestId");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        builder.Property(x => x.AadhaarNumber).HasMaxLength(12);
        builder.Property(x => x.PanNumber).HasMaxLength(10);

        builder.HasOne(x => x.User)
            .WithMany(u => u.IdentityVerificationRequests)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany(u => u.ReviewedIdentityVerificationRequests)
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
