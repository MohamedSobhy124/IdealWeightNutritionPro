using IdealWeightNutrition.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Comment).IsRequired();
    }
}

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("Wishlists");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.ApplicationUserId).IsRequired();
    }
}

internal sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
    {
        builder.ToTable("NewsletterSubscriptions");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Email).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Source).HasMaxLength(50);
    }
}

internal sealed class StockNotificationConfiguration : IEntityTypeConfiguration<StockNotification>
{
    public void Configure(EntityTypeBuilder<StockNotification> builder)
    {
        builder.ToTable("StockNotifications");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(256);
        builder.Property(s => s.PhoneNumber).HasMaxLength(20);
        builder.Property(s => s.ApplicationUserId).HasMaxLength(450);
    }
}
