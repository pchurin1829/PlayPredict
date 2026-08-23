using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(150);
        builder.Property(t => t.ShortName).IsRequired().HasMaxLength(50);
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.Sport).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.Active);
    }
}
