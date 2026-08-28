using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class UserTeamPreferredPlayerConfiguration : IEntityTypeConfiguration<UserTeamPreferredPlayer>
{
    public void Configure(EntityTypeBuilder<UserTeamPreferredPlayer> builder)
    {
        builder.ToTable("UserTeamPreferredPlayers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.TeamId }).IsUnique();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TeamPlayer).WithMany().HasForeignKey(x => x.TeamPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
