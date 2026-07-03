namespace IdealWeightNutrition.Contracts.Admin;

public enum AdminServiceDiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public class AdminServiceOfferListItemDto
{
    public required int Id { get; init; }
    public required int ServiceSubscriptionId { get; init; }
    public required string ServiceTitle { get; init; }
    public int? PromoCodeId { get; init; }
    public string? PromoCode { get; init; }
    public required AdminServiceDiscountType DiscountType { get; init; }
    public required double DiscountValue { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class AdminServiceOfferDetailDto : AdminServiceOfferListItemDto
{
    public required DateTime CreatedDate { get; init; }
}

public sealed class UpsertAdminServiceOfferRequest
{
    public required int ServiceSubscriptionId { get; init; }
    public int? PromoCodeId { get; init; }
    public AdminServiceDiscountType DiscountType { get; init; }
    public required double DiscountValue { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public bool IsActive { get; init; } = true;
}
