using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class TenantFinancialDocumentConfiguration : IEntityTypeConfiguration<TenantFinancialDocument>
{
    public void Configure(EntityTypeBuilder<TenantFinancialDocument> builder)
    {
        builder.ToTable("TenantFinancialDocuments");
        builder.HasKey(x => new { x.ProfileId, x.DocumentId }).HasName("PK_TenantFinancialDocument");

        builder.HasOne(x => x.TenantFinancialProfile)
            .WithMany(p => p.TenantFinancialDocuments)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
