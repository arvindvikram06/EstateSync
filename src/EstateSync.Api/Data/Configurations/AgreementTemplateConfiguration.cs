using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class AgreementTemplateConfiguration : IEntityTypeConfiguration<AgreementTemplate>
{
    public void Configure(EntityTypeBuilder<AgreementTemplate> builder)
    {
        builder.ToTable("AgreementTemplates");
        builder.HasKey(x => x.TemplateId).HasName("PK_AgreementTemplateId");

        builder.Property(x => x.TemplateName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.HtmlContent).IsRequired();
        builder.Property(x => x.PlaceholderSchema).HasColumnType("jsonb");

        builder.HasOne(x => x.AgreementType)
            .WithMany(t => t.AgreementTemplates)
            .HasForeignKey(x => x.AgreementTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}