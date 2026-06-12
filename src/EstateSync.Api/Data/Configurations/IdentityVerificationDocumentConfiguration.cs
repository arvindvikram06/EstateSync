using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class IdentityVerificationDocumentConfiguration : IEntityTypeConfiguration<IdentityVerificationDocument>
{
    public void Configure(EntityTypeBuilder<IdentityVerificationDocument> builder)
    {
        builder.ToTable("IdentityVerificationDocuments");
        builder.HasKey(x => x.IdentityVerificationDocumentId).HasName("PK_IdentityVerificationDocumentId");

        builder.HasOne(x => x.IdentityVerificationRequest)
            .WithMany(r => r.IdentityVerificationDocuments)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
