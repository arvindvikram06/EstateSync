using EstateSync.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateSync.Api.Data.Configurations;

public sealed class ComplaintEscalationContactConfiguration : IEntityTypeConfiguration<ComplaintEscalationContact>
{
    public void Configure(EntityTypeBuilder<ComplaintEscalationContact> builder)
    {
        builder.ToTable("ComplaintEscalationContacts");
        builder.HasKey(x => x.ComplaintEscalationContactId).HasName("PK_ComplaintEscalationContactId");
        builder.Property(x => x.AdminName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Method).IsRequired().HasMaxLength(30);
        builder.Property(x => x.InternalNote).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ComplaintId, x.ContactedAt });
        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.EscalationContacts)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
