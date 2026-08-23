using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class EditionScoringConfigurationConfiguration : IEntityTypeConfiguration<EditionScoringConfiguration>
{
    public void Configure(EntityTypeBuilder<EditionScoringConfiguration> builder)
    {
        builder.ToTable("EditionScoringConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ExactScorePoints).IsRequired();
        builder.Property(c => c.CorrectOutcomePoints).IsRequired();
        builder.Property(c => c.IncorrectPoints).IsRequired();
        builder.Property(c => c.UseExperienceDefaults).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.PreferredPlayerEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.PreferredPlayerPointsPerGoal).IsRequired().HasDefaultValue(2);
        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.UpdatedAtUtc).IsRequired();

        // Relación uno a uno con Edition.
        builder.HasIndex(c => c.EditionId).IsUnique();

        builder.HasOne(c => c.Edition)
            .WithOne()
            .HasForeignKey<EditionScoringConfiguration>(c => c.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
