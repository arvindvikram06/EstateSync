using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(x => x.LocationId).HasName("PK_LocationId");

        builder.Property(x => x.StateName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.District).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Pincode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Country).IsRequired().HasMaxLength(150);

        builder.HasIndex(x => x.Pincode);
        builder.HasIndex(x => x.StateName);
    }
}