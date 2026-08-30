using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class WelcomeCampaignSlideConfiguration : IEntityTypeConfiguration<WelcomeCampaignSlide>
{
    public void Configure(EntityTypeBuilder<WelcomeCampaignSlide> builder)
    {
        builder.ToTable("WelcomeCampaignSlides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FitMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.DurationSeconds).HasPrecision(4, 1).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.CampaignId, x.SortOrder });

        builder.HasOne(x => x.Campaign)
            .WithMany(x => x.Slides)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
