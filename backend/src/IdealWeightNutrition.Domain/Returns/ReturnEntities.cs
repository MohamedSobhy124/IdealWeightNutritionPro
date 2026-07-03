using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;

namespace IdealWeightNutrition.Domain.Returns;

public sealed class ReturnRequest
{
    public int Id { get; set; }
    public int OrderHeaderId { get; set; }
    public string? ApplicationUserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }
    public string Status { get; set; } = ReturnStatuses.Pending;
    public DateTime RequestDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? RejectedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? AdminNotes { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReturnTrackingNumber { get; set; }
    public string? ReturnCarrier { get; set; }
    public DateTime? ReturnShippedDate { get; set; }
    public DateTime? ReturnReceivedDate { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime? RefundProcessedDate { get; set; }
    public string? RefundTransactionId { get; set; }

    public OrderHeader? OrderHeader { get; set; }
    public ICollection<ReturnRequestItem> Items { get; set; } = new List<ReturnRequestItem>();
}

public sealed class ReturnRequestItem
{
    public int Id { get; set; }
    public int ReturnRequestId { get; set; }
    public int OrderDetailId { get; set; }
    public int Quantity { get; set; }
    public decimal ReturnPrice { get; set; }
    public string? ItemReason { get; set; }
    public string? ItemCondition { get; set; }

    public ReturnRequest? ReturnRequest { get; set; }
    public OrderDetail? OrderDetail { get; set; }
}
