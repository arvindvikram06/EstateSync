using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class TenantVerificationDocumentConfiguration : IEntityTypeConfiguration<TenantVerificationDocument>
{
    public void Configure(EntityTypeBuilder<TenantVerificationDocument> builder)
    {
        builder.ToTable("TenantVerificationDocuments");
        builder.HasKey(x => x.TenantVerificationDocumentId).HasName("PK_TenantVerificationDocumentId");

        builder.HasOne(x => x.TenantVerificationRequest)
            .WithMany(r => r.TenantVerificationDocuments)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}