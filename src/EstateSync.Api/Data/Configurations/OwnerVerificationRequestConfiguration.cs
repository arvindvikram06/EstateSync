using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class OwnerVerificationRequestConfiguration : IEntityTypeConfiguration<OwnerVerificationRequest>
{
    public void Configure(EntityTypeBuilder<OwnerVerificationRequest> builder)
    {
        builder.ToTable("OwnerVerificationRequests");
        builder.HasKey(x => x.RequestId).HasName("PK_OwnerVerificationRequestId");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        builder.Property(x => x.PanOrTaxId).HasMaxLength(50);

        builder.HasOne(x => x.User)
            .WithMany(u => u.OwnerVerificationRequests)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}