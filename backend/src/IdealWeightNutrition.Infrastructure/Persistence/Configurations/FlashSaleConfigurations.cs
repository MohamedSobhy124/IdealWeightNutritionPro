using IdealWeightNutrition.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale>
{
    public void Configure(EntityTypeBuilder<FlashSale> builder)
    {
        builder.ToTable("FlashSales");
        builder.HasKey(f => f.Id);
        builder.HasMany(f => f.Items)
            .WithOne(i => i.FlashSale)
            .HasForeignKey(i => i.FlashSaleId);
    }
}

internal sealed class FlashSaleItemConfiguration : IEntityTypeConfiguration<FlashSaleItem>
{
    public void Configure(EntityTypeBuilder<FlashSaleItem> builder)
    {
        builder.ToTable("FlashSaleItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FlashSalePrice).HasColumnType("decimal(18,2)");
    }
}
