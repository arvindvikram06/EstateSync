using EstateSync.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateSync.Api.Data.Configurations;

public sealed class PropertyVisitSlotConfiguration : IEntityTypeConfiguration<PropertyVisitSlot>
{
    public void Configure(EntityTypeBuilder<PropertyVisitSlot> builder)
    {
        builder.ToTable("PropertyVisitSlots", table =>
        {
            table.HasCheckConstraint("CK_PropertyVisitSlots_Duration", "\"DurationMinutes\" = 60");
        });
        builder.HasKey(x => x.PropertyVisitSlotId).HasName("PK_PropertyVisitSlotId");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.DurationMinutes).HasDefaultValue(60);
        builder.HasIndex(x => new { x.PropertyId, x.SlotDate, x.StartTime }).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.Status, x.SlotDate });
        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
