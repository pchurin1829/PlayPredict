using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ParticipantHome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.ParticipantAway)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.StartsAtUtc)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(m => m.RoundId);
        builder.HasIndex(m => m.HomeTeamId);
        builder.HasIndex(m => m.AwayTeamId);
        builder.HasIndex(m => m.StartsAtUtc);

        builder.HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
