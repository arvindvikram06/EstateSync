using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("KycDocuments");
        builder.HasKey(x => new { x.KycVerificationId, x.DocumentId }).HasName("PK_KycDocument");

        builder.HasOne(x => x.KycVerification)
            .WithMany(k => k.KycDocuments)
            .HasForeignKey(x => x.KycVerificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
