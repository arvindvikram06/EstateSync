using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class TenantFinancialProfileConfiguration : IEntityTypeConfiguration<TenantFinancialProfile>
{
    public void Configure(EntityTypeBuilder<TenantFinancialProfile> builder)
    {
        builder.ToTable("TenantFinancialProfiles");
        builder.HasKey(x => x.ProfileId).HasName("PK_TenantFinancialProfileId");

        builder.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EmployerName).HasMaxLength(250);
        builder.Property(x => x.DeclaredMonthlyIncome).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReviewRemarks).HasMaxLength(1000);

        builder.HasOne(x => x.Tenant)
            .WithMany(u => u.TenantFinancialProfiles)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany(u => u.ReviewedTenantFinancialProfiles)
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
