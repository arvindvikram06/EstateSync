using EstateSync.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateSync.Api.Data.Configurations;

public sealed class ComplaintActivityConfiguration : IEntityTypeConfiguration<ComplaintActivity>
{
    public void Configure(EntityTypeBuilder<ComplaintActivity> builder)
    {
        builder.ToTable("ComplaintActivities");
        builder.HasKey(x => x.ComplaintActivityId).HasName("PK_ComplaintActivityId");
        builder.Property(x => x.ActorName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ActorRole).IsRequired().HasMaxLength(30);
        builder.Property(x => x.ActivityType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Summary).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ComplaintId, x.CreatedAt });
        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
