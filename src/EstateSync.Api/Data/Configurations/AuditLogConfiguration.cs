using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.AuditLogId).HasName("PK_AuditLogId");

        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.EntityName).HasMaxLength(150);
        builder.Property(x => x.EntityType).HasMaxLength(150);
        builder.Property(x => x.Action).HasMaxLength(100);
        builder.Property(x => x.IPAddress).HasMaxLength(50);
        builder.Property(x => x.OldValues).HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.CreatedAt);
    }
}