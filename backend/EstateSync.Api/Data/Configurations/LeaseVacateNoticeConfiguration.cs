using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class LeaseVacateNoticeConfiguration : IEntityTypeConfiguration<LeaseVacateNotice>
{
    public void Configure(EntityTypeBuilder<LeaseVacateNotice> builder)
    {
        builder.ToTable("LeaseVacateNotices");
        builder.HasKey(x => x.VacateNoticeId).HasName("PK_VacateNoticeId");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.PenaltyAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.VacateNotices)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
