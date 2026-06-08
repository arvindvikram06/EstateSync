using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class AgreementAcknowledgementConfiguration : IEntityTypeConfiguration<AgreementAcknowledgement>
{
    public void Configure(EntityTypeBuilder<AgreementAcknowledgement> builder)
    {
        builder.ToTable("AgreementAcknowledgements");
        builder.HasKey(x => x.AcknowledgementId).HasName("PK_AgreementAcknowledgementId");
        builder.HasIndex(x => new { x.LeaseId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.AgreementAcknowledgements)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}