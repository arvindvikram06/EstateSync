using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(x => x.PropertyId).HasName("PK_PropertyId");

        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.AddressLine).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PropertyType).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.ApproveRemarks).HasMaxLength(1000);

        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.PropertyType);
        builder.HasIndex(x => x.MonthlyRent);
        builder.HasIndex(x => x.DeletedAt).HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => new { x.Latitude, x.Longitude });

        builder.HasOne(x => x.Owner)
            .WithMany(u => u.Properties)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(l => l.Properties)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApproveBy)
            .WithMany()
            .HasForeignKey(x => x.ApproveById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}