using IdealWeightNutrition.Domain.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired();
        builder.Property(p => p.ImageUrl).IsRequired();
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategryId);
        builder.HasOne(p => p.Brand)
            .WithMany()
            .HasForeignKey(p => p.BrandId);
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId);
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categries");
        builder.HasKey(c => c.Id);
    }
}

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(b => b.Id);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(i => i.Id);
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Price).HasColumnType("decimal(18,2)");
        builder.Property(v => v.ListPrice).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Price50).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Price100).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Sku).HasColumnName("SKU").HasMaxLength(100);
    }
}

internal sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("ProductOptions");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).HasMaxLength(100).IsRequired();
        builder.Property(o => o.NameAr).HasMaxLength(100).IsRequired();
        builder.HasMany(o => o.OptionValues)
            .WithOne(v => v.ProductOption!)
            .HasForeignKey(v => v.ProductOptionId);
    }
}

internal sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Value).HasMaxLength(100).IsRequired();
        builder.Property(v => v.ValueAr).HasMaxLength(100).IsRequired();
    }
}

internal sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("ProductVariantOptionValues");
        builder.HasKey(v => v.Id);
        builder.HasOne(v => v.OptionValue)
            .WithMany()
            .HasForeignKey(v => v.ProductOptionValueId);
    }
}
