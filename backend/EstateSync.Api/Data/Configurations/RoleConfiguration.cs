using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.RoleId).HasName("PK_RoleId");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Role { RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin", CreatedAt = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc) },
            new Role { RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Owner", CreatedAt = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc) },
            new Role { RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Tenant", CreatedAt = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}