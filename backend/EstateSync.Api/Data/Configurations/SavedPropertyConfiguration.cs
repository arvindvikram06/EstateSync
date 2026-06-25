using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
{
    public void Configure(EntityTypeBuilder<SavedProperty> builder)
    {
        builder.ToTable("SavedProperties");
        builder.HasKey(x => x.SavedPropertyId).HasName("PK_SavedPropertyId");

        builder.HasIndex(x => new { x.UserId, x.PropertyId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.SavedProperties)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Property)
            .WithMany(p => p.SavedProperties)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}