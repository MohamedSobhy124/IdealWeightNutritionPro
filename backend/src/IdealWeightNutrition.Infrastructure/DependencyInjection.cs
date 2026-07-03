using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Services;
using IdealWeightNutrition.Infrastructure.Services.Hosted;
using IdealWeightNutrition.Infrastructure.Storage;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdealWeightNutrition.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.Configure<StockAlertOptions>(configuration.GetSection(StockAlertOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<StockNotificationFulfillmentOptions>(
            configuration.GetSection(StockNotificationFulfillmentOptions.SectionName));
        services.Configure<SiteSettingsOptions>(configuration.GetSection(SiteSettingsOptions.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
        services.Configure<GeideaSettings>(configuration.GetSection("Geidea"));
        services.Configure<TamaraSettings>(configuration.GetSection("Tamara"));
        services.Configure<TappySettings>(configuration.GetSection("Tappy"));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.Configure<CallOptions>(configuration.GetSection(CallOptions.SectionName));
        services.Configure<ExpiringProductsAlertOptions>(configuration.GetSection(ExpiringProductsAlertOptions.SectionName));
        services.Configure<PaymentVerificationOptions>(configuration.GetSection(PaymentVerificationOptions.SectionName));
        services.Configure<ProductStorageOptions>(configuration.GetSection(ProductStorageOptions.SectionName));
        services.Configure<LegacyStorageOptions>(configuration.GetSection(LegacyStorageOptions.SectionName));
        services.AddSingleton<LegacyWwwRootPathResolver>();
        services.AddSingleton<ProductStoragePathResolver>();
        services.AddSingleton<LegacyImageStorage>();
        services.AddSingleton<VideoBannerStorage>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGuestAccountService, GuestAccountService>();
        services.AddScoped<ICatalogueService, CatalogueService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<INewsletterService, NewsletterService>();
        services.AddScoped<IAdminPromoCodeService, AdminPromoCodeService>();
        services.AddScoped<IAdminCatalogueService, AdminCatalogueService>();
        services.AddScoped<IAdminFlashSaleService, AdminFlashSaleService>();
        services.AddScoped<IAdminComboOfferService, AdminComboOfferService>();
        services.AddScoped<IAdminDeliveryService, AdminDeliveryService>();
        services.AddScoped<IAdminBlogService, AdminBlogService>();
        services.AddScoped<IAdminServiceSubscriptionService, AdminServiceSubscriptionService>();
        services.AddScoped<IAdminServiceOfferService, AdminServiceOfferService>();
        services.AddScoped<IAdminServicePurchaseService, AdminServicePurchaseService>();
        services.AddScoped<IAdminStockNotificationService, AdminStockNotificationService>();
        services.AddScoped<IInAppNotificationService, InAppNotificationService>();
        services.AddScoped<IAdminCompanyService, AdminCompanyService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminProductVariantService, AdminProductVariantService>();
        services.AddScoped<IStockNotificationService, StockNotificationService>();
        services.AddScoped<IStockNotificationFulfillmentService, StockNotificationFulfillmentService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAdminNotificationService, AdminNotificationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IOrderNotificationService, OrderNotificationService>();
        services.AddScoped<IVideoBannerService, VideoBannerService>();
        services.AddScoped<ISeoService, SeoService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<IFlashSaleService, FlashSaleService>();
        services.AddScoped<IComboOfferService, ComboOfferService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IServiceSubscriptionService, ServiceSubscriptionService>();
        services.AddScoped<IServiceCheckoutService, ServiceCheckoutService>();
        services.AddScoped<IServicePurchaseService, ServicePurchaseService>();
        services.AddSingleton<GuestCartStore>();
        services.AddSingleton<AppliedPromoStore>();

        services.AddHostedService<ExpiringProductsBackgroundService>();
        services.AddHostedService<PaymentVerificationBackgroundService>();
        services.AddHostedService<LowStockDigestBackgroundService>();
        services.AddHostedService<StockNotificationFulfillmentBackgroundService>();

        var redisConnection = configuration.GetSection("Redis")["ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
