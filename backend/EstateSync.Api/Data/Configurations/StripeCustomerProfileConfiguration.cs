using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class StripeCustomerProfileConfiguration : IEntityTypeConfiguration<StripeCustomerProfile>
{
    public void Configure(EntityTypeBuilder<StripeCustomerProfile> builder)
    {
        builder.ToTable("StripeCustomerProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StripeCustomerId).IsRequired().HasMaxLength(255);

        // Each tenant must map to exactly one Stripe customer.
        // Unique index enforces this at the DB level — catches concurrent race where both
        // requests see null and both call CustomerService.CreateAsync before either saves.
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasIndex(x => x.StripeCustomerId);

        builder.HasOne(x => x.Tenant)
            .WithOne(u => u.StripeCustomerProfile)
            .HasForeignKey<StripeCustomerProfile>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
