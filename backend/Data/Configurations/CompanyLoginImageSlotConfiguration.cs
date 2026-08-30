using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data.Configurations;

public class CompanyLoginImageSlotConfiguration : IEntityTypeConfiguration<CompanyLoginImageSlot>
{
    public void Configure(EntityTypeBuilder<CompanyLoginImageSlot> builder)
    {
        builder.ToTable("CompanyLoginImageSlots");
        builder.HasKey(x => new { x.CompanyId, x.Slot });
        builder.Property(x => x.Slot).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FitMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ImageKey).HasMaxLength(500);
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Company)
            .WithMany(x => x.LoginImageSlots)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
