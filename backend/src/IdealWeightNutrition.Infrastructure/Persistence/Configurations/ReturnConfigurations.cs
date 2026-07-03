using IdealWeightNutrition.Domain.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.AdditionalNotes).HasMaxLength(1000);
        builder.Property(r => r.AdminNotes).HasMaxLength(500);
        builder.Property(r => r.ReturnTrackingNumber).HasMaxLength(100);
        builder.Property(r => r.ReturnCarrier).HasMaxLength(50);
        builder.Property(r => r.RefundTransactionId).HasMaxLength(200);
        builder.Property(r => r.RefundAmount).HasColumnType("decimal(18,2)");
        builder.HasOne(r => r.OrderHeader)
            .WithMany()
            .HasForeignKey(r => r.OrderHeaderId);
        builder.HasMany(r => r.Items)
            .WithOne(i => i.ReturnRequest)
            .HasForeignKey(i => i.ReturnRequestId);
    }
}

internal sealed class ReturnRequestItemConfiguration : IEntityTypeConfiguration<ReturnRequestItem>
{
    public void Configure(EntityTypeBuilder<ReturnRequestItem> builder)
    {
        builder.ToTable("ReturnRequestItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ReturnPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.ItemReason).HasMaxLength(500);
        builder.Property(i => i.ItemCondition).HasMaxLength(50);
        builder.HasOne(i => i.OrderDetail)
            .WithMany()
            .HasForeignKey(i => i.OrderDetailId);
    }
}
