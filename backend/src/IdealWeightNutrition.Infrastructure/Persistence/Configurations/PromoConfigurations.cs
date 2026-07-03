using IdealWeightNutrition.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MinimumOrderAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MaximumDiscountAmount).HasColumnType("decimal(18,2)");
        builder.HasMany(p => p.ExcludedProducts)
            .WithOne()
            .HasForeignKey(e => e.PromoCodeId);
        builder.HasMany(p => p.ExcludedComboOffers)
            .WithOne()
            .HasForeignKey(e => e.PromoCodeId);
        builder.HasMany(p => p.ExcludedServiceSubscriptions)
            .WithOne()
            .HasForeignKey(e => e.PromoCodeId);
    }
}

internal sealed class PromoCodeExcludedProductConfiguration : IEntityTypeConfiguration<PromoCodeExcludedProduct>
{
    public void Configure(EntityTypeBuilder<PromoCodeExcludedProduct> builder)
    {
        builder.ToTable("PromoCodeExcludedProducts");
        builder.HasKey(e => e.Id);
    }
}

internal sealed class PromoCodeExcludedComboOfferConfiguration : IEntityTypeConfiguration<PromoCodeExcludedComboOffer>
{
    public void Configure(EntityTypeBuilder<PromoCodeExcludedComboOffer> builder)
    {
        builder.ToTable("PromoCodeExcludedComboOffers");
        builder.HasKey(e => e.Id);
    }
}

internal sealed class PromoCodeExcludedServiceSubscriptionConfiguration
    : IEntityTypeConfiguration<PromoCodeExcludedServiceSubscription>
{
    public void Configure(EntityTypeBuilder<PromoCodeExcludedServiceSubscription> builder)
    {
        builder.ToTable("PromoCodeExcludedServiceSubscriptions");
        builder.HasKey(e => e.Id);
    }
}

internal sealed class PromoCodeUsageConfiguration : IEntityTypeConfiguration<PromoCodeUsage>
{
    public void Configure(EntityTypeBuilder<PromoCodeUsage> builder)
    {
        builder.ToTable("PromoCodeUsages");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserId).HasMaxLength(450);
    }
}
