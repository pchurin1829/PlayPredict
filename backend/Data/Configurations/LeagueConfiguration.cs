using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Data.Configurations;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("Leagues");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(150);
        builder.Property(l => l.Description).HasMaxLength(1000);
        builder.Property(l => l.InviteCode).IsRequired().HasMaxLength(20);
        builder.Property(l => l.IsActive).IsRequired();
        builder.Property(l => l.CreatedAtUtc).IsRequired();
        builder.Property(l => l.UpdatedAtUtc).IsRequired();
        builder.Property(l => l.UseGeneralScoring).IsRequired();
        builder.Property(l => l.PreferredPlayerPositions).IsRequired();

        builder.HasIndex(l => l.InviteCode).IsUnique();
        builder.HasIndex(l => l.CompetitionId);
        builder.HasIndex(l => l.EditionId);

        builder.HasOne(l => l.Competition)
            .WithMany()
            .HasForeignKey(l => l.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Edition)
            .WithMany()
            .HasForeignKey(l => l.EditionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.RoundFrom)
            .WithMany()
            .HasForeignKey(l => l.RoundFromId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.RoundTo)
            .WithMany()
            .HasForeignKey(l => l.RoundToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CreatedByUser)
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
