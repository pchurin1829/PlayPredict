using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class MatchScorerConfiguration : IEntityTypeConfiguration<MatchScorer>
{
    public void Configure(EntityTypeBuilder<MatchScorer> builder)
    {
        builder.ToTable("MatchScorers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Goals).IsRequired();
        builder.HasIndex(x => new { x.MatchId, x.TeamPlayerId }).IsUnique();
        builder.HasOne(x => x.Match).WithMany(x => x.Scorers).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TeamPlayer).WithMany().HasForeignKey(x => x.TeamPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
