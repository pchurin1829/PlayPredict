using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class PredictionEvaluationConfiguration : IEntityTypeConfiguration<PredictionEvaluation>
{
    public void Configure(EntityTypeBuilder<PredictionEvaluation> builder)
    {
        builder.ToTable("PredictionEvaluations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Points).IsRequired();
        builder.Property(e => e.ResultPoints).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.PreferredPlayerPoints).IsRequired().HasDefaultValue(0);

        builder.Property(e => e.EvaluationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.OfficialHomeScore).IsRequired();
        builder.Property(e => e.OfficialAwayScore).IsRequired();
        builder.Property(e => e.AppliedRuleValue).IsRequired();
        builder.Property(e => e.EvaluatedAtUtc).IsRequired();

        // Una única evaluación vigente por Prediction (sin historial en este Sprint).
        builder.HasIndex(e => e.PredictionId).IsUnique();

        builder.HasOne(e => e.Prediction)
            .WithOne()
            .HasForeignKey<PredictionEvaluation>(e => e.PredictionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
