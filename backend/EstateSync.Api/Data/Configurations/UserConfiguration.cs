using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.UserId).HasName("PK_UserId");

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(250);
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsKycVerified).HasDefaultValue(false);
        builder.Property(x => x.IsOwnerVerified).HasDefaultValue(false);

        // Default TRUE so pre-existing users aren't locked out by the migration;
        // new registrations explicitly write false.
        builder.Property(x => x.IsEmailVerified).HasDefaultValue(true);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.IsActive).HasFilter("\"DeletedAt\" IS NULL");

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UpdatedBy)
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new User
            {
                UserId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                FullName = "System Admin",
                Email = "admin@estatesync.com",
                PhoneNumber = "1234567890",
                PasswordHash = "$2a$12$ADKm2THOwyVdzzrREUVdjuvfxion06o.yHrByLvC6Gf0Fd5/PHlfq", // Password123!
                IsActive = true,
                IsKycVerified = true,
                IsOwnerVerified = false,
                CreatedAt = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}