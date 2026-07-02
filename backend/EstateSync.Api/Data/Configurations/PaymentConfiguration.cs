using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.PaymentId).HasName("PK_PaymentId");

        builder.Property(x => x.TransactionId).IsRequired().HasMaxLength(250);
        builder.Property(x => x.GatewayResponse).HasColumnType("jsonb");
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => x.LeaseBillId);
        builder.HasIndex(x => x.PaymentStatus);
        builder.HasIndex(x => x.TransactionId).IsUnique();

        builder.HasOne(x => x.LeaseBill)
            .WithMany(c => c.Payments)
            .HasForeignKey(x => x.LeaseBillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaidByUser)
            .WithMany(u => u.Payments)
            .HasForeignKey(x => x.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}