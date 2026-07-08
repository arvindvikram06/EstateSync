using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.ToTable("OwnerProfiles");
        builder.HasKey(x => x.OwnerProfileId);

        builder.Property(x => x.OwnerType).IsRequired().HasConversion<string>();
        builder.Property(x => x.CompanyName).HasMaxLength(300);
        builder.Property(x => x.CompanyType).HasConversion<string>();
        builder.Property(x => x.CIN).HasMaxLength(21);
        builder.Property(x => x.GSTIN).HasMaxLength(15);
        builder.Property(x => x.OwnerSince).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(u => u.OwnerProfile)
            .HasForeignKey<OwnerProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
