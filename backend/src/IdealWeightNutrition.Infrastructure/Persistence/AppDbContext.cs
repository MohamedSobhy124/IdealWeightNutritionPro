using IdealWeightNutrition.Domain.Cart;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Content;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Domain.Returns;
using IdealWeightNutrition.Domain.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ShoppingCartLine> ShoppingCartLines => Set<ShoppingCartLine>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<RemoteArea> RemoteAreas => Set<RemoteArea>();
    public DbSet<OrderHeader> OrderHeaders => Set<OrderHeader>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderAuditLog> OrderAuditLogs => Set<OrderAuditLog>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();
    public DbSet<ComboOffer> ComboOffers => Set<ComboOffer>();
    public DbSet<ComboOfferItem> ComboOfferItems => Set<ComboOfferItem>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<ServiceSubscription> ServiceSubscriptions => Set<ServiceSubscription>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();
    public DbSet<ServiceOffer> ServiceOffers => Set<ServiceOffer>();
    public DbSet<ServicePurchase> ServicePurchases => Set<ServicePurchase>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestItem> ReturnRequestItems => Set<ReturnRequestItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();
    public DbSet<StockNotification> StockNotifications => Set<StockNotification>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues => Set<ProductVariantOptionValue>();
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasDiscriminator<string>("Discriminator").HasValue<ApplicationUser>("ApplicationUser");
            entity.Property(u => u.Name).IsRequired(false);
            entity.Property(u => u.CompanyId).IsRequired(false);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
