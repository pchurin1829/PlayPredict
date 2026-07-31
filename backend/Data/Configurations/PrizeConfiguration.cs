using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class PrizeConfiguration : IEntityTypeConfiguration<Prize>
{
    public void Configure(EntityTypeBuilder<Prize> builder)
    {
        builder.ToTable("Prizes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ReferenceValue).HasMaxLength(150);
        builder.Property(p => p.SponsorName).HasMaxLength(150);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();

        builder.HasIndex(p => p.EditionId);
        builder.HasIndex(p => p.RoundId);

        builder.HasOne(p => p.Edition)
            .WithMany()
            .HasForeignKey(p => p.EditionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Round)
            .WithMany()
            .HasForeignKey(p => p.RoundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
