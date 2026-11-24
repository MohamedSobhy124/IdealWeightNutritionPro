namespace IdealWeightNutrition.Domain.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Paid = "Paid";
    public const string Processing = "Processing";
    /// <summary>Spelled exactly as the legacy system stored it (existing rows use this value).</summary>
    public const string PreparingShipping = "Preparing Shiping";
    public const string Shipped = "Shipped";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string Returned = "Returned";
    public const string ReturnApproved = "ReturnApproved";
    public const string ReturnRequested = "ReturnRequested";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string DelayedPayment = "ApprovedForDelayedPayment";
    /// <summary>Authorised by the gateway but not yet captured (Tamara).</summary>
    public const string Authorized = "Authorized";
    public const string Cancelled = "Cancelled";
    public const string Rejected = "Rejected";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string Geidea = "Geidea";
    public const string Tamara = "Tamara";
    public const string Tabby = "Tabby";
}
