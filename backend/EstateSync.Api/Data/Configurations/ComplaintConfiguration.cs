using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");
        builder.HasKey(x => x.ComplaintId).HasName("PK_ComplaintId");

        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.ComplaintType).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        builder.Property(x => x.ResolutionSummary).HasMaxLength(1000);
        builder.Property(x => x.ReopenReason).HasMaxLength(1000);

        builder.HasIndex(x => x.LeaseId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.Complaints)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaseBill)
            .WithMany(c => c.Complaints)
            .HasForeignKey(x => x.LeaseBillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
