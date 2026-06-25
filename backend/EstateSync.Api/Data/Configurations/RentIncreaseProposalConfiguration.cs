using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data.Configurations;

public class RentIncreaseProposalConfiguration : IEntityTypeConfiguration<RentIncreaseProposal>
{
    public void Configure(EntityTypeBuilder<RentIncreaseProposal> builder)
    {
        builder.ToTable("RentIncreaseProposals");
        builder.HasKey(x => x.ProposalId).HasName("PK_ProposalId");

        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(x => x.Lease)
            .WithMany(l => l.RentIncreaseProposals)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProposedBy)
            .WithMany()
            .HasForeignKey(x => x.ProposedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}