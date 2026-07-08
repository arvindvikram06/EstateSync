using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> builder)
    {
        builder.ToTable("KycVerifications");
        builder.HasKey(x => x.KycVerificationId).HasName("PK_KycVerificationId");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        builder.Property(x => x.AadhaarNumber).HasMaxLength(12);
        builder.Property(x => x.PanNumber).HasMaxLength(10);

        builder.HasIndex(x => x.AadhaarNumber)
            .IsUnique()
            .HasFilter("\"Status\" = 'Approved'");

        builder.HasIndex(x => x.PanNumber)
            .IsUnique()
            .HasFilter("\"Status\" = 'Approved'");

        builder.HasOne(x => x.User)
            .WithMany(u => u.KycVerifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany(u => u.ReviewedKycVerifications)
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
