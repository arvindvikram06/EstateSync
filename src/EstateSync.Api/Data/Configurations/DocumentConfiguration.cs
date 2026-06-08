using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(x => x.DocumentId).HasName("PK_DocumentId");

        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DocumentUrl).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(250);
        builder.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.UploadedById);
        builder.HasIndex(x => x.DocumentType);
        builder.HasIndex(x => x.DeletedAt).HasFilter("\"DeletedAt\" IS NULL");

        builder.HasOne(x => x.UploadedBy)
            .WithMany(u => u.UploadedDocuments)
            .HasForeignKey(x => x.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}