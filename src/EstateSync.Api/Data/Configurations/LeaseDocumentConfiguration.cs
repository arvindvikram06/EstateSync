using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class LeaseDocumentConfiguration : IEntityTypeConfiguration<LeaseDocument>
{
    public void Configure(EntityTypeBuilder<LeaseDocument> builder)
    {
        builder.ToTable("LeaseDocuments");
        builder.HasKey(x => x.LeaseDocumentId).HasName("PK_LeaseDocumentId");

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.LeaseDocuments)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}