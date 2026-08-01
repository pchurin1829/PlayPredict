using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("Experiences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.PrimaryColor).HasMaxLength(20);
        builder.Property(e => e.SecondaryColor).HasMaxLength(20);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasIndex(e => e.Name);

        builder.HasMany(e => e.Competitions)
            .WithOne(c => c.Experience)
            .HasForeignKey(c => c.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
