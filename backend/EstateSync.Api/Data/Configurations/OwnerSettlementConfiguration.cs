using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class OwnerSettlementConfiguration : IEntityTypeConfiguration<OwnerSettlement>
{
    public void Configure(EntityTypeBuilder<OwnerSettlement> builder)
    {
        builder.ToTable("OwnerSettlements");
        builder.HasKey(x => x.OwnerSettlementId).HasName("PK_OwnerSettlementId");

        builder.Property(x => x.StripePayoutId).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.DestinationId).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");

        // StripePayoutId is globally unique per Stripe payout — enforce at DB level
        // so concurrent duplicate webhook deliveries cannot both insert successfully.
        builder.HasIndex(x => x.StripePayoutId).IsUnique();
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.ArrivalDate);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
