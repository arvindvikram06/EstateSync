using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class LeaseBillConfiguration : IEntityTypeConfiguration<LeaseBill>
{
    public void Configure(EntityTypeBuilder<LeaseBill> builder)
    {
        builder.ToTable("LeaseBills");
        builder.HasKey(x => x.LeaseBillId).HasName("PK_LeaseBillId");

        builder.Property(x => x.BillType).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.ActiveStripeSessionId).HasMaxLength(255);

        builder.HasIndex(x => x.LeaseId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.BillType);

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.LeaseBills)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
