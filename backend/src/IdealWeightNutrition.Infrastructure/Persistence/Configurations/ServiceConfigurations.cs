using IdealWeightNutrition.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ServiceSubscriptionConfiguration : IEntityTypeConfiguration<ServiceSubscription>
{
    public void Configure(EntityTypeBuilder<ServiceSubscription> builder)
    {
        builder.ToTable("ServiceSubscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Price).HasColumnType("decimal(18,2)");
        builder.Property(s => s.OfflinePaymentPercent).HasColumnType("decimal(5,2)");
        builder.HasMany(s => s.Images)
            .WithOne(i => i.ServiceSubscription)
            .HasForeignKey(i => i.ServiceSubscriptionId);
        builder.HasMany(s => s.Offers)
            .WithOne(o => o.ServiceSubscription)
            .HasForeignKey(o => o.ServiceSubscriptionId);
    }
}

internal sealed class ServiceImageConfiguration : IEntityTypeConfiguration<ServiceImage>
{
    public void Configure(EntityTypeBuilder<ServiceImage> builder)
    {
        builder.ToTable("ServiceImages");
        builder.HasKey(i => i.Id);
    }
}

internal sealed class ServiceOfferConfiguration : IEntityTypeConfiguration<ServiceOffer>
{
    public void Configure(EntityTypeBuilder<ServiceOffer> builder)
    {
        builder.ToTable("ServiceOffers");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.DiscountValue).HasColumnType("decimal(18,2)");
    }
}

internal sealed class ServicePurchaseConfiguration : IEntityTypeConfiguration<ServicePurchase>
{
    public void Configure(EntityTypeBuilder<ServicePurchase> builder)
    {
        builder.ToTable("ServicePurchases");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.HasOne(p => p.ServiceSubscription)
            .WithMany()
            .HasForeignKey(p => p.ServiceSubscriptionId);
    }
}
