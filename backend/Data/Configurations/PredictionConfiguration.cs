using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("Predictions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PredictedHomeScore)
            .IsRequired();

        builder.Property(p => p.PredictedAwayScore)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        // El Pronóstico pertenece a una Liga: un usuario puede tener un pronóstico
        // distinto para el mismo partido en Ligas distintas, pero solo uno por Liga.
        builder.HasIndex(p => new { p.UserId, p.MatchId })
            .IsUnique();

        builder.HasOne(p => p.Match)
            .WithMany()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PreferredPlayer)
            .WithMany()
            .HasForeignKey(p => p.PreferredPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
