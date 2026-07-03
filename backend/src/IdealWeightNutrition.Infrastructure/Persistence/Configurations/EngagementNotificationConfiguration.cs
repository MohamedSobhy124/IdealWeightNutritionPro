using IdealWeightNutrition.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired();
        builder.Property(n => n.Message).IsRequired();
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.Icon).IsRequired();
        builder.Property(n => n.Link).IsRequired();
        builder.Property(n => n.UserId).HasMaxLength(450);
    }
}
