using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class WelcomeCampaignConfiguration : IEntityTypeConfiguration<WelcomeCampaign>
{
    public void Configure(EntityTypeBuilder<WelcomeCampaign> builder)
    {
        builder.ToTable("WelcomeCampaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.IsActive });

        // Segunda barrera de integridad (además de la lógica en ActivateAsync): a nivel de
        // PostgreSQL, garantiza que nunca pueda haber más de una WelcomeCampaign con
        // IsActive=true por CompanyId, incluso ante activaciones concurrentes.
        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasFilter("\"IsActive\" = true")
            .HasDatabaseName("IX_WelcomeCampaigns_CompanyId_ActiveOnly");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.WelcomeCampaigns)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
