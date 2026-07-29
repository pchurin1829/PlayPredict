using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("Rounds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Order)
            .IsRequired();

        builder.HasIndex(r => r.EditionId);
        builder.HasIndex(r => new { r.EditionId, r.Order }).IsUnique();

        builder.HasMany(r => r.Matches)
            .WithOne(m => m.Round)
            .HasForeignKey(m => m.RoundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
