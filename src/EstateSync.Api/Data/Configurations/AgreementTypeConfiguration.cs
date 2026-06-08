using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class AgreementTypeConfiguration : IEntityTypeConfiguration<AgreementType>
{
    public void Configure(EntityTypeBuilder<AgreementType> builder)
    {
        builder.ToTable("AgreementTypes");
        builder.HasKey(x => x.AgreementTypeId).HasName("PK_AgreementTypeId");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}