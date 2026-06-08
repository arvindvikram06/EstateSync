using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.ToTable("Charges");
        builder.HasKey(x => x.ChargeId).HasName("PK_ChargeId");

        builder.Property(x => x.ChargeType).HasConversion<string>().HasMaxLength(100);

        builder.HasIndex(x => x.LeaseId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.ChargeType);

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.Charges)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}