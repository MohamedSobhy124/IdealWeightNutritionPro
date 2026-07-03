using IdealWeightNutrition.Domain.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasKey(c => c.Id);
    }
}

internal sealed class RemoteAreaConfiguration : IEntityTypeConfiguration<RemoteArea>
{
    public void Configure(EntityTypeBuilder<RemoteArea> builder)
    {
        builder.ToTable("RemoteAreas");
        builder.HasKey(r => r.Id);
        builder.HasOne(r => r.City)
            .WithMany()
            .HasForeignKey(r => r.CityId);
    }
}

internal sealed class OrderHeaderConfiguration : IEntityTypeConfiguration<OrderHeader>
{
    public void Configure(EntityTypeBuilder<OrderHeader> builder)
    {
        builder.ToTable("orderHeaders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.ApplicationUserId).HasMaxLength(450);
        builder.HasMany(o => o.Details)
            .WithOne()
            .HasForeignKey(d => d.OrderHeaderId);
    }
}

internal sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("orderDetails");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.PromoCodeDiscountAmount).HasColumnType("decimal(18,2)");
    }
}

internal sealed class OrderAuditLogConfiguration : IEntityTypeConfiguration<OrderAuditLog>
{
    public void Configure(EntityTypeBuilder<OrderAuditLog> builder)
    {
        builder.ToTable("OrderAuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).HasMaxLength(100).IsRequired();
        builder.Property(l => l.ActionDetails).HasMaxLength(500);
        builder.Property(l => l.PerformedByUserId).HasMaxLength(100);
        builder.Property(l => l.PerformedByUserEmail).HasMaxLength(256);
        builder.Property(l => l.OldOrderStatus).HasMaxLength(50);
        builder.Property(l => l.NewOrderStatus).HasMaxLength(50);
        builder.Property(l => l.OldPaymentStatus).HasMaxLength(50);
        builder.Property(l => l.NewPaymentStatus).HasMaxLength(50);
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.Property(l => l.CreatedBy).HasMaxLength(450);
        builder.Property(l => l.ModifiedBy).HasMaxLength(450);
    }
}
