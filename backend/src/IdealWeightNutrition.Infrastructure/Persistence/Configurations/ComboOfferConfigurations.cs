using IdealWeightNutrition.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ComboOfferConfiguration : IEntityTypeConfiguration<ComboOffer>
{
    public void Configure(EntityTypeBuilder<ComboOffer> builder)
    {
        builder.ToTable("ComboOffers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ComboPrice).HasColumnType("decimal(18,2)");
        builder.HasMany(c => c.Items)
            .WithOne(i => i.ComboOffer)
            .HasForeignKey(i => i.ComboOfferId);
        builder.HasMany(c => c.Images)
            .WithOne(i => i.ComboOffer)
            .HasForeignKey(i => i.ComboOfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ComboOfferImageConfiguration : IEntityTypeConfiguration<ComboOfferImage>
{
    public void Configure(EntityTypeBuilder<ComboOfferImage> builder)
    {
        builder.ToTable("ComboOfferImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ImageUrl).IsRequired();
        builder.Property(i => i.ImageInfo).HasMaxLength(500);
    }
}

internal sealed class ComboOfferItemConfiguration : IEntityTypeConfiguration<ComboOfferItem>
{
    public void Configure(EntityTypeBuilder<ComboOfferItem> builder)
    {
        builder.ToTable("ComboOfferItems");
        builder.HasKey(i => i.Id);
    }
}
