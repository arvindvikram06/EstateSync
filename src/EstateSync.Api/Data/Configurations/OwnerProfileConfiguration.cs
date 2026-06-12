using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.ToTable("OwnerProfiles");
        builder.HasKey(x => x.OwnerProfileId).HasName("PK_OwnerProfileId");

        builder.Property(x => x.BankAccountNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BankIFSCCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.BankAccountHolderName).IsRequired().HasMaxLength(250);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(250);

        builder.HasOne(x => x.User)
            .WithOne(u => u.OwnerProfile)
            .HasForeignKey<OwnerProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
