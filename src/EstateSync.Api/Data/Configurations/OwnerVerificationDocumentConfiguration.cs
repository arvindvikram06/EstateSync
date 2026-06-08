using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class OwnerVerificationDocumentConfiguration : IEntityTypeConfiguration<OwnerVerificationDocument>
{
    public void Configure(EntityTypeBuilder<OwnerVerificationDocument> builder)
    {
        builder.ToTable("OwnerVerificationDocuments");
        builder.HasKey(x => x.OwnerVerificationDocumentId).HasName("PK_OwnerVerificationDocumentId");

        builder.HasOne(x => x.OwnerVerificationRequest)
            .WithMany(r => r.OwnerVerificationDocuments)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}