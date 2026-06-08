using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class ComplaintImageConfiguration : IEntityTypeConfiguration<ComplaintImage>
{
    public void Configure(EntityTypeBuilder<ComplaintImage> builder)
    {
        builder.ToTable("ComplaintImages");
        builder.HasKey(x => x.ComplaintImageId).HasName("PK_ComplaintImageId");

        builder.Property(x => x.Image).IsRequired().HasMaxLength(1000);

        builder.HasOne(x => x.Complaint)
            .WithMany(c => c.ComplaintImages)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}