using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class LeagueParticipantConfiguration : IEntityTypeConfiguration<LeagueParticipant>
{
    public void Configure(EntityTypeBuilder<LeagueParticipant> builder)
    {
        builder.ToTable("LeagueParticipants");

        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.JoinedAtUtc).IsRequired();
        builder.Property(lp => lp.LeftAtUtc);

        // Un usuario no puede unirse dos veces a la misma Liga.
        builder.HasIndex(lp => new { lp.LeagueId, lp.UserId })
            .IsUnique()
            .HasFilter("\"LeftAtUtc\" IS NULL");

        builder.HasOne(lp => lp.League)
            .WithMany(l => l.Participants)
            .HasForeignKey(lp => lp.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lp => lp.User)
            .WithMany()
            .HasForeignKey(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
