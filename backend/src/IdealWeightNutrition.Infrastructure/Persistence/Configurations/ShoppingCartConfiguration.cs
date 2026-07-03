using IdealWeightNutrition.Domain.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingCartLineConfiguration : IEntityTypeConfiguration<ShoppingCartLine>
{
    public void Configure(EntityTypeBuilder<ShoppingCartLine> builder)
    {
        builder.ToTable("ShoppingCarts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ApplicationUserId).HasMaxLength(450).IsRequired();
        builder.Property(c => c.FlashSalePrice).HasColumnType("decimal(18,2)");
    }
}
