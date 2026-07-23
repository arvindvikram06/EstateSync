using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class SiteVisitConfiguration : IEntityTypeConfiguration<SiteVisit>
{
    public void Configure(EntityTypeBuilder<SiteVisit> builder)
    {
        builder.ToTable("SiteVisits");
        builder.HasKey(x => x.SiteVisitId).HasName("PK_SiteVisitId");

        builder.Property(x => x.VisitTime).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.OwnerNotes).HasMaxLength(500);
        builder.Property(x => x.RescheduleReason).HasMaxLength(500);

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PropertyVisitSlotId);
        builder.HasOne(x => x.PropertyVisitSlot)
            .WithMany()
            .HasForeignKey(x => x.PropertyVisitSlotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PreviousPropertyVisitSlot)
            .WithMany()
            .HasForeignKey(x => x.PreviousPropertyVisitSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
